import { Component, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ReportService } from '../../_services/report.service';
import { AlertService } from '../../_services/alert.service';

export interface LabNoOption {
  labNo: string;
  invoiceNo?: string;
  patientName?: string;
  displayLabel: string;
}

export interface PrintableTestOption {
  testRequestDetailId: number;
  label: string;
}

@Component({
  selector: 'app-test-report',
  templateUrl: './test-report.component.html',
  styleUrls: ['../reports.shared.css', './test-report.component.css']
})
export class TestReportComponent implements OnInit, OnDestroy {
  readonly pageTitle = 'Diagnostic Report';

  labNo = '';
  labNumbers: LabNoOption[] = [];
  labNumbersLoading = false;
  filterError = '';
  loading = false;
  searched = false;
  report: any = null;
  isPrintView = false;

  /** Print All (default when eligible) or Specific Report. */
  printMode: 'all' | 'specific' = 'all';
  printableTests: PrintableTestOption[] = [];
  selectedTestRequestDetailId: number | null = null;
  canPrintAll = false;
  private fullReport: any = null;

  /** Phase 4: whole-report declarative HTML when server PresentationMode=Declarative. */
  useDeclarativePresentation = false;
  declarativeSafeHtml: SafeHtml = null;

  constructor(
    private reportService: ReportService,
    private alertService: AlertService,
    private sanitizer: DomSanitizer
  ) { }

  ngOnInit() {
    this.loadLabNumbers();
  }

  ngOnDestroy() {
    document.body.classList.remove('test-report-print-mode');
  }

  loadLabNumbers() {
    this.labNumbersLoading = true;
    this.reportService.getTestReportLabNumbers().subscribe(
      items => {
        this.labNumbers = (items || []).map((x: any) => this.normalizeLabOption(x));
        this.labNumbersLoading = false;
      },
      err => {
        this.labNumbersLoading = false;
        this.alertService.error(this.readError(err, 'Unable to load lab numbers.'));
      }
    );
  }

  clear() {
    this.labNo = '';
    this.filterError = '';
    this.report = null;
    this.fullReport = null;
    this.printableTests = [];
    this.selectedTestRequestDetailId = null;
    this.printMode = 'all';
    this.canPrintAll = false;
    this.searched = false;
    this.isPrintView = false;
    this.useDeclarativePresentation = false;
    this.declarativeSafeHtml = null;
    document.body.classList.remove('test-report-print-mode');
  }

  validateSearch(): boolean {
    this.filterError = '';
    if (!this.labNo?.trim()) {
      this.filterError = 'Select a Lab No to search.';
      return false;
    }
    return true;
  }

  search() {
    if (!this.validateSearch()) {
      return;
    }

    this.loading = true;
    this.report = null;
    this.fullReport = null;
    this.printableTests = [];
    this.selectedTestRequestDetailId = null;
    this.printMode = 'all';
    this.canPrintAll = false;
    this.searched = false;
    this.filterError = '';

    const lab = this.labNo.trim();
    this.reportService.getTestReportPrintOptions(lab).subscribe(
      options => {
        const normalized = this.normalizePrintOptions(options);
        this.canPrintAll = !!normalized.canPrintAll;
        this.printableTests = (normalized.tests || []).filter(t => t.isPrintable);
        this.searched = true;

        if (!this.printableTests.length) {
          this.loading = false;
          this.filterError = 'No approved test reports are ready for printing on this order.';
          this.alertService.error(this.filterError);
          return;
        }

        if (this.canPrintAll) {
          this.printMode = 'all';
          this.selectedTestRequestDetailId = null;
          this.loadFullReport(lab);
          return;
        }

        this.printMode = 'specific';
        this.selectedTestRequestDetailId = this.printableTests[0].testRequestDetailId;
        this.loadScopedReport();
      },
      err => {
        this.loading = false;
        this.searched = true;
        this.report = null;
        this.fullReport = null;
        this.printableTests = [];
        this.canPrintAll = false;
        this.filterError = this.readError(err);
        this.alertService.error(this.filterError);
      }
    );
  }

