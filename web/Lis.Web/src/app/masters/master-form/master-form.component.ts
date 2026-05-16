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
    }
    if (this.fields.find(f => f.name === 'isActive')) {
      group['isActive'] = [true];
    }
    this.form = this.fb.group(group);

    if (this.apiName === 'TestRate') {
      this.loadTestRateLookups();
    } else if (this.apiName === 'HisParameterMaster' || this.apiName === 'HisParameterRangeMaster' || this.apiName === 'TestMappingMaster') {
      this.loadSetupLookups();
    } else if (this.id) {
      this.masterService.getItem(this.apiName, this.id).subscribe(item => {
        if (item) {
          this.patchItem(item);
        }
      });
    }
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
            if (item) { this.patchItem(item); }
          });
        }
      },
      () => this.alertService.error('Failed to load lookup data.')
    );
  }

  private httpEquipmentList(): Observable<any[]> {
    return this.masterService.getItems('EquipmentHeartbeat', {}).pipe(
      map((list: any) => Array.isArray(list) ? list : [])
    );
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
    if (patch.testId != null) { patch.testId = +patch.testId; }
    if (patch.rateType != null) { patch.rateType = +patch.rateType; }
    if (patch.corporateId != null) { patch.corporateId = +patch.corporateId; }
    if (patch.referralDoctorId != null) { patch.referralDoctorId = +patch.referralDoctorId; }
    if (patch.testProfileId != null) { patch.testProfileId = +patch.testProfileId; }
    if (patch.hisTestId != null) { patch.hisTestId = +patch.hisTestId; }
    if (patch.hisParameterId != null) { patch.hisParameterId = +patch.hisParameterId; }
    if (patch.equipmentId != null) { patch.equipmentId = +patch.equipmentId; }
    this.form.patchValue(patch);
  }

  onSubmit() {
    this.submitted = true;
    if (this.apiName === 'TestRate') {
      this.applyTestRateValidators();
    }
    if (this.form.invalid) { return; }

    const item = Object.assign({}, this.form.value);
    if (item.effectiveStart) { item.effectiveStart = new Date(item.effectiveStart); }
    if (item.effectiveEnd) { item.effectiveEnd = new Date(item.effectiveEnd); }
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
        this.alertService.error(err?.error?.message || 'Save failed');
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
    if (!confirm('Deactivate this record?')) { return; }

    const payload = this.apiName === 'Department' ? { code: this.id } : { id: +this.id };
    this.loading = true;
    this.masterService.deleteItem(this.apiName, payload).subscribe(
      () => {
        this.loading = false;
        this.alertService.success('Record deactivated');
        this.router.navigate([this.returnUrl]);
      },
      () => {
        this.loading = false;
        this.alertService.error('Deactivate failed');
      }
    );
  }

  cancel() {
    this.router.navigate([this.returnUrl]);
  }
}
