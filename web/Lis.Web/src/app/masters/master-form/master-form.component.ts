import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AlertService, MasterService } from '../../_services';

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
  lookupsLoaded = false;

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
    if (this.apiName === 'HisParameterMaster') {
      group['hisTestId'] = [null, Validators.required];
    }
    if (this.apiName === 'HisParameterRangeMaster') {
      group['hisParameterId'] = [null, Validators.required];
    }
    if (this.apiName === 'TestMappingMaster') {
      group['equipmentId'] = [null, Validators.required];
      group['hisTestPicker'] = [null, Validators.required];
      group['hisTestCode'] = [''];
      group['hisTestCodeDescription'] = [''];
    }
    if (this.fields.find(f => f.name === 'isActive')) {
      group['isActive'] = [true];
    }
    this.form = this.fb.group(group);

    if (this.apiName === 'TestRate') {
      this.loadTestRateLookups();
    } else if (this.apiName === 'HisParameterMaster' || this.apiName === 'HisParameterRangeMaster' || this.apiName === 'TestMappingMaster') {
      this.loadSetupLookups();
    } else if (!this.id && this.apiName === 'PatientMaster') {
      this.masterService.getNextPatientId().subscribe(pid => {
        if (pid) {
          this.form.patchValue({ hisPatientId: pid });
        }
      });
    } else if (this.id) {
      this.masterService.getItem(this.apiName, this.id).subscribe(item => {
        if (item) {
          this.patchItem(item);
        }
      });
    }
  }

  isCodeReadonly(field: any): boolean {
    if (!field || field.name !== 'code' || !this.id) {
      return false;
    }
    return true;
  }

  visibleFields() {
    if (this.apiName === 'TestMappingMaster') {
      return this.fields.filter(f => f.name !== 'hisTestCode' && f.name !== 'hisTestCodeDescription');
    }
    return this.fields;
  }

  get f() { return this.form.controls; }

  get rateType(): number {
    return +this.form.get('rateType').value;
  }

  isInValid(controlName: string) {
    const c = this.form.get(controlName);
    return this.submitted && c && c.invalid;
  }

  toDateInput(d: Date | string) {
    if (!d) { return ''; }
    const dt = typeof d === 'string' ? new Date(d) : d;
    return dt.toISOString().substring(0, 10);
  }

  private loadSetupLookups() {
    const requests: { [key: string]: Observable<any> } = {
      tests: this.masterService.getLookupList('HisTest')
    };
    if (this.apiName === 'HisParameterRangeMaster') {
      requests.params = this.masterService.getItems('HisParameterMaster', {
        RecordPerPage: 500, CurrentPage: 1, SortColumnName: 'HISParamCode', SortDirection: true
      });
    }
    if (this.apiName === 'TestMappingMaster') {
      requests.equipments = this.httpEquipmentList();
    }

    forkJoin(requests).subscribe(
      (data: any) => {
        this.tests = data.tests || [];
        if (data.params) {
          this.hisParameters = (data.params.items || data.params.Items || data.params) || [];
        }
        if (data.equipments) {
          this.equipments = data.equipments || [];
        }
        this.lookupsLoaded = true;
        if (this.id) {
          this.masterService.getItem(this.apiName, this.id).subscribe(item => {
            if (item) {
              this.patchItem(item);
              this.syncHisTestPicker(item);
            }
          });
        }
      },
      () => this.alertService.error('Failed to load lookup data.')
    );
  }

  onHisTestSelected() {
    const testId = this.form.get('hisTestPicker').value;
    const test = this.tests.find(t => t.id === testId);
    if (test) {
      this.form.patchValue({
        hisTestCode: test.hisTestCode,
        hisTestCodeDescription: test.hisTestCodeDescription
      });
    }
  }

  private syncHisTestPicker(item: any) {
    if (this.apiName !== 'TestMappingMaster' || !item?.hisTestCode) {
      return;
    }

    const test = this.tests.find(t => t.hisTestCode === item.hisTestCode);
    if (test) {
      this.form.patchValue({ hisTestPicker: test.id });
    }
  }

  private httpEquipmentList(): Observable<any[]> {
    return this.masterService.getEquipments();
  }

  private loadTestRateLookups() {
    forkJoin({
      tests: this.masterService.getLookupList('HisTest'),
      corporates: this.masterService.getLookupList('Corporate'),
      doctors: this.masterService.getLookupList('ReferralDoctor'),
      profiles: this.masterService.getLookupList('TestProfile')
    }).subscribe(
      data => {
        this.tests = data.tests || [];
        this.corporates = (data.corporates || []).filter(c => c.isActive !== false);
        this.doctors = (data.doctors || []).filter(d => d.isActive !== false);
        this.profiles = (data.profiles || []).filter(p => p.isActive !== false);
        this.lookupsLoaded = true;

        if (this.id) {
          this.masterService.getItem(this.apiName, this.id).subscribe(item => {
            if (item) {
              this.patchItem(item);
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
    if (patch.effectiveStart) { patch.effectiveStart = this.toDateInput(patch.effectiveStart); }
    if (patch.effectiveEnd) { patch.effectiveEnd = this.toDateInput(patch.effectiveEnd); }
    if (patch.dateOfBirth) { patch.dateOfBirth = this.toDateInput(patch.dateOfBirth); }
    if (patch.testId != null) { patch.testId = +patch.testId; }
    if (patch.rateType != null) { patch.rateType = +patch.rateType; }
    if (patch.corporateId != null) { patch.corporateId = +patch.corporateId; }
    if (patch.referralDoctorId != null) { patch.referralDoctorId = +patch.referralDoctorId; }
    if (patch.testProfileId != null) { patch.testProfileId = +patch.testProfileId; }
    if (patch.hisTestId != null) { patch.hisTestId = +patch.hisTestId; }
    if (patch.hisParameterId != null) { patch.hisParameterId = +patch.hisParameterId; }
    if (patch.equipmentId != null) { patch.equipmentId = +patch.equipmentId; }
    if (patch.isActive != null) { patch.isActive = !!patch.isActive; }
    this.form.patchValue(patch);
  }

  onSubmit() {
    this.submitted = true;
    if (this.apiName === 'TestRate') {
      this.applyTestRateValidators();
    }
    if (this.apiName === 'TestMappingMaster') {
      this.onHisTestSelected();
      const picker = this.form.get('hisTestPicker');
      if (!picker?.value) {
        picker?.setErrors({ required: true });
        return;
      }
    }
    if (this.form.invalid) { return; }

    let item = Object.assign({}, this.form.value);
    if (item.effectiveStart) { item.effectiveStart = new Date(item.effectiveStart); }
    if (item.effectiveEnd) { item.effectiveEnd = new Date(item.effectiveEnd); }
    if (item.dateOfBirth) { item.dateOfBirth = new Date(item.dateOfBirth); }
    if (this.apiName === 'TestMappingMaster') {
      const eq = this.equipments.find(e => +e.id === +item.equipmentId);
      if (eq) {
        item.groupName = eq.name || eq.groupName;
      }
      delete item.hisTestPicker;
    }
    if (this.apiName === 'PatientMaster' && !item.hisPatientId) {
      this.alertService.error('Patient ID is required.');
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
      () => {
        this.loading = false;
        this.alertService.success('Saved successfully');
        this.router.navigate([this.returnUrl]);
      },
      err => {
        this.loading = false;
        const body = err?.error;
        const msg = typeof body === 'string' ? body : (body?.message || body?.Message || body?.error || 'Save failed');
        this.alertService.error(msg);
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
    const isDelete = this.apiName === 'HisParameterMaster';
    const msg = isDelete
      ? 'Delete this test-parameter mapping? Remove parameter ranges first if delete is blocked.'
      : 'Deactivate this record?';
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
        const apiMsg = typeof err?.error === 'string' ? err.error : err?.error?.message;
        this.alertService.error(apiMsg || (isDelete ? 'Delete failed' : 'Deactivate failed'));
      }
    );
  }

  cancel() {
    this.router.navigate([this.returnUrl]);
  }
}