  onPrintModeChange() {
    if (this.printMode === 'all') {
      if (!this.canPrintAll) {
        this.printMode = 'specific';
        return;
      }
      this.selectedTestRequestDetailId = null;
      if (this.fullReport) {
        this.setReport(this.fullReport);
        return;
      }
      this.loadFullReport(this.labNo.trim());
      return;
    }

    if (this.printableTests.length && !this.selectedTestRequestDetailId) {
      this.selectedTestRequestDetailId = this.printableTests[0].testRequestDetailId;
    }
    this.loadScopedReport();
  }

  onSelectedTestChange() {
    if (this.printMode === 'specific') {
      this.loadScopedReport();
    }
  }

  private loadScopedReport() {
    if (!this.labNo?.trim() || !this.selectedTestRequestDetailId) {
      return;
    }

    this.loading = true;
    this.reportService.getTestReport(this.labNo.trim(), this.selectedTestRequestDetailId).subscribe(
      r => {
        this.setReport(this.normalizeReport(r));
        this.loading = false;
      },
      err => {
        this.loading = false;
        this.filterError = this.readError(err);
        this.alertService.error(this.filterError);
      }
    );
  }

  private loadFullReport(lab: string) {
    this.loading = true;
    this.reportService.getTestReport(lab).subscribe(
      r => {
        this.fullReport = this.normalizeReport(r);
        this.setReport(this.fullReport);
        // Prefer server print options; fall back to sections if options empty.
        if (!this.printableTests.length) {
          this.printableTests = this.buildPrintableTests(this.fullReport);
        }
        this.loading = false;
        const hasDept = this.report?.departmentGroups?.length;
        const hasSections = this.report?.sections?.length || this.report?.profileGroups?.length;
        if (!hasDept && !hasSections) {
          this.filterError = 'No report data returned.';
        }
      },
      err => {
        this.loading = false;
        this.report = null;
        this.fullReport = null;
        this.filterError = this.readError(err);
        this.alertService.error(this.filterError);
      }
    );
  }

  private normalizePrintOptions(raw: any): {
    canPrintAll: boolean;
    tests: Array<{ testRequestDetailId: number; label: string; isPrintable: boolean }>;
  } {
    if (!raw) {
      return { canPrintAll: false, tests: [] };
    }
    const testsRaw = raw.tests || raw.Tests || [];
    const tests = (testsRaw as any[]).map(t => ({
      testRequestDetailId: Number(t.testRequestDetailId ?? t.TestRequestDetailId ?? 0),
      label: String(t.label ?? t.Label ?? t.testName ?? t.TestName ?? t.testCode ?? t.TestCode ?? 'Test').trim(),
      isPrintable: !!(t.isPrintable ?? t.IsPrintable)
    })).filter(t => t.testRequestDetailId > 0);

    return {
      canPrintAll: !!(raw.canPrintAll ?? raw.CanPrintAll),
      tests
    };
  }

  print() {
    if (!this.report) {
      return;
    }

    if (this.printMode === 'all' && !this.canPrintAll) {
      this.filterError = 'All test reports are not yet approved.';
      this.alertService.error(this.filterError);
      return;
    }

    if (this.printMode === 'specific') {
      if (!this.selectedTestRequestDetailId) {
        this.filterError = 'Select a report to print.';
        this.alertService.error(this.filterError);
        return;
      }
    }

    this.executePrint();
  }

