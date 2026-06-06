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
        if (!this.report?.sections?.length) {
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

    // Print via isolated iframe so portal sidebar/header never appear on paper/PDF.
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
    doc.write(`<!DOCTYPE html><html><head><title>Diagnostic Report</title>${styles}</head><body class="diagnostic-report-print-doc">${source.innerHTML}</body></html>`);
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

  /** Fallback if iframe print is blocked */
  private printViaWindow() {
    this.isPrintView = true;
    document.body.classList.add('test-report-print-mode');
    setTimeout(() => {
      const prevTitle = document.title;
      document.title = 'Diagnostic Report';
      window.print();
      setTimeout(() => {
        document.title = prevTitle;
        document.body.classList.remove('test-report-print-mode');
        this.isPrintView = false;
      }, 500);
    }, 100);
  }

  /** Print layout: fixed header (letterhead zone) + footer on every page. */
  private getDiagnosticPrintStyles(): string {
    return `
      :root {
        --diag-letterhead-height: 32mm;
        --diag-title-bar-height: 8mm;
        --diag-header-total: 40mm;
        --diag-footer-height: 16mm;
        --diag-side-margin: 10mm;
      }
      @page { size: A4 portrait; margin: 0; }
      body.diagnostic-report-print-doc {
        margin: 0;
        padding: 0;
        font-family: Segoe UI, Arial, sans-serif;
        font-size: 11px;
        color: #222;
      }
      table { border-collapse: collapse; width: 100%; }
      .text-right { text-align: right; }
      .text-center { text-align: center; }
      .report-print-header {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        height: var(--diag-header-total);
        z-index: 1000;
        background: #fff;
      }
      .report-letterhead-zone {
        height: var(--diag-letterhead-height);
        background: transparent;
      }
      .letterhead-screen-hint { display: none; }
      .report-print-title-bar {
        height: var(--diag-title-bar-height);
        line-height: var(--diag-title-bar-height);
        padding: 0 var(--diag-side-margin);
        margin: 0;
        text-align: center;
        font-size: 11pt;
        font-weight: 700;
        letter-spacing: 0.05em;
        text-transform: uppercase;
        border-bottom: 1.5pt solid #1a7f8c;
        box-sizing: border-box;
      }
      .report-print-footer {
        position: fixed;
        bottom: 0;
        left: 0;
        right: 0;
        height: var(--diag-footer-height);
        padding: 2mm var(--diag-side-margin) 0;
        border-top: 0.5pt solid #888;
        font-size: 8pt;
        color: #333;
        background: #fff;
        z-index: 1000;
        box-sizing: border-box;
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
      .report-footer-sign { margin-top: 1.5mm; font-size: 7.5pt; }
      .report-footer-approved { flex: 1; }
      .report-print-body {
        padding: 42mm var(--diag-side-margin) 19mm var(--diag-side-margin);
      }
      .report-meta-table td { border: 1px solid #ccc; padding: 3px 6px; }
      .meta-label { font-weight: 600; background: #f5f5f5; width: 14%; }
      .test-block-header {
        background: #e8f4f6;
        border: 1px solid #b8d4da;
        padding: 4px 6px;
        margin: 10px 0 3px;
      }
      .report-results-table th {
        background: #1a7f8c;
        color: #fff;
        padding: 3px 5px;
        text-align: left;
      }
      .report-results-table td { border: 1px solid #ddd; padding: 2px 5px; }
      .report-test-block { page-break-inside: avoid; }
      .abnormal-value { font-weight: 700; }
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
    const sections = (r.sections || r.Sections || []).map((s: any) => ({
      testCode: s.testCode ?? s.TestCode,
      testName: s.testName ?? s.TestName,
      specimen: s.specimen ?? s.Specimen,
      sampleNo: s.sampleNo ?? s.SampleNo,
      department: s.department ?? s.Department,
      parameters: (s.parameters || s.Parameters || []).map((p: any) => ({
        parameterCode: p.parameterCode ?? p.ParameterCode,
        parameterName: p.parameterName ?? p.ParameterName,
        resultValue: p.resultValue ?? p.ResultValue,
        unit: p.unit ?? p.Unit,
        referenceRange: p.referenceRange ?? p.ReferenceRange,
        flag: p.flag ?? p.Flag,
        isAbnormal: p.isAbnormal ?? p.IsAbnormal
      }))
    }));

    return {
      header: {
        labNo: header.labNo ?? header.LabNo,
        invoiceNo: header.invoiceNo ?? header.InvoiceNo,
        patientName: header.patientName ?? header.PatientName,
        patientId: header.patientId ?? header.PatientId,
        age: header.age ?? header.Age,
        gender: header.gender ?? header.Gender,
        referralDoctor: header.referralDoctor ?? header.ReferralDoctor,
        corporate: header.corporate ?? header.Corporate,
        collectionDate: header.collectionDate ?? header.CollectionDate,
        reportDate: header.reportDate ?? header.ReportDate,
        approvedBy: header.approvedBy ?? header.ApprovedBy
      },
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
