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
    this.isPrintView = true;
    document.body.classList.add('test-report-print-mode');
    setTimeout(() => {
      const prevTitle = document.title;
      document.title = '\u00A0';
      window.print();
      setTimeout(() => {
        document.title = prevTitle;
        document.body.classList.remove('test-report-print-mode');
        this.isPrintView = false;
      }, 500);
    }, 100);
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
