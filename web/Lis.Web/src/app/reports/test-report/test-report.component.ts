import { Component, OnDestroy, OnInit } from '@angular/core';
import { ReportService } from '../../_services/report.service';
import { AlertService } from '../../_services/alert.service';

export interface LabNoOption {
  labNo: string;
  invoiceNo?: string;
  patientName?: string;
  displayLabel: string;
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

  constructor(
    private reportService: ReportService,
    private alertService: AlertService
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
    this.searched = false;
    this.isPrintView = false;
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
    this.searched = false;

    this.reportService.getTestReport(this.labNo.trim()).subscribe(
      r => {
        this.report = this.normalizeReport(r);
        this.searched = true;
        this.loading = false;
        const hasDept = this.report?.departmentGroups?.length;
        const hasSections = this.report?.sections?.length || this.report?.profileGroups?.length;
        if (!hasDept && !hasSections) {
          this.filterError = 'No report data returned.';
        }
      },
      err => {
        this.loading = false;
        this.searched = true;
        this.report = null;
        this.filterError = this.readError(err);
        this.alertService.error(this.filterError);
      }
    );
  }

  print() {
    if (!this.report) {
      return;
    }

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

    const styles = `<style>${this.getDiagnosticPrintStyles()}</style>`;

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
    return `
      :root {
        --diag-header-total: 5cm;
        --diag-footer-height: 5cm;
        --diag-doctor-gap: 1cm;
        --diag-doctor-block-height: 28mm;
        --diag-side-margin: 12mm;
      }
      @page { size: A4 portrait; margin: 0; }
      body.diagnostic-report-print-doc {
        margin: 0; padding: 0;
        font-family: Segoe UI, Arial, Helvetica, sans-serif;
        font-size: 10pt; color: #111;
      }
      table { border-collapse: collapse; width: 100%; }
      .text-center { text-align: center; }
      .report-print-header {
        position: fixed; top: 0; left: 0; right: 0;
        height: var(--diag-header-total); z-index: 1000; background: #fff;
      }
      .letterhead-screen-hint { display: none; }
      .report-print-body {
        padding: calc(var(--diag-header-total) + 2mm) var(--diag-side-margin)
          calc(var(--diag-footer-height) + var(--diag-doctor-gap) + var(--diag-doctor-block-height) + 2mm)
          var(--diag-side-margin);
      }
      .patient-info-table { border: 1.2pt solid #333; margin-bottom: 3mm; }
      .patient-info-table td { border: 0.6pt solid #999; padding: 1.2mm 2mm; font-size: 9pt; }
      .pi-label { font-weight: 700; background: #f3f7f8; width: 16%; }
      .pi-value { width: 34%; }
      .pi-strong { font-weight: 700; text-transform: uppercase; }
      .report-department-header {
        margin: 4mm 0 2mm; padding: 1.5mm 2mm; text-align: center;
        font-size: 11pt; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase;
        border-top: 1.5pt solid #1a7f8c; border-bottom: 1.5pt solid #1a7f8c;
        background: #f0f7f8; page-break-after: avoid;
      }
      .report-profile-header {
        margin: 3mm 0 2mm; padding: 1.5mm; text-align: center; font-size: 11pt; font-weight: 700;
        text-transform: uppercase; border-top: 1.5pt solid #1a7f8c; border-bottom: 1.5pt solid #1a7f8c;
      }
      .test-block-header {
        border-bottom: 0.8pt solid #666; padding: 2mm 0 1mm; margin: 3mm 0 1mm;
        font-size: 11pt; font-weight: 700; text-transform: uppercase; page-break-after: avoid;
      }
      .report-results-table { margin-bottom: 2mm; }
      .report-results-table th {
        background: #e8eef0; color: #111; font-weight: 700; border: 0.8pt solid #555;
        padding: 1.2mm 1.5mm; font-size: 8pt; text-transform: uppercase;
      }
      .report-results-table td { border: 0.6pt solid #888; padding: 1mm 1.5mm; font-size: 9pt; }
      .col-param { width: 42%; text-align: left; }
      .col-result, .col-unit, .col-ref { text-align: center; }
      .param-section-heading {
        font-weight: 700; text-transform: uppercase; background: #f5f5f5; letter-spacing: 0.04em;
      }
      .report-test-block { page-break-inside: avoid; }
      .test-comment-block { margin: 1mm 0 2.5mm; font-size: 9pt; }
      .test-comment-label { font-weight: 700; }
      .test-comment-text { white-space: pre-wrap; line-height: 1.35; }
      .report-doctor-approval-comment {
        margin: 4mm 0 3mm; padding-top: 2mm; border-top: 0.8pt solid #666;
        font-size: 9pt; page-break-inside: avoid;
      }
      .abnormal-value { font-weight: 700; }
      .result-flag { font-weight: 700; }
      .report-doctor-signature-zone {
        position: fixed; left: 0; right: 0;
        bottom: calc(var(--diag-footer-height) + var(--diag-doctor-gap));
        height: var(--diag-doctor-block-height); z-index: 1001;
        box-sizing: border-box; padding: 0 var(--diag-side-margin);
        display: flex; flex-direction: column; align-items: flex-start; justify-content: flex-end;
        text-align: left; font-size: 8pt; line-height: 1.25;
      }
      .report-signature-img { max-height: 14mm; max-width: 50mm; display: block; margin: 0 0 1mm; }
      .report-footer-doctor-name { display: block; font-weight: 700; text-transform: uppercase; font-size: 9pt; }
      .report-footer-doctor-qualification,
      .report-footer-doctor-designation {
        display: block; white-space: pre-wrap; text-transform: uppercase; font-size: 7.5pt;
      }
      .report-print-footer-zone {
        position: fixed; bottom: 0; left: 0; right: 0;
        height: var(--diag-footer-height); z-index: 1000; background: #fff;
      }
    `;
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
      testCode: s.testCode ?? s.TestCode,
      testName: s.testName ?? s.TestName,
      specimen: s.specimen ?? s.Specimen,
      sampleNo: s.sampleNo ?? s.SampleNo,
      department: s.department ?? s.Department,
      comment: s.comment ?? s.Comment,
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
      sections
    };
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
