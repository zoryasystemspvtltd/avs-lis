import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TestResultEditService } from '../../../_services/test-result-edit.service';
import { AlertService } from '../../../_services/alert.service';
import { extractApiError } from '../../../_helpers/api-error';

/** ReportStatusType.ReportGenerated — ready for technician approval. */
const REPORT_GENERATED = 2;

@Component({
  selector: 'app-edit-test-results',
  templateUrl: './edit-test-results.component.html',
  styleUrls: ['./edit-test-results.component.css']
})
export class EditTestResultsComponent implements OnInit {
  sampleNo = '';
  invoiceNo = '';
  patientName = '';
  fromDate = '';
  toDate = '';

  searchRows: any[] = [];
  loading = false;
  saving = false;
  filterError = '';
  loadError = '';
  data: any = null;
  selectedTestIndex = 0;
  /** True when opened directly for a sample (from Recent Samples / lab-result-entry). */
  directSampleMode = false;
  /** True for Working Board → Test Result Edit (only Report Generated). */
  isTestResultEditMode = false;
  /** True when URL includes a sample number. */
  sampleRouteActive = false;

  constructor(
    private testResultEditService: TestResultEditService,
    private alertService: AlertService,
    private route: ActivatedRoute,
    private router: Router) { }

  ngOnInit(): void {
    this.isTestResultEditMode = (this.router.url || '').toLowerCase().indexOf('/edit-test-results') >= 0;
    this.route.params.subscribe(params => {
      const sn = (params['sampleNo'] || '').trim();
      this.isTestResultEditMode = (this.router.url || '').toLowerCase().indexOf('/edit-test-results') >= 0;
      this.sampleRouteActive = !!sn;
      if (sn) {
        this.directSampleMode = !this.isTestResultEditMode;
        this.loadSample(decodeURIComponent(sn));
      } else if (this.isTestResultEditMode) {
        this.directSampleMode = false;
        this.data = null;
        this.search();
      }
    });
  }

  get pageTitle(): string {
    return this.isTestResultEditMode ? 'Test Result Edit' : 'Lab Result Entry';
  }

  get pageSubtitle(): string {
    return this.isTestResultEditMode
      ? 'Edit results for tests that are ready for technician approval.'
      : 'Enter or update laboratory results for the selected sample.';
  }

  get selectedTest(): any {
    if (!this.data?.tests?.length) {
      return null;
    }
    return this.data.tests[this.selectedTestIndex];
  }

  /** Auto-detect entry vs edit — user does not choose the mode. */
  isEntryMode(test: any): boolean {
    return test && !test.testResultId;
  }

  private basePath(): string {
    return this.isTestResultEditMode ? '/edit-test-results' : '/lab-result-entry';
  }

  search() {
    this.filterError = '';
    this.data = null;
    const hasText = !!(this.sampleNo?.trim() || this.invoiceNo?.trim() || this.patientName?.trim());
    const hasDates = !!(this.fromDate || this.toDate);
    if (!this.isTestResultEditMode && !hasText && !hasDates) {
      this.filterError = 'Enter at least one search criterion (Sample / Lab No, Invoice No, Patient Name, or date range).';
      return;
    }
    this.loading = true;
    this.testResultEditService.search({
      sampleNo: this.sampleNo?.trim() || null,
      invoiceNo: this.invoiceNo?.trim() || null,
      patientName: this.patientName?.trim() || null,
      fromDate: this.fromDate || null,
      toDate: this.toDate || null,
      readyForTechnicianApproval: this.isTestResultEditMode ? true : undefined
    }).subscribe(
      rows => {
        this.searchRows = (rows || []).map((r: any) => ({
          ...r,
          sampleNo: r.sampleNo ?? r.SampleNo,
          invoiceNo: r.invoiceNo ?? r.InvoiceNo,
          patientName: r.patientName ?? r.PatientName,
          collectionDate: r.collectionDate ?? r.CollectionDate,
          reportStatus: r.reportStatus ?? r.ReportStatus,
          reportStatusLabel: r.reportStatusLabel ?? r.ReportStatusLabel,
          hasResults: r.hasResults ?? r.HasResults,
          canEnter: r.canEnter ?? r.CanEnter
        }));
        this.loading = false;
        if (this.searchRows.length === 1 && (hasText || hasDates)) {
          this.openSample(this.searchRows[0]);
        } else if (this.searchRows.length === 0) {
          this.filterError = this.isTestResultEditMode
            ? 'No samples ready for technician approval match the criteria.'
            : 'No matching samples found.';
        }
      },
      err => {
        this.loading = false;
        this.alertService.error(extractApiError(err, 'Search failed.'));
      }
    );
  }

  openSample(row: any) {
    if (!row?.sampleNo) {
      return;
    }
    if (!this.canOpenSample(row)) {
      this.filterError = this.isTestResultEditMode
        ? 'Only tests ready for technician approval can be edited.'
        : 'This sample cannot be edited in its current status, or no parameters are configured for its test(s).';
      return;
    }
    this.router.navigate([this.basePath(), encodeURIComponent(row.sampleNo)]);
  }