  private executePrint() {
    const source = document.getElementById('diagnostic-test-report-print');
    if (!source) {
      return;
    }

    const iframe = document.createElement('iframe');
    iframe.style.position = 'fixed';
    iframe.style.right = '0';
    iframe.style.bottom = '0';
    iframe.style.width = '0';
    iframe.style.height = '0';
    iframe.style.border = 'none';
    document.body.appendChild(iframe);

    const doc = iframe.contentWindow?.document;
    if (!doc) {
      document.body.removeChild(iframe);
      this.printViaWindow();
      return;
    }

    const styles = this.useDeclarativePresentation
      ? '<style>html,body{margin:0;padding:0;background:#fff;}</style>'
      : `<style>${this.getDiagnosticPrintStyles()}</style>`;

    doc.open();
    doc.write(`<!DOCTYPE html><html><head><title></title>${styles}</head><body class="diagnostic-report-print-doc">${source.innerHTML}</body></html>`);
    doc.close();

    setTimeout(() => {
      try {
        iframe.contentWindow?.focus();
        iframe.contentWindow?.print();
      } finally {
        setTimeout(() => {
          if (iframe.parentNode) {
            document.body.removeChild(iframe);
          }
        }, 1000);
      }
    }, 300);
  }

  private printViaWindow() {
    this.isPrintView = true;
    document.body.classList.add('test-report-print-mode');
    setTimeout(() => {
      const prevTitle = document.title;
      document.title = '';
      window.print();
      setTimeout(() => {
        document.title = prevTitle;
        document.body.classList.remove('test-report-print-mode');
        this.isPrintView = false;
      }, 500);
    }, 100);
  }

