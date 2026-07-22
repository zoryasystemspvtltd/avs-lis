import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AlertService, MasterService } from '../../_services';
import { extractApiError } from '../../_helpers/api-error';

@Component({
  selector: 'app-master-form',
  templateUrl: './master-form.component.html'
})
export class MasterFormComponent implements OnInit {

  form: FormGroup;
  submitted = false;
  loading = false;
  id: string;
  apiName: string;
  returnUrl: string;
  title: string;
  fields: any[] = [];
  tests: any[] = [];
  corporates: any[] = [];
  doctors: any[] = [];
  profiles: any[] = [];
  hisParameters: any[] = [];
  equipments: any[] = [];
  methods: any[] = [];
  units: any[] = [];
  parameterOptions: any[] = [];
  existingParameters: any[] = [];
  existingTestParameterMappings: any[] = [];
  lookupsLoaded = false;
  testRateOptions: any[] = [];
  testRateSearchLoading = false;
  private testRateSearchTimer: any;
  testParameterTestOptions: any[] = [];
  testParameterTestSearchLoading = false;
  private testParameterTestSearchTimer: any;
  /** Server-side test search — disable ng-select client filter. */
  readonly testParameterTestSearchFn = () => true;
  readonly parameterCodeExistsMessage = 'Parameter Code already exists.';
  readonly parameterDescriptionExistsMessage = 'Description already exists.';
  readonly parameterCombinationExistsMessage = 'Parameter Code and Description combination already exists.';
  patientMasterTab: 'details' | 'visits' = 'details';
  visitHistory: any[] = [];
  visitHistoryLoading = false;
  visitStarting = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private masterService: MasterService,
    private alertService: AlertService) { }

  ngOnInit() {
    this.apiName = this.route.snapshot.data['apiName'];
    this.returnUrl = this.route.snapshot.data['returnUrl'];
    this.title = this.route.snapshot.data['title'];
    this.fields = this.route.snapshot.data['fields'] || [];
    this.id = this.route.snapshot.params['id'];

    const group: any = {};
    this.fields.forEach(f => {
      group[f.name] = f.required ? ['', Validators.required] : [''];
    });
    if (this.apiName === 'TestRate') {
      group['testId'] = [null, Validators.required];
      group['rateType'] = [0];
      group['corporateId'] = [null];
      group['referralDoctorId'] = [null];
      group['testProfileId'] = [null];
      group['isActive'] = [true];
    }
    if (this.apiName === 'HisParameterMaster' && this.isParameterMasterScreen) {
      // Parameter Master is independent of Test.
    }
    if (this.apiName === 'TestParameterMappingMaster') {
      group['hisTestId'] = [null, Validators.required];
      group['hisParameterId'] = [null, Validators.required];
      group['hisTestPicker'] = [null, Validators.required];
      group['hisParameterPicker'] = [null, Validators.required];
    }
    if (this.apiName === 'HisParameterRangeMaster') {
      group['hisParameterId'] = [null, Validators.required];
    }
    if (this.apiName === 'TestMappingMaster') {
      group['equipmentId'] = [null, Validators.required];
      group['hisParameterPicker'] = [null, Validators.required];
      group['hisParamCode'] = [''];
      group['hisParamDescription'] = [''];
    }
    if (this.isParameterPickerScreen) {
      group['hisParameterPicker'] = [null];
    }
    if (this.fields.find(f => f.name === 'isActive')) {
      group['isActive'] = [true];
    }
    this.form = this.fb.group(group);

    if (this.apiName === 'TestRate') {
      this.loadTestRateLookups();
    } else if (this.apiName === 'HisParameterMaster' || this.apiName === 'HisParameterRangeMaster' ||
      this.apiName === 'TestMappingMaster' || this.apiName === 'TestParameterMappingMaster') {
      this.loadSetupLookups();
    } else     if (this.apiName === 'Department' || this.apiName === 'Unit' || this.apiName === 'Method' || this.apiName === 'Specimens') {
      if (!this.id && this.apiName === 'Department') {
        this.form.patchValue({ processingCategory: 'Laboratory' });
      }
      if (this.id) {
        this.masterService.getItem(this.apiName, this.id).subscribe(item => {
          if (item) {
            this.patchItem(item);
          }
        });
      }
    } else if (!this.id && this.apiName === 'PatientMaster') {
      this.assignNextPatientId();
    } else if (this.id) {
      this.masterService.getItem(this.apiName, this.id).subscribe(item => {
        if (item) {
          this.patchItem(item);
          if (this.apiName === 'PatientMaster') {
            this.loadVisitHistory();
          }
        }
      });
    }
  }

  setPatientMasterTab(tab: 'details' | 'visits'): void {
    this.patientMasterTab = tab;
    if (tab === 'visits') {
      this.loadVisitHistory();
    }
  }

  loadVisitHistory(): void {
    if (this.apiName !== 'PatientMaster' || !this.id) {
      return;
    }
    this.visitHistoryLoading = true;
    this.masterService.getPatientVisits(+this.id).subscribe(
      rows => {
        this.visitHistory = (rows || []).map(r => ({
          patientVisitId: r.patientVisitId ?? r.PatientVisitId,
          visitId: r.visitId ?? r.VisitId,
          visitDateTime: r.visitDateTime ?? r.VisitDateTime,
          saleInvoiceId: r.saleInvoiceId ?? r.SaleInvoiceId,
          invoiceNo: r.invoiceNo ?? r.InvoiceNo,
          visitStatusLabel: r.visitStatusLabel ?? r.VisitStatusLabel,
          createdBy: r.createdBy ?? r.CreatedBy
        }));
        this.visitHistoryLoading = false;
      },
      () => {
        this.visitHistory = [];
        this.visitHistoryLoading = false;
      }
    );
  }

  startNewVisitFromMaster(): void {
    if (!this.id || this.apiName !== 'PatientMaster' || this.visitStarting) {
      return;
    }
    this.visitStarting = true;
    this.masterService.startPatientVisit(+this.id).subscribe(
      visit => {
        this.visitStarting = false;
        const payload = visit?.patientVisitId != null || visit?.PatientVisitId != null
          ? visit
          : (visit || {});
        const visitId = payload.visitId ?? payload.VisitId ?? '';
        const patientVisitId = payload.patientVisitId ?? payload.PatientVisitId ?? null;
        this.form.patchValue({ visitId });
        this.loadVisitHistory();
        this.router.navigate(['/sale-invoices/create'], {
          state: { patientId: +this.id, patientVisitId }
        });
      },
      err => {
        this.visitStarting = false;
        this.alertService.error(extractApiError(err, 'Unable to start a new visit.'));
      }
    );
  }

  openVisitInvoice(invoiceId: number): void {
    if (invoiceId && +invoiceId > 0) {
      this.router.navigate(['/sale-invoices', +invoiceId]);
    }
  }

  get isTestParameterScreen(): boolean {
    return this.apiName === 'TestParameterMappingMaster';
  }

  get isParameterMasterScreen(): boolean {
    return this.apiName === 'HisParameterMaster' && (this.returnUrl || '').indexOf('/his-parameters') >= 0;
  }

  get isParameterPickerScreen(): boolean {
    return false;
  }

  isUnitOrMethodDropdown(field: any): boolean {
    return this.isParameterMasterScreen &&
      (field?.name === 'hisParamUnit' || field?.name === 'hisParamMethod');
  }

  unitMethodOptions(fieldName: string): any[] {
    return fieldName === 'hisParamMethod' ? this.methods : this.units;
  }

  isCodeReadonly(field: any): boolean {
    if (!field || field.name !== 'code' || !this.id) {
      return false;
    }
    return true;
  }

  isFieldReadonly(field: any): boolean {
    if (!field) {
      return false;
    }
    if (this.apiName === 'TestMappingMaster' &&
      (field.name === 'hisParamCode' || field.name === 'hisParamDescription')) {
      return true;
    }
    if (this.isTestParameterScreen &&
      (field.name === 'hisParamCode' || field.name === 'hisParamDescription')) {
      return true;
    }
    if (this.isParameterMasterScreen && field.name === 'hisParamCode' && !this.id) {
      return false;
    }
    if (this.isParameterMasterScreen && field.name === 'hisParamCode' && !!this.id) {
      return true;
    }
    if (this.apiName === 'HisParameterRangeMaster' && field.name === 'hisRangeCode') {
      return true;
    }
    if (this.apiName === 'PatientMaster' && field.name === 'mrNo') {
      return true;
    }
    if (this.apiName === 'PatientMaster' && field.name === 'visitId') {
      return true;
    }
    if (this.apiName === 'PatientMaster' && field.name === 'hisPatientId') {
      return true;
    }
    return !!field.readonly || this.isCodeReadonly(field);
  }

  visibleFields() {
    if (this.apiName === 'TestMappingMaster') {
      return this.fields.filter(f =>
        f.name !== 'hisParamCode' && f.name !== 'hisParamDescription');
    }
    if (this.apiName === 'TestRate') {
      return this.fields.filter(f => f.name !== 'taxPercent');
    }
    return this.fields;
  }

  get f() { return this.form.controls; }

  get rateType(): number {
    return +this.form.get('rateType').value;
  }

  get patientDisplayName(): string {
    const prefix = (this.form?.get('patientPrefix')?.value || '').trim();
    const name = (this.form?.get('name')?.value || '').trim();
    return [prefix, name].filter(Boolean).join(' ') || '—';
  }

  isInValid(controlName: string) {
    const c = this.form.get(controlName);
    return this.submitted && c && c.invalid;
  }

  patientAutoPlaceholder(field: any): string | null {
    if (this.apiName !== 'PatientMaster' || !field) {
      return null;
    }
    if (field.name === 'hisPatientId' && !this.id) {
      return 'Assigned automatically on save';
    }
    if ((field.name === 'mrNo' || field.name === 'visitId') && !this.id) {
      return 'Auto-generated';
    }
    return null;
  }

  toDateInput(d: Date | string) {
    if (!d) { return ''; }
    const dt = this.parseLocalDate(d);
    if (!dt) { return ''; }
    return this.formatLocalDate(dt);
  }

  private parseLocalDate(d: Date | string): Date | null {
    if (!d) { return null; }
    if (d instanceof Date) {
      return new Date(d.getFullYear(), d.getMonth(), d.getDate());
    }
    const text = ('' + d).trim();
    const datePart = text.length >= 10 ? text.substring(0, 10) : text;
    const pieces = datePart.split('-').map(v => +v);
    if (pieces.length === 3 && pieces.every(n => !isNaN(n))) {
      return new Date(pieces[0], pieces[1] - 1, pieces[2]);
    }
    const parsed = new Date(text);
    return isNaN(parsed.getTime())
      ? null
      : new Date(parsed.getFullYear(), parsed.getMonth(), parsed.getDate());
  }

  private formatLocalDate(dt: Date): string {
    const y = dt.getFullYear();
    const m = String(dt.getMonth() + 1).padStart(2, '0');
    const day = String(dt.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  private toApiDateString(d: Date | string): string {
    const dt = this.parseLocalDate(d);
    return dt ? this.formatLocalDate(dt) : '';
  }

  private loadSetupLookups() {
    const requests: { [key: string]: Observable<any> } = {};
    if (!this.isParameterMasterScreen && !this.isTestParameterScreen) {
      requests.tests = this.masterService.getLookupList('HisTest');
    }
    if (this.apiName === 'HisParameterMaster' || this.apiName === 'TestParameterMappingMaster') {
      requests.existingParams = this.masterService.getItems('HisParameterMaster', {
        RecordPerPage: 2000, CurrentPage: 1, SortColumnName: 'HISParamCode', SortDirection: true
      });
    }
    if (this.isParameterMasterScreen) {
      requests.units = this.masterService.getLookupList('Unit');
      requests.methods = this.masterService.getLookupList('Method');
    }
    if (this.apiName === 'HisParameterRangeMaster') {
      requests.params = this.masterService.getItems('HisParameterMaster', {
        RecordPerPage: 500, CurrentPage: 1, SortColumnName: 'HISParamCode', SortDirection: true
      });
    }
    if (this.apiName === 'TestMappingMaster' || this.apiName === 'TestParameterMappingMaster') {
      requests.params = this.masterService.getItems('HisParameterMaster', {
        RecordPerPage: 2000, CurrentPage: 1, SortColumnName: 'HISParamCode', SortDirection: true
      });
    }
    if (this.apiName === 'TestParameterMappingMaster') {
      requests.existingTestParamMappings = this.masterService.getItems('TestParameterMappingMaster', {
        RecordPerPage: 5000, CurrentPage: 1, SortColumnName: 'HISTestCode', SortDirection: true
      });
    }
    if (this.apiName === 'TestMappingMaster') {
      requests.equipments = this.httpEquipmentList();
    }
    forkJoin(requests).subscribe(
      (data: any) => {
        if (this.isTestParameterScreen) {
          this.tests = [];
          this.testParameterTestOptions = [];
        } else {
          this.tests = data.tests || [];
        }
        if (data.units) {
          this.units = (data.units || []).filter(u => u.isActive !== false);
        }
        if (data.methods) {
          this.methods = (data.methods || []).filter(m => m.isActive !== false);
        }
        if (data.existingParams) {
          this.existingParameters = (data.existingParams.items || data.existingParams.Items || data.existingParams) || [];
        }
        if (data.params) {
          const params = (data.params.items || data.params.Items || data.params) || [];
          this.hisParameters = (this.isTestParameterScreen || this.apiName === 'HisParameterRangeMaster')
            ? params.map((p: any) => this.toParameterPickerOption(p))
            : params;
          this.parameterOptions = this.hisParameters;
        }
        if (data.existingTestParamMappings) {
          this.existingTestParameterMappings = (data.existingTestParamMappings.items
            || data.existingTestParamMappings.Items
            || data.existingTestParamMappings) || [];
        }
        if (data.equipments) {
          this.equipments = data.equipments || [];
        }
        this.lookupsLoaded = true;
        if (this.isTestParameterScreen) {
          if (this.id) {
            this.masterService.getItem(this.apiName, this.id).subscribe(item => {
              if (item) {
                this.patchItem(item);
                this.syncTestParameterPickers(item);
              }
            });
          }
        } else if (this.apiName === 'HisParameterMaster' && this.id) {
          this.masterService.getItem(this.apiName, this.id).subscribe(item => {
            if (item) {
              this.patchItem(item);
            }
          });
        } else if (this.apiName === 'HisParameterRangeMaster' && !this.id) {
          this.assignNextRangeCode();
        } else if (this.id) {
          this.masterService.getItem(this.apiName, this.id).subscribe(item => {
            if (item) {
              this.patchItem(item);
              if (this.apiName === 'TestMappingMaster') {
                this.syncHisParameterPicker(item);
              }
              if (this.apiName === 'HisParameterRangeMaster') {
                this.lockRangeCodeField();
              }
            }
          });
        }
      },
      () => this.alertService.error('Failed to load lookup data.')
    );
  }

  private assignNextPatientId() {
    this.masterService.getNextPatientId().subscribe(
      pid => {
        if (pid) {
          this.form.patchValue({ hisPatientId: pid });
        }
      },
      () => {
        this.form.patchValue({ hisPatientId: '' });
      }
    );
    this.masterService.getNextMrNo().subscribe(
      mr => { if (mr) { this.form.patchValue({ mrNo: mr }); } },
      () => { this.form.patchValue({ mrNo: '' }); }
    );
    this.masterService.getNextVisitId().subscribe(
      vid => { if (vid) { this.form.patchValue({ visitId: vid }); } },
      () => { this.form.patchValue({ visitId: '' }); }
    );
  }

  private assignNextRangeCode() {
    this.masterService.getNextRangeCode().subscribe(code => {
      if (code) {
        this.form.patchValue({ hisRangeCode: code });
        this.lockRangeCodeField();
      }
    });
  }

  private lockRangeCodeField() {
    const control = this.form.get('hisRangeCode');
    if (control) {
      control.disable({ emitEvent: false });
    }
  }

  onHisParameterSelected() {
    const paramId = this.form.get('hisParameterPicker')?.value;
    const param = this.hisParameters.find(p => +p.id === +paramId);
    if (param) {
      const patch: any = {
        hisParamCode: param.hisParamCode || param.HISParamCode,
        hisParamDescription: param.hisParamDescription || param.HISParamDescription
      };
      if (this.apiName === 'TestParameterMappingMaster') {
        patch.hisParameterId = param.id;
      }
      this.form.patchValue(patch);
    }
  }

  onHisTestSelected() {
    const testId = this.form.get('hisTestPicker')?.value ?? this.form.get('hisTestId')?.value;
    const test = this.findTestPickerOption(testId);
    if (test) {
      const patch: any = {
        hisTestCode: test.hisTestCode || test.HISTestCode,
        hisTestCodeDescription: test.hisTestCodeDescription || test.HISTestCodeDescription
      };
      if (this.apiName === 'TestParameterMappingMaster') {
        patch.hisTestId = test.id;
      } else {
        patch.specimenCode = test.hisSpecimenCode || test.HISSpecimenCode || '';
        patch.specimenName = test.hisSpecimenName || test.HISSpecimenName || '';
      }
      this.form.patchValue(patch);
    }
  }

  onHisTestIdChanged() {
    if (this.isTestParameterScreen || this.isParameterMasterScreen) {
      this.onHisTestSelected();
    }
  }

  private validateTestParameterMappingDuplicate(item: any): string | null {
    if (this.apiName !== 'TestParameterMappingMaster') {
      return null;
    }

    const testId = +(item.hisTestId ?? item.HisTestId ?? 0);
    const paramId = +(item.hisParameterId ?? item.HisParameterId ?? 0);
    if (!testId || !paramId) {
      return null;
    }

    const excludeId = this.id ? +this.id : 0;
    const duplicate = (this.existingTestParameterMappings || []).find(m => {
      const mId = +(m.id ?? m.Id ?? 0);
      const mTestId = +(m.hisTestId ?? m.HisTestId ?? 0);
      const mParamId = +(m.hisParameterId ?? m.HisParameterId ?? 0);
      const active = m.isActive ?? m.IsActive;
      return active !== false && mTestId === testId && mParamId === paramId && mId !== excludeId;
    });

    if (!duplicate) {
      return null;
    }

    const test = this.findTestPickerOption(testId);
    const param = this.hisParameters.find(p => +p.id === paramId);
    const testLabel = test
      ? `${test.hisTestCode || test.HISTestCode} - ${test.hisTestCodeDescription || test.HISTestCodeDescription}`
      : 'selected test';
    const paramLabel = param
      ? `${param.hisParamCode || param.HISParamCode} - ${param.hisParamDescription || param.HISParamDescription}`
      : 'selected parameter';
    return `A mapping already exists for Test "${testLabel}" and Parameter "${paramLabel}".`;
  }

  private validateParameterDuplicate(item: any): string | null {
    if (this.apiName !== 'HisParameterMaster') {
      return null;
    }

    const code = ('' + (item.hisParamCode ?? item.HISParamCode ?? '')).trim();
    const description = ('' + (item.hisParamDescription ?? item.HISParamDescription ?? '')).trim();
    if (!code && !description) {
      return null;
    }

    const excludeId = this.id ? +this.id : 0;
    const candidates = (this.existingParameters || []).filter(p => {
      const pId = +(p.id ?? p.Id ?? 0);
      return pId !== excludeId;
    });

    const matchesCode = (p: any, value: string) => {
      const pCode = ('' + (p.hisParamCode ?? p.HISParamCode ?? '')).trim();
      return pCode && pCode.toLowerCase() === value.toLowerCase();
    };

    const matchesDescription = (p: any, value: string) => {
      const pDesc = ('' + (p.hisParamDescription ?? p.HISParamDescription ?? '')).trim();
      return pDesc && pDesc.toLowerCase() === value.toLowerCase();
    };

    if (code && candidates.some(p => matchesCode(p, code))) {
      return this.parameterCodeExistsMessage;
    }

    if (description && candidates.some(p => matchesDescription(p, description))) {
      return this.parameterDescriptionExistsMessage;
    }

    if (code && description && candidates.some(p => matchesCode(p, code) && matchesDescription(p, description))) {
      return this.parameterCombinationExistsMessage;
    }

    return null;
  }

  loadParameterCatalog(afterLoad?: () => void) {
    this.masterService.getItems('HisParameterMaster', {
      RecordPerPage: 500,
      CurrentPage: 1,
      SortColumnName: 'HISParamCode',
      SortDirection: true
    }).subscribe(r => {
      const items = r?.items || r?.Items || [];
      const seen = new Set<string>();
      this.parameterOptions = [];
      for (const p of items) {
        const code = ('' + (p.hisParamCode || p.HISParamCode || '')).trim();
        if (!code) {
          continue;
        }
        const key = code.toLowerCase();
        if (seen.has(key)) {
          continue;
        }
        seen.add(key);
        this.parameterOptions.push(p);
      }
      if (afterLoad) {
        afterLoad();
      }
    }, () => {
      this.parameterOptions = [];
      if (afterLoad) {
        afterLoad();
      }
    });
  }

  onParameterTemplateSelected() {
    const pickerId = this.form.get('hisParameterPicker')?.value;
    const param = this.parameterOptions.find((p: any) => +p.id === +pickerId);
    if (!param) {
      return;
    }
    this.form.patchValue({
      hisParamCode: param.hisParamCode || param.HISParamCode,
      hisParamDescription: param.hisParamDescription || param.HISParamDescription,
      hisParamUnit: param.hisParamUnit || param.HISParamUnit || '',
      hisParamMethod: param.hisParamMethod || param.HISParamMethod || '',
      lisParamCode: param.lisParamCode || param.LISParamCode || param.hisParamCode || param.HISParamCode
    });
    this.lockParameterDerivedFields(true);
  }

  private lockParameterDerivedFields(includeIdentityFields = true) {
    const names = includeIdentityFields
      ? ['hisParamCode', 'hisParamDescription']
      : [];
    names.forEach(name => {
      const control = this.form.get(name);
      if (control) {
        control.disable({ emitEvent: false });
      }
    });
  }

  private syncHisParameterPicker(item: any) {
    if (this.apiName !== 'TestMappingMaster' || !item?.hisParamCode) {
      return;
    }

    const param = this.hisParameters.find(p =>
      (p.hisParamCode || p.HISParamCode) === item.hisParamCode);
    if (param) {
      this.form.patchValue({ hisParameterPicker: param.id });
      this.onHisParameterSelected();
    }
  }

  private syncTestParameterPickers(item: any) {
    if (this.apiName !== 'TestParameterMappingMaster') {
      return;
    }
    const testId = item?.hisTestId ?? item?.HisTestId;
    const paramId = item?.hisParameterId ?? item?.HisParameterId;
    if (testId) {
      this.ensureTestParameterTestOption(testId);
      this.form.patchValue({ hisTestPicker: testId, hisTestId: testId });
    }
    if (paramId) {
      this.form.patchValue({ hisParameterPicker: paramId, hisParameterId: paramId });
    }
  }

  private syncHisTestPicker(item: any) {
    if (this.apiName !== 'TestParameterMappingMaster') {
      return;
    }
    const testId = item?.hisTestId ?? item?.HisTestId;
    if (testId) {
      this.form.patchValue({ hisTestPicker: testId, hisTestId: testId });
    }
  }

  private syncParameterPicker(item: any) {
    if (!this.isParameterPickerScreen) {
      return;
    }
    const code = item?.hisParamCode || item?.HISParamCode;
    if (!code) {
      return;
    }
    const match = this.parameterOptions.find(p =>
      (p.hisParamCode || p.HISParamCode) === code);
    if (match) {
      this.form.patchValue({ hisParameterPicker: match.id });
      this.onParameterTemplateSelected();
    }
  }

  private httpEquipmentList(): Observable<any[]> {
    return this.masterService.getEquipments();
  }

  private loadTestRateLookups() {
    forkJoin({
      corporates: this.masterService.getLookupList('Corporate'),
      doctors: this.masterService.getLookupList('ReferralDoctor'),
      profiles: this.masterService.getLookupList('TestProfile')
    }).subscribe(
      data => {
        this.corporates = (data.corporates || []).filter(c => c.isActive !== false);
        this.doctors = (data.doctors || []).filter(d => d.isActive !== false);
        this.profiles = (data.profiles || []).filter(p => p.isActive !== false);
        this.lookupsLoaded = true;

        if (this.id) {
          this.masterService.getItem(this.apiName, this.id).subscribe(item => {
            if (item) {
              this.patchItem(item);
              this.ensureTestRateOption(item);
            }
          });
        } else {
          const now = new Date();
          const nextYear = new Date(now.getFullYear() + 1, now.getMonth(), now.getDate());
          this.form.patchValue({
            effectiveStart: this.toDateInput(now),
            effectiveEnd: this.toDateInput(nextYear),
            rateType: 0
          });
        }
      },
      () => {
        this.alertService.error('Failed to load lookup data for Test Rate form.');
      }
    );
  }

  onTestRateDropdownOpen() {
    if (!this.testRateOptions.length) {
      this.searchTestRateOptions('');
    }
  }

  onTestParameterTestDropdownOpen() {
    if (!this.testParameterTestOptions.length) {
      this.searchTestParameterTestOptions('');
    }
  }

  onTestParameterTestSearch(event: { term: string }) {
    const term = (event?.term || '').trim();
    if (this.testParameterTestSearchTimer) {
      clearTimeout(this.testParameterTestSearchTimer);
    }
    this.testParameterTestSearchTimer = setTimeout(() => this.searchTestParameterTestOptions(term), 250);
  }

  private searchTestParameterTestOptions(term: string) {
    this.testParameterTestSearchLoading = true;
    this.masterService.searchHisTests(term, 50).subscribe(
      rows => {
        this.testParameterTestOptions = (rows || []).map(t => this.toTestRateOption(t));
        this.testParameterTestSearchLoading = false;
      },
      () => {
        this.testParameterTestSearchLoading = false;
        this.testParameterTestOptions = [];
      }
    );
  }

  private ensureTestParameterTestOption(testId: any) {
    if (!testId) {
      return;
    }
    const existing = this.testParameterTestOptions.find(o => +o.id === +testId);
    if (existing) {
      return;
    }
    this.masterService.getItem('HisTest', testId).subscribe(test => {
      if (test) {
        const option = this.toTestRateOption(test);
        this.testParameterTestOptions = [option, ...this.testParameterTestOptions];
      }
    });
  }

  private findTestPickerOption(testId: any): any {
    if (!testId) {
      return null;
    }
    const id = +testId;
    return this.testParameterTestOptions.find(t => +t.id === id)
      || this.tests.find(t => +t.id === id)
      || this.testRateOptions.find(t => +t.id === id);
  }

  onTestRateSearch(event: { term: string }) {
    const term = (event?.term || '').trim();
    if (this.testRateSearchTimer) {
      clearTimeout(this.testRateSearchTimer);
    }
    this.testRateSearchTimer = setTimeout(() => this.searchTestRateOptions(term), 250);
  }

  private searchTestRateOptions(term: string) {
    this.testRateSearchLoading = true;
    this.masterService.searchHisTests(term, 50).subscribe(
      rows => {
        this.testRateOptions = (rows || []).map(t => this.toTestRateOption(t));
        this.testRateSearchLoading = false;
      },
      () => {
        this.testRateSearchLoading = false;
        this.testRateOptions = [];
      }
    );
  }

  private toTestRateOption(t: any) {
    const id = t.id ?? t.Id;
    const code = t.hisTestCode || t.HISTestCode || '';
    const desc = t.hisTestCodeDescription || t.HISTestCodeDescription || '';
    return { id, hisTestCode: code, hisTestCodeDescription: desc, displayLabel: `${code} - ${desc}`.trim() };
  }

  private toParameterPickerOption(p: any) {
    const id = p.id ?? p.Id;
    const code = p.hisParamCode || p.HISParamCode || '';
    const desc = p.hisParamDescription || p.HISParamDescription || '';
    return { id, hisParamCode: code, hisParamDescription: desc, displayLabel: `${code} - ${desc}`.trim() };
  }

  private ensureHisTestOption(testId: any) {
    if (!testId) {
      return;
    }
    const existing = this.testRateOptions.find(o => +o.id === +testId);
    if (existing) {
      return;
    }
    this.masterService.getItem('HisTest', testId).subscribe(test => {
      if (test) {
        const option = this.toTestRateOption(test);
        this.testRateOptions = [option, ...this.testRateOptions];
      }
    });
  }

  private ensureTestRateOption(item: any) {
    this.ensureHisTestOption(item?.testId ?? item?.TestId);
  }

  private findHisTestById(testId: any): any {
    if (!testId) {
      return null;
    }
    const id = +testId;
    return this.testRateOptions.find(t => +t.id === id)
      || this.tests.find(t => +t.id === id);
  }

  onRateTypeChange() {
    const rt = this.rateType;
    if (rt !== 1) {
      this.form.patchValue({ corporateId: null });
    }
    if (rt !== 2) {
      this.form.patchValue({ referralDoctorId: null });
    }
    if (rt !== 3) {
      this.form.patchValue({ testProfileId: null });
    }
  }

  patchItem(item: any) {
    const patch: any = Object.assign({}, item);
    if (patch.Code != null && patch.code == null) { patch.code = patch.Code; }
    if (patch.Name != null && patch.name == null) { patch.name = patch.Name; }
    if (patch.Phone != null && patch.phone == null) { patch.phone = patch.Phone; }
    if (patch.Email != null && patch.email == null) { patch.email = patch.Email; }
    if (patch.Address != null && patch.address == null) { patch.address = patch.Address; }
    if (patch.ProcessingCategory != null && patch.processingCategory == null) { patch.processingCategory = patch.ProcessingCategory; }
    if (patch.effectiveStart) { patch.effectiveStart = this.toDateInput(patch.effectiveStart); }
    if (patch.effectiveEnd) { patch.effectiveEnd = this.toDateInput(patch.effectiveEnd); }
    if (patch.dateOfBirth) { patch.dateOfBirth = this.toDateInput(patch.dateOfBirth); }
    if (patch.testId != null) { patch.testId = +patch.testId; }
    if (patch.rateType != null) { patch.rateType = +patch.rateType; }
    if (patch.corporateId != null) { patch.corporateId = +patch.corporateId; }
    if (patch.referralDoctorId != null) { patch.referralDoctorId = +patch.referralDoctorId; }
    if (patch.testProfileId != null) { patch.testProfileId = +patch.testProfileId; }
    if (patch.hisTestId != null) { patch.hisTestId = +patch.hisTestId; }
    if (patch.HISTestId != null && patch.hisTestId == null) { patch.hisTestId = +patch.HISTestId; }
    if (patch.hisParameterId != null) { patch.hisParameterId = +patch.hisParameterId; }
    if (patch.equipmentId != null) { patch.equipmentId = +patch.equipmentId; }
    if (patch.isActive != null) { patch.isActive = this.coerceBool(patch.isActive); }
    if (patch.IsActive != null) { patch.isActive = this.coerceBool(patch.IsActive); }
    if (patch.HISParamCode != null && patch.hisParamCode == null) { patch.hisParamCode = patch.HISParamCode; }
    if (patch.HISParamDescription != null && patch.hisParamDescription == null) { patch.hisParamDescription = patch.HISParamDescription; }
    if (patch.HISParamUnit != null && patch.hisParamUnit == null) { patch.hisParamUnit = patch.HISParamUnit; }
    if (patch.HISParamMethod != null && patch.hisParamMethod == null) { patch.hisParamMethod = patch.HISParamMethod; }
    if (patch.LISParamCode != null && patch.lisParamCode == null) { patch.lisParamCode = patch.LISParamCode; }
    if (patch.Comments != null && patch.comments == null) { patch.comments = patch.Comments; }
    if (patch.MRNo != null && patch.mrNo == null) { patch.mrNo = patch.MRNo; }
    if (patch.VisitId != null && patch.visitId == null) { patch.visitId = patch.VisitId; }
    if (patch.PatientPrefix != null && patch.patientPrefix == null) { patch.patientPrefix = patch.PatientPrefix; }
    if (patch.HISRangeCode != null && patch.hisRangeCode == null) { patch.hisRangeCode = patch.HISRangeCode; }
    if (patch.HISRangeValue != null && patch.hisRangeValue == null) { patch.hisRangeValue = patch.HISRangeValue; }
    if (patch.AgeFrom != null && patch.ageFrom == null) { patch.ageFrom = patch.AgeFrom; }
    if (patch.AgeTo != null && patch.ageTo == null) { patch.ageTo = patch.AgeTo; }
    if (patch.AgeType != null && patch.ageType == null) { patch.ageType = patch.AgeType; }
    if (patch.MinValue != null && patch.minValue == null) { patch.minValue = patch.MinValue; }
    if (patch.MaxValue != null && patch.maxValue == null) { patch.maxValue = patch.MaxValue; }
    if (patch.Gender != null && patch.gender == null) { patch.gender = patch.Gender; }
    patch.gender = this.apiName === 'PatientMaster'
      ? this.normalizePatientGenderForForm(patch.gender)
      : this.normalizeGenderForForm(patch.gender);
    if (this.isParameterMasterScreen) {
      if (patch.hisParamUnit) {
        patch.hisParamUnit = this.matchLookupName(patch.hisParamUnit, this.units);
      }
      if (patch.hisParamMethod) {
        patch.hisParamMethod = this.matchLookupName(patch.hisParamMethod, this.methods);
      }
    }
    this.form.patchValue(patch);
  }

  onPatientAgeChange(): void {
    if (this.apiName !== 'PatientMaster') {
      return;
    }
    const age = +this.form.get('age')?.value;
    if (!age || age <= 0 || isNaN(age)) {
      return;
    }
    const dob = new Date();
    dob.setHours(0, 0, 0, 0);
    dob.setFullYear(dob.getFullYear() - Math.floor(age));
    this.form.patchValue({ dateOfBirth: this.toDateInput(dob) }, { emitEvent: false });
  }

  onPatientDobChange(): void {
    if (this.apiName !== 'PatientMaster') {
      return;
    }
    const dob = this.parseLocalDate(this.form.get('dateOfBirth')?.value);
    if (!dob) {
      return;
    }
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    let age = today.getFullYear() - dob.getFullYear();
    const monthDiff = today.getMonth() - dob.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
      age--;
    }
    if (age >= 0) {
      this.form.patchValue({ age }, { emitEvent: false });
    }
  }

  private normalizePatientGenderForForm(gender: string): string {
    if (!gender) {
      return '';
    }
    const g = ('' + gender).trim().toUpperCase();
    if (g === 'M' || g === 'MALE') {
      return 'M';
    }
    if (g === 'F' || g === 'FEMALE') {
      return 'F';
    }
    if (g === 'O' || g === 'OTHER') {
      return 'O';
    }
    return gender;
  }

  private normalizeGenderForForm(gender: string): string {
    if (!gender) {
      return '';
    }
    const g = ('' + gender).trim().toUpperCase();
    if (g === 'M' || g === 'MALE') {
      return 'Male';
    }
    if (g === 'F' || g === 'FEMALE') {
      return 'Female';
    }
    if (g === 'B' || g === 'BOTH') {
      return 'Both';
    }
    return gender;
  }

  private coerceBool(value: any): boolean {
    if (value === true || value === 1) {
      return true;
    }
    if (value === false || value === 0) {
      return false;
    }
    if (value == null) {
      return false;
    }
    const normalized = ('' + value).trim().toLowerCase();
    return normalized === 'true' || normalized === '1' || normalized === 'yes';
  }

  private matchLookupName(value: string, options: any[]): string {
    if (!value || !options?.length) {
      return value || '';
    }
    const trimmed = ('' + value).trim();
    const exact = options.find(o => (o.name || o.Name) === trimmed);
    if (exact) {
      return exact.name || exact.Name;
    }
    const ci = options.find(o => (o.name || o.Name || '').toLowerCase() === trimmed.toLowerCase());
    return ci ? (ci.name || ci.Name) : trimmed;
  }

  onSubmit() {
    this.submitted = true;
    if (this.apiName === 'TestRate') {
      this.applyTestRateValidators();
    }
    if (this.apiName === 'TestMappingMaster') {
      this.onHisParameterSelected();
      const picker = this.form.get('hisParameterPicker');
      if (!picker?.value) {
        picker?.setErrors({ required: true });
        return;
      }
    }
    if (this.apiName === 'TestParameterMappingMaster') {
      this.onHisTestSelected();
      this.onHisParameterSelected();
      const testPicker = this.form.get('hisTestPicker');
      const paramPicker = this.form.get('hisParameterPicker');
      if (!testPicker?.value) {
        testPicker?.setErrors({ required: true });
        return;
      }
      if (!paramPicker?.value) {
        paramPicker?.setErrors({ required: true });
        return;
      }
      this.form.patchValue({
        hisTestId: +testPicker.value,
        hisParameterId: +paramPicker.value
      });
    }
    if (this.form.invalid) { return; }

    let item = Object.assign({}, this.form.getRawValue());
    if (this.apiName === 'TestParameterMappingMaster') {
      item.hisTestId = +this.form.get('hisTestPicker').value;
      item.hisParameterId = +this.form.get('hisParameterPicker').value;
    }
    if (item.code) { item.code = ('' + item.code).trim(); }
    if (item.name) { item.name = ('' + item.name).trim(); }
    if (item.hisParamCode) { item.hisParamCode = ('' + item.hisParamCode).trim(); }
    if (item.comments != null) { item.comments = '' + item.comments; }
    if (item.effectiveStart) { item.effectiveStart = this.toApiDateString(item.effectiveStart); }
    if (item.effectiveEnd) { item.effectiveEnd = this.toApiDateString(item.effectiveEnd); }
    if (item.dateOfBirth) { item.dateOfBirth = this.toApiDateString(item.dateOfBirth); }
    if (this.apiName === 'PatientMaster' && item.gender) {
      item.gender = this.normalizePatientGenderForForm(item.gender);
    }
    if (this.apiName === 'TestMappingMaster') {
      const eq = this.equipments.find(e => +e.id === +item.equipmentId);
      if (eq) {
        item.groupName = eq.name || eq.groupName;
      }
      delete item.hisParameterPicker;
    }
    if (this.apiName === 'TestParameterMappingMaster') {
      delete item.hisTestPicker;
      delete item.hisParameterPicker;
    }
    if (this.apiName === 'PatientMaster' && !('' + (item.phone || '')).trim()) {
      this.alertService.error('Phone number is required.');
      return;
    }
    if (this.apiName === 'PatientMaster') {
      item.patientPrefix = ('' + (item.patientPrefix || '')).trim();
      item.mrNo = ('' + (item.mrNo || '')).trim();
      item.visitId = ('' + (item.visitId || '')).trim();
      if (!item.patientPrefix) {
        this.alertService.error('Salutation is required.');
        return;
      }
      // On create, MR No / Visit ID are auto-generated by API when blank.
      if (this.id) {
        if (!item.mrNo) {
          this.alertService.error('MR No is required.');
          return;
        }
        if (!item.visitId) {
          this.alertService.error('Visit ID is required.');
          return;
        }
      } else {
        if (!item.mrNo) { delete item.mrNo; }
        if (!item.visitId) { delete item.visitId; }
      }
      if (!this.id && !('' + (item.hisPatientId || '')).trim()) {
        delete item.hisPatientId;
      }
    }
    if (this.isParameterMasterScreen) {
      if (!item.hisParamCode) {
        item.hisParamCode = `P-${Date.now().toString(36).toUpperCase()}`;
      }
      if (!item.lisParamCode) {
        item.lisParamCode = item.hisParamCode;
      }
    }

    const parameterDuplicateError = this.validateParameterDuplicate(item);
    if (parameterDuplicateError) {
      this.alertService.error(parameterDuplicateError);
      return;
    }

    const testParameterDuplicateError = this.validateTestParameterMappingDuplicate(item);
    if (testParameterDuplicateError) {
      this.alertService.error(testParameterDuplicateError);
      return;
    }

    if (this.apiName === 'TestRate') {
      const rt = +item.rateType;
      if (rt !== 1) { item.corporateId = null; }
      if (rt !== 2) { item.referralDoctorId = null; }
      if (rt !== 3) { item.testProfileId = null; }
    }
    if (this.id) {
      if (this.apiName === 'Department') {
        item.code = this.id;
      } else {
        item.id = +this.id;
      }
    } else if (item.isActive === undefined || item.isActive === null) {
      item.isActive = true;
    }

    this.loading = true;
    const req = this.id
      ? this.masterService.editItem(this.apiName, item)
      : this.masterService.addItem(this.apiName, item);

    req.subscribe(
      (res) => {
        this.loading = false;
        this.alertService.success('Saved successfully');
        if (this.apiName === 'PatientMaster' && !this.id) {
          const newPatientId = res?.result ?? res?.Result;
          if (newPatientId) {
            this.router.navigate(['/sale-invoices/create'], { state: { patientId: +newPatientId } });
            return;
          }
        }
        this.router.navigate([this.returnUrl]);
      },
      err => {
        this.loading = false;
        this.alertService.error(extractApiError(err, 'Save failed'));
      }
    );
  }

  private applyTestRateValidators() {
    const corp = this.form.get('corporateId');
    const doc = this.form.get('referralDoctorId');
    const prof = this.form.get('testProfileId');
    corp.clearValidators();
    doc.clearValidators();
    prof.clearValidators();
    if (this.rateType === 1) {
      corp.setValidators([Validators.required]);
    } else if (this.rateType === 2) {
      doc.setValidators([Validators.required]);
    } else if (this.rateType === 3) {
      prof.setValidators([Validators.required]);
    }
    corp.updateValueAndValidity();
    doc.updateValueAndValidity();
    prof.updateValueAndValidity();
  }

  deactivate() {
    if (!this.id) { return; }
    const isDelete = this.apiName === 'HisParameterMaster' || this.apiName === 'Department';
    const msg = this.apiName === 'Department'
      ? 'Delete this department permanently?'
      : (isDelete
        ? 'Delete this test-parameter mapping? Remove parameter ranges first if delete is blocked.'
        : 'Deactivate this record?');
    if (!confirm(msg)) { return; }

    const payload = this.apiName === 'Department' ? { code: this.id } : { id: +this.id };
    this.loading = true;
    this.masterService.deleteItem(this.apiName, payload).subscribe(
      () => {
        this.loading = false;
        this.alertService.success(isDelete ? 'Mapping deleted' : 'Record deactivated');
        this.router.navigate([this.returnUrl]);
      },
      err => {
        this.loading = false;
        this.alertService.error(extractApiError(err, isDelete ? 'Delete failed' : 'Deactivate failed'));
      }
    );
  }

  cancel() {
    this.router.navigate([this.returnUrl]);
  }
  getFieldByName(name: string): any {
    return this.fields.find(f => f.name === name);
  }

  isFieldRequired(name: string): boolean {
    const field = this.getFieldByName(name);
    return field?.required === true;
  }
}

