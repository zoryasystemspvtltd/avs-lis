import { Component } from '@angular/core';
import { TestResultEditService } from '../../../_services/test-result-edit.service';
import { AlertService } from '../../../_services/alert.service';

@Component({
  selector: 'app-edit-test-results',
  templateUrl: './edit-test-results.component.html',
  styleUrls: ['./edit-test-results.component.css']
})
export class EditTestResultsComponent {
  sampleNo = '';
  invoiceNo = '';
  patientName = '';
  fromDate = '';
  toDate = '';

  searchRows: any[] = [];
  loading = false;
  saving = false;
  filterError = '';
  data: any = null;
  selectedTestIndex = 0;

  constructor(
    private testResultEditService: TestResultEditService,
    private alertService: AlertService) { }

  get selectedTest(): any {
    if (!this.data?.tests?.length) {
      return null;
    }
    return this.data.tests[this.selectedTestIndex];
  }

  search() {
    this.filterError = '';
    this.data = null;
    if (!this.sampleNo?.trim() && !this.invoiceNo?.trim() && !this.patientName?.trim()) {
      this.filterError = 'Enter Sample / Lab No, Invoice No, or Patient Name to search.';
      return;
    }
    this.loading = true;
    this.testResultEditService.search({
      sampleNo: this.sampleNo?.trim() || null,
      invoiceNo: this.invoiceNo?.trim() || null,
      patientName: this.patientName?.trim() || null,
      fromDate: this.fromDate || null,
      toDate: this.toDate || null
    }).subscribe(
      rows => {
        this.searchRows = rows || [];
        this.loading = false;
        if (this.searchRows.length === 1) {
          this.loadSample(this.searchRows[0].sampleNo);
        } else if (this.searchRows.length === 0) {
          this.filterError = 'No matching samples found.';
        }
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Search failed.'));
      }
    );
  }

  openSample(row: any) {
    if (!row?.sampleNo) {
      return;
    }
    if (row.hasResults === false) {
      this.filterError = 'No analyzer results exist for this sample yet.';
      return;
    }
    this.loadSample(row.sampleNo);
  }

  loadSample(sampleNo: string) {
    if (!sampleNo) {
      return;
    }
    this.sampleNo = sampleNo;
    this.loading = true;
    this.filterError = '';
    this.testResultEditService.getBySampleNo(sampleNo).subscribe(
      d => {
        this.data = d;
        this.selectedTestIndex = 0;
        this.loading = false;
      },
      err => {
        this.loading = false;
        this.data = null;
        this.alertService.error(this.readError(err, 'Unable to load results.'));
      }
    );
  }

  loadBySampleNo() {
    if (!this.sampleNo?.trim()) {
      this.filterError = 'Sample / Lab No is required.';
      return;
    }
    this.searchRows = [];
    this.loadSample(this.sampleNo.trim());
  }

  selectTest(index: number) {
    this.selectedTestIndex = index;
  }

  recalcFlag(param: any) {
    if (!param || !this.selectedTest) {
      return;
    }
    const test = this.selectedTest;
    const patient = { age: this.data?.age, gender: this.data?.gender };
    param.flag = '';
    param.isAbnormal = false;
    const val = parseFloat(('' + param.resultValue).replace(/,/g, ''));
    if (isNaN(val) || !param.referenceRange) {
      return;
    }
    const parts = param.referenceRange.split('-').map((x: string) => parseFloat(x.trim()));
    if (parts.length === 2 && !isNaN(parts[0]) && !isNaN(parts[1])) {
      if (parts[0] > 0 && val < parts[0]) {
        param.flag = 'L';
        param.isAbnormal = true;
      } else if (parts[1] > 0 && val > parts[1]) {
        param.flag = 'H';
        param.isAbnormal = true;
      }
    }
  }

  onValueChange(param: any) {
    this.recalcFlag(param);
  }

  save() {
    const test = this.selectedTest;
    if (!test?.canEdit) {
      this.alertService.error('This test result is read-only in the current approval status.');
      return;
    }
    if (!test.testResultId) {
      return;
    }
    this.saving = true;
    const payload = {
      testResultId: test.testResultId,
      testRequestId: test.testRequestId,
      parameters: (test.parameters || []).map((p: any) => ({
        detailId: p.detailId,
        resultValue: p.resultValue,
        remark: p.remark || ''
      }))
    };
    this.testResultEditService.save(payload).subscribe(
      r => {
        this.saving = false;
        this.alertService.success(r?.message || 'Saved successfully.');
        this.loadSample(this.data.sampleNo);
      },
      err => {
        this.saving = false;
        this.alertService.error(this.readError(err, 'Save failed.'));
      }
    );
  }

  clear() {
    this.sampleNo = '';
    this.invoiceNo = '';
    this.patientName = '';
    this.fromDate = '';
    this.toDate = '';
    this.searchRows = [];
    this.data = null;
    this.filterError = '';
  }

  private readError(err: any, fallback: string): string {
    if (err?.status === 401) {
      return 'Insufficient privilege. Your role needs Reports Edit or Authorize permission.';
    }
    if (typeof err?.error === 'string' && err.error.trim()) {
      return err.error;
    }
    if (err?.error?.message) {
      return err.error.message;
    }
    return fallback;
  }
}
