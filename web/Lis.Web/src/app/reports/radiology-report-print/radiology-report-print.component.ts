import { Component, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { ReportService } from '../../_services/report.service';
import { AlertService } from '../../_services/alert.service';

@Component({
  selector: 'app-radiology-report-print',
  templateUrl: './radiology-report-print.component.html',
  styleUrls: ['../reports.shared.css']
})
export class RadiologyReportPrintComponent implements OnInit, OnDestroy {
  readonly pageTitle = 'Radiology Report';

  selectedId: number = null;
  accessions: any[] = [];
  accessionsLoading = false;
  filterError = '';
  loading = false;
  searched = false;
  report: any = null;
  isPrintView = false;

  /** Phase 4: whole-report declarative HTML when server PresentationMode=Declarative. */
  useDeclarativePresentation = false;
  declarativeSafeHtml: SafeHtml = null;

  constructor(
    private reportService: ReportService,
    private alertService: AlertService,
    private route: ActivatedRoute,
    private sanitizer: DomSanitizer
  ) { }

  ngOnInit() {
    this.loadAccessions();
    const qid = +this.route.snapshot.queryParamMap.get('id');
    if (qid > 0) {
      this.selectedId = qid;
      this.search();
    }
  }

  ngOnDestroy() {
    document.body.classList.remove('radiology-report-print-mode');
  }

  loadAccessions() {
    this.accessionsLoading = true;
    this.reportService.getRadiologyPrintAccessions().subscribe(
      items => {
        this.accessions = (items || []).map((x: any) => ({
          id: x.radiologyRequestId ?? x.RadiologyRequestId,
          accessionNo: x.accessionNo ?? x.AccessionNo,
          displayLabel: x.displayLabel ?? x.DisplayLabel ?? x.accessionNo
        }));
        this.accessionsLoading = false;
      },
      err => {
        this.accessionsLoading = false;
        this.alertService.error(this.readError(err, 'Unable to load accessions.'));
      }
    );
  }

  clear() {
    this.selectedId = null;
    this.filterError = '';
    this.report = null;
    this.searched = false;
    this.isPrintView = false;
    this.useDeclarativePresentation = false;
    this.declarativeSafeHtml = null;
    document.body.classList.remove('radiology-report-print-mode');
  }

  validateSearch(): boolean {
    this.filterError = '';
    if (!this.selectedId || +this.selectedId <= 0) {
      this.filterError = 'Select an accession to search.';
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
    this.reportService.getRadiologyReport(+this.selectedId).subscribe(
      r => {
        this.report = this.normalizeReport(r);
        this.applyPresentation(this.report);
        this.searched = true;
        this.loading = false;
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
    const source = document.getElementById('diagnostic-radiology-report-print');
    if (!source) {
      return;
    }
    const iframe = document.createElement('iframe');
    iframe.style.cssText = 'position:fixed;right:0;bottom:0;width:0;height:0;border:none';
    document.body.appendChild(iframe);
    const doc = iframe.contentWindow?.document;
    if (!doc) {
      document.body.removeChild(iframe);
      this.printViaWindow();
      return;
    }
    doc.open();
    const styles = this.useDeclarativePresentation
      ? 'html,body{margin:0;padding:0;background:#fff;}'
      : this.getPrintStyles();
    doc.write(`<!DOCTYPE html><html><head><title>Radiology Report</title><style>${styles}</style></head><body class="radiology-report-print-doc">${source.innerHTML}</body></html>`);
    doc.close();
    setTimeout(() => {
      try {
        iframe.contentWindow?.focus();
        iframe.contentWindow?.print();
      } finally {
        setTimeout(() => { if (iframe.parentNode) { document.body.removeChild(iframe); } }, 1000);
      }
    }, 300);
  }

  private printViaWindow() {
    this.isPrintView = true;
    document.body.classList.add('radiology-report-print-mode');
    setTimeout(() => {
      window.print();
      setTimeout(() => {
        document.body.classList.remove('radiology-report-print-mode');
        this.isPrintView = false;
      }, 500);
    }, 100);
  }

  private normalizeReport(r: any) {
    const h = r?.header || r?.Header || {};
    return {
      header: {
        accessionNo: h.accessionNo ?? h.AccessionNo,
        invoiceNo: h.invoiceNo ?? h.InvoiceNo,
        patientName: h.patientName ?? h.PatientName,
        patientId: h.patientId ?? h.PatientId,
        mrNo: h.mrNo ?? h.MRNo,
        visitId: h.visitId ?? h.VisitId,
        age: h.age ?? h.Age,
        gender: h.gender ?? h.Gender,
        testName: h.testName ?? h.TestName,
        modality: h.modality ?? h.Modality,
        department: h.department ?? h.Department,
        reportStatus: h.reportStatus ?? h.ReportStatus,
        reportDate: h.reportDate ?? h.ReportDate,
        authorizedBy: h.authorizedBy ?? h.AuthorizedBy,
        authorizedOn: h.authorizedOn ?? h.AuthorizedOn,
        digitalSignature: h.digitalSignature ?? h.DigitalSignature,
        authorizedByName: h.authorizedByName ?? h.AuthorizedByName,
        authorizedByDesignation: h.authorizedByDesignation ?? h.AuthorizedByDesignation,
        authorizedBySignatureImage: h.authorizedBySignatureImage ?? h.AuthorizedBySignatureImage
      },
      clinicalHistory: r?.clinicalHistory ?? r?.ClinicalHistory,
      findings: r?.findings ?? r?.Findings,
      impression: r?.impression ?? r?.Impression,
      recommendation: r?.recommendation ?? r?.Recommendation,
      layout: r?.layout || r?.Layout || null,
      presentation: r?.presentation || r?.Presentation || null
    };
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

  private readError(err: any, fallback = 'Unable to load radiology report.'): string {
    if (!err) { return fallback; }
    if (typeof err.error === 'string') { return err.error; }
    if (err.error?.message) { return err.error.message; }
    if (err.message) { return err.message; }
    return fallback;
  }

  private getPrintStyles(): string {
    const layout = this.resolvePrintLayout();
    const header = this.mm(layout.headerHeightMm);
    const footerGap = this.mm(layout.footerHeightMm);
    const left = this.mm(layout.leftMarginMm);
    const right = this.mm(layout.rightMarginMm);
    const side = left; // radiology uses one side var historically; apply left for padding
    const sigW = this.mm(layout.doctorSignatureWidthMm);
    const sigH = this.mm(layout.doctorSignatureHeightMm);
    const sigSpace = this.mm(Math.max(28, layout.doctorSignatureHeightMm + 14));
    const sigDisplay = layout.doctorSignatureEnabled ? 'block' : 'none';
    const sigAlignSelf = layout.doctorSignatureHorizontal === 'Right'
      ? 'flex-end'
      : (layout.doctorSignatureHorizontal === 'Center' ? 'center' : 'flex-start');
    const sigTextAlign = layout.doctorSignatureHorizontal === 'Right'
      ? 'right'
      : (layout.doctorSignatureHorizontal === 'Center' ? 'center' : 'left');

    return `
      :root {
        --rad-title-bar-height: 8mm;
        --rad-header-total: ${header};
        --rad-letterhead-height: calc(${header} - var(--rad-title-bar-height));
        --rad-footer-gap: ${footerGap};
        --rad-footer-signature-space: ${sigSpace};
        --rad-footer-height: calc(var(--rad-footer-gap) + var(--rad-footer-signature-space));
        --rad-side-margin: ${side};
        --rad-side-margin-right: ${right};
      }
      @page { size: A4 portrait; margin: 0; }
      body.radiology-report-print-doc {
        margin: 0;
        padding: 0;
        font-family: Segoe UI, Arial, sans-serif;
        font-size: 11px;
        color: #222;
      }
      table { border-collapse: collapse; width: 100%; }
      .report-print-header {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        height: var(--rad-header-total);
        z-index: 1000;
        background: #fff;
      }
      .report-letterhead-zone {
        height: var(--rad-letterhead-height);
        background: transparent;
      }
      .letterhead-screen-hint { display: none; }
      .report-print-title-bar {
        height: var(--rad-title-bar-height);
        line-height: var(--rad-title-bar-height);
        padding: 0 var(--rad-side-margin);
        margin: 0;
        text-align: center;
        font-size: 11pt;
        font-weight: 700;
        letter-spacing: 0.05em;
        text-transform: uppercase;
        border-bottom: 1.5pt solid #125d74;
        box-sizing: border-box;
      }
      .report-print-footer-zone {
        position: fixed;
        bottom: 0;
        left: 0;
        right: 0;
        height: var(--rad-footer-height);
        z-index: 1000;
        background: #fff;
        box-sizing: border-box;
        padding: 2mm var(--rad-side-margin) var(--rad-footer-gap);
        display: flex;
        flex-direction: column;
        justify-content: flex-end;
        border: none;
      }
      .report-footer-signature {
        display: ${sigDisplay};
        align-self: ${sigAlignSelf};
        text-align: ${sigTextAlign};
        max-width: 55%;
        margin-bottom: 0;
        font-size: 8pt;
        line-height: 1.25;
      }
      .report-footer-doctor-name {
        display: block;
        font-weight: 600;
        margin-top: 0.5mm;
      }
      .report-footer-doctor-designation {
        display: block;
        white-space: pre-wrap;
        margin-top: 0.5mm;
      }
      .report-footer-legal {
        display: none;
        border-top: none;
      }
      .report-footer-row {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 8mm;
      }
      .report-footer-disclaimer { flex: 1; line-height: 1.25; }
      .report-footer-pagenum::after {
        content: "Page " counter(page) " of " counter(pages);
        white-space: nowrap;
        font-weight: 600;
      }
      .report-signature-img { max-height: ${sigH}; max-width: ${sigW}; display: block; margin-bottom: 1mm; }
      .report-print-body {
        padding: calc(var(--rad-header-total) + 2mm) var(--rad-side-margin-right)
          calc(var(--rad-footer-height) + 2mm) var(--rad-side-margin);
      }
      .report-meta-table td { border: 1px solid #ccc; padding: 3px 6px; }
      .meta-label { font-weight: 600; background: #f4f8fa; width: 18%; }
      .narrative-block { margin: 12px 0; }
      .narrative-block h4 { margin: 0 0 6px; color: #125d74; font-size: 10pt; }
      .narrative-body { white-space: pre-wrap; line-height: 1.5; }
    `;
  }

  private resolvePrintLayout() {
    const raw = this.report?.layout || this.report?.Layout || null;
    const defaults = {
      headerHeightMm: 40, footerHeightMm: 50, leftMarginMm: 10, rightMarginMm: 10,
      doctorSignatureEnabled: true, doctorSignatureHorizontal: 'Left',
      doctorSignatureWidthMm: 50, doctorSignatureHeightMm: 14
    };
    if (!raw) {
      return defaults;
    }
    const num = (v: any, fb: number) => {
      const n = Number(v);
      return isFinite(n) ? n : fb;
    };
    return {
      headerHeightMm: num(raw.headerHeightMm ?? raw.HeaderHeightMm, defaults.headerHeightMm),
      footerHeightMm: num(raw.footerHeightMm ?? raw.FooterHeightMm, defaults.footerHeightMm),
      leftMarginMm: num(raw.leftMarginMm ?? raw.LeftMarginMm, defaults.leftMarginMm),
      rightMarginMm: num(raw.rightMarginMm ?? raw.RightMarginMm, defaults.rightMarginMm),
      doctorSignatureEnabled: (raw.doctorSignatureEnabled ?? raw.DoctorSignatureEnabled) !== false,
      doctorSignatureHorizontal: raw.doctorSignatureHorizontal || raw.DoctorSignatureHorizontal || defaults.doctorSignatureHorizontal,
      doctorSignatureWidthMm: num(raw.doctorSignatureWidthMm ?? raw.DoctorSignatureWidthMm, defaults.doctorSignatureWidthMm),
      doctorSignatureHeightMm: num(raw.doctorSignatureHeightMm ?? raw.DoctorSignatureHeightMm, defaults.doctorSignatureHeightMm)
    };
  }

  private mm(value: number): string {
    return `${value}mm`;
  }
}