  canOpenSample(row: any): boolean {
    if (!row) {
      return false;
    }
    if (this.isTestResultEditMode) {
      const status = row.reportStatus ?? row.ReportStatus;
      return status === REPORT_GENERATED;
    }
    const hasResults = row.hasResults ?? row.HasResults;
    const canEnter = row.canEnter ?? row.CanEnter;
    if (hasResults) {
      return true;
    }
    return canEnter !== false;
  }

  loadSample(sampleNo: string) {
    if (!sampleNo) {
      this.loadError = 'Sample / Lab No is required.';
      return;
    }
    this.sampleNo = sampleNo;
    this.loading = true;
    this.filterError = '';
    this.loadError = '';
    this.testResultEditService.getBySampleNo(sampleNo).subscribe(
      d => {
        this.data = this.normalizeSampleDto(d);
        if (this.isTestResultEditMode && this.data?.tests?.length) {
          this.data.tests = this.data.tests.filter((t: any) =>
            (t.reportStatus ?? t.ReportStatus) === REPORT_GENERATED);
        }
        this.selectedTestIndex = 0;
        this.loading = false;
        if (!this.data?.tests?.length) {
          this.loadError = this.isTestResultEditMode
            ? 'No tests ready for technician approval were found for this sample.'
            : 'No editable tests found for this sample. Configure Test Parameter Mapping for the test(s), or check the approval status.';
          this.data = null;
        } else {
          const hasParams = this.data.tests.some((t: any) => (t.parameters || []).length > 0);
          if (!hasParams) {
            this.loadError = 'No parameters are configured for this sample\'s test(s). Add Test Parameter Mapping in Masters.';
            this.data = null;
          }
        }
      },
      err => {
        this.loading = false;
        this.data = null;
        this.loadError = extractApiError(err, 'Unable to load results.');
        this.alertService.error(this.loadError);
      }
    );
  }

  loadBySampleNo() {
    if (!this.sampleNo?.trim()) {
      this.filterError = 'Sample / Lab No is required.';
      return;
    }
    this.searchRows = [];
    this.router.navigate([this.basePath(), encodeURIComponent(this.sampleNo.trim())]);
  }

  selectTest(index: number) {
    this.selectedTestIndex = index;
  }

  recalcFlag(param: any) {
    if (!param || !this.selectedTest) {
      return;
    }
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
    if (this.isTestResultEditMode && (test.reportStatus ?? test.ReportStatus) !== REPORT_GENERATED) {
      this.alertService.error('Only tests ready for technician approval can be edited.');
      return;
    }
    if (!test.testRequestId) {
      return;
    }
    const filledParams = (test.parameters || []).filter((p: any) => (p.resultValue || '').toString().trim());
    if (!filledParams.length) {
      this.alertService.error('Enter at least one parameter value before saving.');
      return;
    }
    this.saving = true;
    const payload = {
      testResultId: test.testResultId || 0,
      testRequestId: test.testRequestId,
      parameters: filledParams.map((p: any) => ({
        detailId: p.detailId || 0,
        parameterCode: p.parameterCode,
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
        this.alertService.error(extractApiError(err, 'Save failed.'));
      }
    );
  }

  backToList() {
    if (this.isTestResultEditMode) {
      this.data = null;
      this.loadError = '';
      this.filterError = '';
      this.router.navigate(['/edit-test-results']);
      return;
    }
    this.router.navigate(['/samples']);
  }

  backToSamples() {
    this.backToList();
  }

  clear() {
    this.backToList();
  }

  private normalizeSampleDto(d: any): any {
    if (!d) {
      return null;
    }
    const tests = d.tests || d.Tests || [];
    return {
      ...d,
      sampleNo: d.sampleNo ?? d.SampleNo,
      invoiceNo: d.invoiceNo ?? d.InvoiceNo,
      patientName: d.patientName ?? d.PatientName,
      age: d.age ?? d.Age,
      gender: d.gender ?? d.Gender,
      tests: (tests || []).map((t: any) => ({
        ...t,
        hisTestCode: t.hisTestCode ?? t.HisTestCode,
        hisTestName: t.hisTestName ?? t.HisTestName,
        equipmentName: t.equipmentName ?? t.EquipmentName,
        reportStatus: t.reportStatus ?? t.ReportStatus,
        reportStatusLabel: t.reportStatusLabel ?? t.ReportStatusLabel,
        resultDate: t.resultDate ?? t.ResultDate,
        canEdit: t.canEdit ?? t.CanEdit,
        testRequestId: t.testRequestId ?? t.TestRequestId,
        testResultId: t.testResultId ?? t.TestResultId,
        parameters: (t.parameters || t.Parameters || []).map((p: any) => ({
          ...p,
          parameterCode: p.parameterCode ?? p.ParameterCode,
          parameterName: p.parameterName ?? p.ParameterName,
          resultValue: p.resultValue ?? p.ResultValue,
          unit: p.unit ?? p.Unit,
          referenceRange: p.referenceRange ?? p.ReferenceRange,
          flag: p.flag ?? p.Flag,
          isAbnormal: p.isAbnormal ?? p.IsAbnormal,
          method: p.method ?? p.Method,
          isEditable: p.isEditable ?? p.IsEditable,
          detailId: p.detailId ?? p.DetailId
        }))
      }))
    };
  }

}