  private getDiagnosticPrintStyles(): string {
    const layout = this.resolvePrintLayout('Diagnostic');
    const header = this.mm(layout.headerHeightMm);
    const footer = this.mm(layout.footerHeightMm);
    const left = this.mm(layout.leftMarginMm);
    const right = this.mm(layout.rightMarginMm);
    const sigW = this.mm(layout.doctorSignatureWidthMm);
    const sigH = this.mm(layout.doctorSignatureHeightMm);
    const sigAlign = this.signatureAlignCss(layout.doctorSignatureHorizontal);
    const sigDisplay = layout.doctorSignatureEnabled ? 'block' : 'none';
    const sigImgMargin = layout.doctorSignatureHorizontal === 'Left'
      ? '0 auto 1mm 0'
      : (layout.doctorSignatureHorizontal === 'Center' ? '0 auto 1mm auto' : '0 0 1mm auto');
    const techW = this.mm(layout.technicianSignatureWidthMm);
    const techH = this.mm(layout.technicianSignatureHeightMm);
    const techAlign = this.signatureAlignCss(layout.technicianSignatureHorizontal);
    const techDisplay = layout.technicianSignatureEnabled ? 'block' : 'none';
    const techImgMargin = layout.technicianSignatureHorizontal === 'Left'
      ? '0 auto 1mm 0'
      : (layout.technicianSignatureHorizontal === 'Center' ? '0 auto 1mm auto' : '0 0 1mm auto');

    return `
      :root {
        --diag-header-total: ${header};
        --diag-footer-height: ${footer};
        --diag-doctor-gap: 8mm;
        --diag-technician-gap: 8mm;
        --diag-side-margin: ${left};
      }
      /* Layout values from Report Layout Configuration (fallback = production defaults). */
      @page {
        size: A4 portrait;
        margin-top: ${header};
        margin-bottom: ${footer};
        margin-left: ${left};
        margin-right: ${right};
      }
      html, body.diagnostic-report-print-doc {
        margin: 0; padding: 0;
        font-family: Segoe UI, Arial, Helvetica, sans-serif;
        font-size: 10pt; color: #111;
      }
      table { border-collapse: collapse; width: 100%; }
      .text-center { text-align: center; }
      /* Screen placeholders only — @page margins provide print letterhead bands */
      .report-print-header,
      .report-print-footer-zone,
      .letterhead-screen-hint { display: none !important; }
      .report-print-body { padding: 0; margin: 0; }
      .patient-info-table {
        border: 1.2pt solid #333; margin-bottom: 2.5mm; table-layout: fixed;
        page-break-inside: avoid; break-inside: avoid;
      }
      .patient-info-table td { border: 0.6pt solid #999; padding: 1.4mm 2mm; font-size: 9pt; line-height: 1.3; }
      .pi-label { font-weight: 700; background: #f3f7f8; width: 16%; white-space: nowrap; }
      .pi-value { width: 34%; word-wrap: break-word; }
      .pi-strong { font-weight: 700; text-transform: uppercase; }
      .report-department-header {
        margin: 3.5mm 0 1.5mm; padding: 1.8mm 2mm; text-align: center;
        font-size: 11pt; font-weight: 700; letter-spacing: 0.1em; text-transform: uppercase;
        border-top: 1.5pt solid #1a7f8c; border-bottom: 1.5pt solid #1a7f8c;
        background: #f0f7f8; page-break-after: avoid; break-after: avoid-page; page-break-inside: avoid;
      }
      .report-profile-header {
        margin: 3mm 0 1.5mm; padding: 1.5mm; text-align: center; font-size: 11pt; font-weight: 700;
        text-transform: uppercase; border-top: 1.5pt solid #1a7f8c; border-bottom: 1.5pt solid #1a7f8c;
        page-break-after: avoid;
      }
      .test-block-header {
        border-bottom: 1pt solid #555; padding: 2.2mm 0 1mm; margin: 2.5mm 0 1.2mm;
        font-size: 11pt; font-weight: 700; text-transform: uppercase;
        page-break-after: avoid; break-after: avoid-page; page-break-inside: avoid;
      }
      .report-results-table { margin-bottom: 1.5mm; table-layout: fixed; }
      .report-results-table thead { display: table-header-group; }
      .report-results-table tr { page-break-inside: avoid; break-inside: avoid; }
      .report-results-table th {
        background: #e8eef0; color: #111; font-weight: 700; border: 0.8pt solid #555;
        padding: 1.2mm 1.5mm; font-size: 8pt; text-transform: uppercase;
      }
      .report-results-table td { border: 0.6pt solid #888; padding: 1.1mm 1.5mm; font-size: 9pt; line-height: 1.3; }
      .col-param { width: 40%; text-align: left; }
      .col-result { width: 16%; text-align: center; }
      .col-unit { width: 14%; text-align: center; }
      .col-ref { width: 30%; text-align: center; word-wrap: break-word; }
      .param-section-heading {
        font-weight: 700; text-transform: uppercase; background: #f5f5f5; letter-spacing: 0.04em;
      }
      /* Allow tall tests to split; keep headers/rows intact to avoid mid-row clipping */
      .report-test-block { page-break-inside: auto; break-inside: auto; }
      .report-department-group { page-break-inside: auto; break-inside: auto; }
      .test-comment-block { margin: 1mm 0 2mm; font-size: 9pt; page-break-inside: avoid; }
      .test-comment-label { font-weight: 700; }
      .test-comment-text { white-space: pre-wrap; line-height: 1.35; }
      .doctor-approval-comment { page-break-inside: avoid; break-inside: avoid; }
      .abnormal-value { font-weight: 700; }
      .result-flag { font-weight: 700; }
      .report-technician-signature-zone {
        position: static; display: ${techDisplay}; width: 48%; max-width: 90mm;
        margin: var(--diag-technician-gap) 0 2mm; padding: 0;
        font-size: 8pt; line-height: 1.25;
        page-break-inside: avoid; break-inside: avoid;
        ${techAlign}
      }
      .report-technician-signature-img { max-height: ${techH}; max-width: ${techW}; display: block; margin: ${techImgMargin}; }
      .report-footer-technician-name { display: block; font-weight: 700; text-transform: uppercase; font-size: 9pt; }
      .report-footer-technician-qualification {
        display: block; white-space: pre-wrap; text-transform: uppercase; font-size: 7.5pt;
      }
      .report-doctor-signature-zone {
        position: static; display: ${sigDisplay}; width: 48%; max-width: 90mm;
        margin: var(--diag-doctor-gap) 0 2mm; padding: 0;
        font-size: 8pt; line-height: 1.25;
        page-break-inside: avoid; break-inside: avoid;
        ${sigAlign}
      }
      .report-signature-img { max-height: ${sigH}; max-width: ${sigW}; display: block; margin: ${sigImgMargin}; }
      .report-footer-doctor-name { display: block; font-weight: 700; text-transform: uppercase; font-size: 9pt; }
      .report-footer-doctor-qualification,
      .report-footer-doctor-designation {
        display: block; white-space: pre-wrap; text-transform: uppercase; font-size: 7.5pt;
      }
    `;
  }

