import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
      group['testId'] = ['', Validators.required];
      group['rateType'] = [0];
      group['corporateId'] = [null];
      group['referralDoctorId'] = [null];
      group['testProfileId'] = [null];
      group['isActive'] = [true];
    }
    if (this.fields.find(f => f.name === 'isActive')) {
      group['isActive'] = [true];
    }
    this.form = this.fb.group(group);

    if (this.apiName === 'TestRate') {
      this.masterService.getAll('HisTest').subscribe(t => this.tests = t || []);
      this.masterService.getAll('Corporate').subscribe(c => this.corporates = c || []);
      this.masterService.getAll('ReferralDoctor').subscribe(d => this.doctors = d || []);
      this.masterService.getAll('TestProfile').subscribe(p => this.profiles = p || []);
    }

    if (this.id) {
      this.masterService.getItem(this.apiName, this.id).subscribe(item => {
        if (item) {
          this.patchItem(item);
        }
      });
    } else if (this.apiName === 'TestRate') {
      const now = new Date();
      const nextYear = new Date(now.getFullYear() + 1, now.getMonth(), now.getDate());
      this.form.patchValue({ effectiveStart: this.toDateInput(now), effectiveEnd: this.toDateInput(nextYear), rateType: 0 });
    }
  }

  get f() { return this.form.controls; }

  isInValid(controlName: string) {
    const c = this.form.get(controlName);
    return this.submitted && c && c.invalid;
  }

  toDateInput(d: Date | string) {
    if (!d) { return ''; }
    const dt = typeof d === 'string' ? new Date(d) : d;
    return dt.toISOString().substring(0, 10);
  }

  patchItem(item: any) {
    const patch: any = Object.assign({}, item);
    if (patch.effectiveStart) { patch.effectiveStart = this.toDateInput(patch.effectiveStart); }
    if (patch.effectiveEnd) { patch.effectiveEnd = this.toDateInput(patch.effectiveEnd); }
    this.form.patchValue(patch);
  }

  onSubmit() {
    this.submitted = true;
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
