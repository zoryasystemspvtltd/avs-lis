import { Component, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AlertService, MasterService } from '../../_services';

@Component({
  selector: 'app-test-profile-form',
  templateUrl: './test-profile-form.component.html'
})
export class TestProfileFormComponent implements OnInit {
  form: FormGroup;
  submitted = false;
  loading = false;
  id: string;
  tests: any[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private masterService: MasterService,
    private alertService: AlertService) { }

  ngOnInit() {
    this.id = this.route.snapshot.params['id'];
    this.form = this.fb.group({
      id: [0],
      code: ['', Validators.required],
      name: ['', Validators.required],
      packageRate: [0],
      isActive: [true],
      lines: this.fb.array([])
    });

    this.masterService.getLookupList('HisTest').subscribe(t => this.tests = t || []);

    if (this.id) {
      this.masterService.getItem('TestProfile', this.id).subscribe(profile => {
        if (profile) {
          this.form.patchValue(profile);
          const details = profile.profileDetails || profile.ProfileDetails || [];
          details.forEach(line => this.addLine(line));
        }
      });
    }
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }

  addLine(line?: any) {
    this.lines.push(this.fb.group({
      id: [line?.id || 0],
      testId: [line?.testId || '', Validators.required],
      quantity: [line?.quantity || 1, [Validators.required, Validators.min(1)]]
    }));
  }

  removeLine(i: number) {
    this.lines.removeAt(i);
    this.recalcPackageRate();
  }

  onTestChange(i: number) {
    const testId = +this.lines.at(i).get('testId').value;
    if (!testId) { return; }

    const duplicate = this.lines.controls.some((c, idx) => idx !== i && +c.value.testId === testId);
    if (duplicate) {
      this.alertService.error('Test already added to this profile');
      this.lines.at(i).patchValue({ testId: '' });
      return;
    }
  }

  recalcPackageRate() {
    const controls = this.lines.controls.filter(c => +c.get('testId').value);
    if (controls.length === 0) {
      this.form.patchValue({ packageRate: 0 }, { emitEvent: false });
      return;
    }

    let total = 0;
    let completed = 0;
    controls.forEach(ctrl => {
      const testId = +ctrl.get('testId').value;
      const qty = +ctrl.get('quantity').value || 1;
      this.masterService.getEffectiveRate(testId, 0).subscribe(rate => {
        total += (rate?.rate || 0) * qty;
        completed++;
        if (completed === controls.length) {
          this.form.patchValue({ packageRate: Math.round(total * 100) / 100 }, { emitEvent: false });
        }
      });
    });
  }

  isInValid(controlName: string) {
    const c = this.form.get(controlName);
    return this.submitted && c && c.invalid;
  }

  onSubmit() {
    this.submitted = true;
    if (this.form.invalid || this.lines.length === 0) {
      if (this.lines.length === 0) {
        this.alertService.error('Add at least one test to the profile');
      }
      return;
    }

    const val = this.form.value;
    const profile = {
      id: this.id ? +this.id : (val.id || 0),
      code: val.code,
      name: val.name,
      packageRate: val.packageRate,
      isActive: val.isActive,
      ProfileDetails: val.lines.map(l => ({
        id: l.id || 0,
        testId: +l.testId,
        quantity: +l.quantity
      }))
    };

    this.loading = true;
    this.masterService.addItem('TestProfile', profile).subscribe(
      () => {
        this.loading = false;
        this.alertService.success('Profile saved successfully');
        this.router.navigate(['/test-profiles']);
      },
      err => {
        this.loading = false;
        this.alertService.error(err?.error?.message || 'Save failed');
      }
    );
  }

  deactivate() {
    if (!this.id) { return; }
    if (!confirm('Deactivate this test profile?')) { return; }
    this.masterService.deleteItem('TestProfile', { id: +this.id }).subscribe(
      () => {
        this.alertService.success('Profile deactivated');
        this.router.navigate(['/test-profiles']);
      },
      () => this.alertService.error('Deactivate failed')
    );
  }

  cancel() {
    this.router.navigate(['/test-profiles']);
  }
}