  private resolvePrintLayout(reportType: 'Diagnostic' | 'Radiology') {
    const raw = this.report?.layout || this.report?.Layout || null;
    const defaults = reportType === 'Radiology'
      ? { headerHeightMm: 40, footerHeightMm: 50, leftMarginMm: 10, rightMarginMm: 10,
          doctorSignatureEnabled: true, doctorSignatureHorizontal: 'Left',
          doctorSignatureWidthMm: 50, doctorSignatureHeightMm: 14,
          technicianSignatureEnabled: false, technicianSignatureHorizontal: 'Left',
          technicianSignatureWidthMm: 50, technicianSignatureHeightMm: 14 }
      : { headerHeightMm: 50, footerHeightMm: 50, leftMarginMm: 12, rightMarginMm: 12,
          doctorSignatureEnabled: true, doctorSignatureHorizontal: 'Right',
          doctorSignatureWidthMm: 50, doctorSignatureHeightMm: 14,
          technicianSignatureEnabled: false, technicianSignatureHorizontal: 'Left',
          technicianSignatureWidthMm: 50, technicianSignatureHeightMm: 14 };
    if (!raw) {
      return defaults;
    }
    return {
      headerHeightMm: this.num(raw.headerHeightMm ?? raw.HeaderHeightMm, defaults.headerHeightMm),
      footerHeightMm: this.num(raw.footerHeightMm ?? raw.FooterHeightMm, defaults.footerHeightMm),
      leftMarginMm: this.num(raw.leftMarginMm ?? raw.LeftMarginMm, defaults.leftMarginMm),
      rightMarginMm: this.num(raw.rightMarginMm ?? raw.RightMarginMm, defaults.rightMarginMm),
      doctorSignatureEnabled: (raw.doctorSignatureEnabled ?? raw.DoctorSignatureEnabled) !== false,
      doctorSignatureHorizontal: (raw.doctorSignatureHorizontal || raw.DoctorSignatureHorizontal || defaults.doctorSignatureHorizontal),
      doctorSignatureWidthMm: this.num(raw.doctorSignatureWidthMm ?? raw.DoctorSignatureWidthMm, defaults.doctorSignatureWidthMm),
      doctorSignatureHeightMm: this.num(raw.doctorSignatureHeightMm ?? raw.DoctorSignatureHeightMm, defaults.doctorSignatureHeightMm),
      technicianSignatureEnabled: !!(raw.technicianSignatureEnabled ?? raw.TechnicianSignatureEnabled),
      technicianSignatureHorizontal: (raw.technicianSignatureHorizontal || raw.TechnicianSignatureHorizontal || defaults.technicianSignatureHorizontal),
      technicianSignatureWidthMm: this.num(raw.technicianSignatureWidthMm ?? raw.TechnicianSignatureWidthMm, defaults.technicianSignatureWidthMm),
      technicianSignatureHeightMm: this.num(raw.technicianSignatureHeightMm ?? raw.TechnicianSignatureHeightMm, defaults.technicianSignatureHeightMm)
    };
  }

  private num(value: any, fallback: number): number {
    const n = Number(value);
    return isFinite(n) ? n : fallback;
  }

