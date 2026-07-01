import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ReportService } from '../../_services/report.service';
import { AlertService } from '../../_services/alert.service';

@Component({
  selector: 'app-radiology-report-print',
  templateUrl: './radiology-report-print.component.html',
  styleUrls: ['../reports.shared.css', './radiology-report-print.component.css']
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

  constructor(
    private reportService: ReportService,
    private alertService: AlertService,
    private route: ActivatedRoute
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
    doc.write(`<!DOCTYPE html><html><head><title>Radiology Report</title><style>${this.getPrintStyles()}</style></head><body class="radiology-report-print-doc">${source.innerHTML}</body></html>`);
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
        age: h.age ?? h.Age,
        gender: h.gender ?? h.Gender,
        testName: h.testName ?? h.TestName,
        modality: h.modality ?? h.Modality,
        department: h.department ?? h.Department,
        reportStatus: h.reportStatus ?? h.ReportStatus,
        reportDate: h.reportDate ?? h.ReportDate,
        authorizedBy: h.authorizedBy ?? h.AuthorizedBy,
        authorizedOn: h.authorizedOn ?? h.AuthorizedOn,
        digitalSignature: h.digitalSignature ?? h.DigitalSignature
      },
      clinicalHistory: r?.clinicalHistory ?? r?.ClinicalHistory,
      findings: r?.findings ?? r?.Findings,
      impression: r?.impression ?? r?.Impression,
      recommendation: r?.recommendation ?? r?.Recommendation
    };
  }

  private readError(err: any, fallback = 'Unable to load radiology report.'): string {
    if (!err) { return fallback; }
    if (typeof err.error === 'string') { return err.error; }
    if (err.error?.message) { return err.error.message; }
    if (err.message) { return err.message; }
    return fallback;
  }

  private getPrintStyles(): string {
    return `
      @page { size: A4 portrait; margin: 12mm; }
      body { font-family: Segoe UI, Arial, sans-serif; font-size: 11px; color: #222; }
      h2 { margin: 0 0 8px; font-size: 16px; }
      .report-title-bar { background: #125d74; color: #fff; padding: 8px 12px; margin-bottom: 12px; font-weight: bold; }
      .report-meta-table td { padding: 4px 8px; border: 1px solid #ccc; }
      .meta-label { font-weight: 600; background: #f4f8fa; width: 18%; }
      .narrative-block { margin: 14px 0; }
      .narrative-block h4 { margin: 0 0 6px; color: #125d74; }
      .narrative-body { white-space: pre-wrap; line-height: 1.5; }
      .report-footer { margin-top: 24px; border-top: 1px solid #ccc; padding-top: 8px; font-size: 10px; }
    `;
  }
}