  private mm(value: number): string {
    return `${value}mm`;
  }

  private signatureAlignCss(horizontal: string): string {
    const h = (horizontal || 'Right').toLowerCase();
    if (h === 'left') {
      return 'margin-left: 0; margin-right: auto; text-align: left;';
    }
    if (h === 'center' || h === 'centre') {
      return 'margin-left: auto; margin-right: auto; text-align: center;';
    }
    return 'margin-left: auto; margin-right: 0; text-align: right;';
  }

  private normalizeLabOption(x: any): LabNoOption {
    const labNo = x.labNo ?? x.LabNo ?? '';
    const patientName = x.patientName ?? x.PatientName ?? '';
    return {
      labNo,
      invoiceNo: x.invoiceNo ?? x.InvoiceNo,
      patientName,
      displayLabel: x.displayLabel ?? x.DisplayLabel ?? (patientName ? `${labNo} — ${patientName}` : labNo)
    };
  }

  private normalizeReport(r: any): any {
    if (!r) {
      return null;
    }
    const header = r.header || r.Header || {};
    const mapSection = (s: any) => ({
      testRequestDetailId: s.testRequestDetailId ?? s.TestRequestDetailId ?? 0,
      testCode: s.testCode ?? s.TestCode,
      testName: s.testName ?? s.TestName,
      specimen: s.specimen ?? s.Specimen,
      sampleNo: s.sampleNo ?? s.SampleNo,
      department: s.department ?? s.Department,
      comment: s.comment ?? s.Comment,
      doctorApprovalComment: s.doctorApprovalComment ?? s.DoctorApprovalComment,
      parameters: (s.parameters || s.Parameters || []).map((p: any) => ({
        parameterCode: p.parameterCode ?? p.ParameterCode,
        parameterName: p.parameterName ?? p.ParameterName,
        resultValue: p.resultValue ?? p.ResultValue,
        unit: p.unit ?? p.Unit,
        referenceRange: p.referenceRange ?? p.ReferenceRange,
        flag: p.flag ?? p.Flag,
        isAbnormal: p.isAbnormal ?? p.IsAbnormal,
        sectionName: p.sectionName ?? p.SectionName
      }))
    });
    const sections = (r.sections || r.Sections || []).map(mapSection);
    const profileGroups = (r.profileGroups || r.ProfileGroups || []).map((g: any) => ({
      profileName: g.profileName ?? g.ProfileName,
      profileCode: g.profileCode ?? g.ProfileCode,
      sections: (g.sections || g.Sections || []).map(mapSection)
    }));
    const departmentGroups = (r.departmentGroups || r.DepartmentGroups || []).map((g: any) => ({
      departmentName: g.departmentName ?? g.DepartmentName,
      sections: (g.sections || g.Sections || []).map(mapSection)
    }));

    const logoRaw = header.logoUrl ?? header.LogoUrl;
    let logoUrl = logoRaw;
    if (logoUrl && !/^https?:\/\//i.test(logoUrl) && !logoUrl.startsWith('data:')) {
      // Portal-relative asset path
      logoUrl = logoUrl.replace(/^\//, '');
    }

    return {
      header: {
        labNo: header.labNo ?? header.LabNo,
        invoiceNo: header.invoiceNo ?? header.InvoiceNo,
        patientName: header.patientName ?? header.PatientName,
        patientId: header.patientId ?? header.PatientId,
        mrNo: header.mrNo ?? header.MRNo,
        visitId: header.visitId ?? header.VisitId,
        age: header.age ?? header.Age,
        gender: header.gender ?? header.Gender,
        referralDoctor: header.referralDoctor ?? header.ReferralDoctor,
        corporate: header.corporate ?? header.Corporate,
        collectionDate: header.collectionDate ?? header.CollectionDate,
        receivedDate: header.receivedDate ?? header.ReceivedDate,
        reportDate: header.reportDate ?? header.ReportDate,
        status: header.status ?? header.Status,
        approvedBy: header.approvedBy ?? header.ApprovedBy,
        approvedByName: header.approvedByName ?? header.ApprovedByName,
        approvedByQualification: header.approvedByQualification ?? header.ApprovedByQualification,
        approvedByDesignation: header.approvedByDesignation ?? header.ApprovedByDesignation,
        approvedBySignatureImage: header.approvedBySignatureImage ?? header.ApprovedBySignatureImage,
        reviewedBy: header.reviewedBy ?? header.ReviewedBy,
        reviewedByName: header.reviewedByName ?? header.ReviewedByName,
        reviewedByQualification: header.reviewedByQualification ?? header.ReviewedByQualification,
        reviewedBySignatureImage: header.reviewedBySignatureImage ?? header.ReviewedBySignatureImage,
        doctorApprovalComment: header.doctorApprovalComment ?? header.DoctorApprovalComment,
        labName: header.labName ?? header.LabName,
        tagline: header.tagline ?? header.Tagline,
        centreName: header.centreName ?? header.CentreName,
        logoUrl,
        licenseName: header.licenseName ?? header.LicenseName,
        address: header.address ?? header.Address,
        email: header.email ?? header.Email,
        contactNumbers: header.contactNumbers ?? header.ContactNumbers,
        pharmacyContact: header.pharmacyContact ?? header.PharmacyContact,
        appointmentContact: header.appointmentContact ?? header.AppointmentContact
      },
      profileGroups,
      departmentGroups,
      sections,
      layout: r.layout || r.Layout || null,
      presentation: r.presentation || r.Presentation || null
    };
  }

  private setReport(report: any) {
    this.report = report;
    this.applyPresentation(report);
  }

  private applyPresentation(report: any) {
    const p = report?.presentation || report?.Presentation;
    const mode = (p?.presentationMode || p?.PresentationMode || '').toString();
    const html = p?.html || p?.Html;
    const css = p?.css || p?.Css;
    if (mode.toLowerCase() === 'declarative' && html) {
      this.useDeclarativePresentation = true;
      const styleBlock = css ? `<style type="text/css">${css}</style>` : '';
      this.declarativeSafeHtml = this.sanitizer.bypassSecurityTrustHtml(styleBlock + html);
    } else {
      this.useDeclarativePresentation = false;
      this.declarativeSafeHtml = null;
    }
  }

  private buildPrintableTests(report: any): PrintableTestOption[] {
    if (!report) {
      return [];
    }
    const sections: any[] = [];
    (report.departmentGroups || []).forEach((g: any) => {
      (g.sections || []).forEach((s: any) => sections.push(s));
    });
    if (!sections.length) {
      (report.profileGroups || []).forEach((g: any) => {
        (g.sections || []).forEach((s: any) => sections.push(s));
      });
      (report.sections || []).forEach((s: any) => sections.push(s));
    }

    const options: PrintableTestOption[] = [];
    const seen = new Set<number>();
    sections.forEach(s => {
      const id = Number(s.testRequestDetailId || 0);
      if (!id || seen.has(id)) {
        return;
      }
      seen.add(id);
      const label = (s.testName || s.testCode || `Test ${id}`).toString().trim();
      options.push({ testRequestDetailId: id, label });
    });
    return options;
  }

  private readError(err: any, fallback = 'Unable to load test report.'): string {
    if (!err) {
      return fallback;
    }
    if (typeof err === 'string') {
      return err;
    }
    if (typeof err.error === 'string' && err.error.trim()) {
      return err.error;
    }
    if (err.error?.message) {
      return err.error.message;
    }
    if (err.status === 404) {
      return 'Diagnostic report service is not available. Please contact your administrator.';
    }
    if (err.status === 400) {
      return err.statusText || fallback;
    }
    return fallback;
  }
}
