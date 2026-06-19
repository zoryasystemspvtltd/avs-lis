import { Component, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AlertService, MasterService } from '../../_services';
import { extractApiError } from '../../_helpers/api-error';

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
  allParameters: any[] = [];
  allRanges: any[] = [];
  previewTests: any[] = [];

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

    const paramLoad$ = forkJoin({
      parameters: this.masterService.getItems('HisParameterMaster', {
        RecordPerPage: 2000, CurrentPage: 1, SortColumnName: 'HISParamCode', SortDirection: true
      }),
      ranges: this.masterService.getItems('HisParameterRangeMaster', {
        RecordPerPage: 5000, CurrentPage: 1, SortColumnName: 'HISRangeCode', SortDirection: true
      })
    });

    if (this.id) {
      forkJoin({
        tests: this.masterService.getLookupList('HisTest'),
        profile: this.masterService.getProfileHierarchy(this.id),
        catalog: paramLoad$
      }).subscribe(data => {
        this.tests = this.activeTestsOnly(data.tests);
        this.allParameters = data.catalog.parameters?.items || data.catalog.parameters?.Items || [];
        this.allRanges = data.catalog.ranges?.items || data.catalog.ranges?.Items || [];
        const profile = data.profile;
        if (profile) {
          this.form.patchValue({
            id: profile.id,
            code: profile.code,
            name: profile.name,
            packageRate: profile.packageRate,
            isActive: profile.isActive !== false
          });
          let details = profile.profileDetails || profile.ProfileDetails || [];
          if (!details.length && (profile.tests || []).length) {
            details = profile.tests.map((t: any) => ({
              id: 0,
              testId: t.testId,
              quantity: t.quantity || 1
            }));
          }
          details.forEach(line => this.addLine(line));
          this.previewTests = profile.tests || [];
          this.refreshParameterPreview();
        }
      });
    } else {
      forkJoin({
        tests: this.masterService.getLookupList('HisTest'),
        catalog: paramLoad$
      }).subscribe(data => {
        this.tests = this.activeTestsOnly(data.tests);
        this.allParameters = data.catalog.parameters?.items || data.catalog.parameters?.Items || [];
        this.allRanges = data.catalog.ranges?.items || data.catalog.ranges?.Items || [];
      });
      this.masterService.getItems('TestProfile', {
        RecordPerPage: 1, CurrentPage: 1, SortColumnName: 'Code', SortDirection: false
      }).subscribe(list => {
        const total = list?.totalRecord || list?.TotalRecord || 0;
        const next = total + 1;
        this.form.patchValue({ code: `PKG${String(next).padStart(4, '0')}` });
      });
    }
  }

  private activeTestsOnly(tests: any[]): any[] {
    return (tests || []).filter(t => t.isActive !== false && t.IsActive !== false);
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }

  addLine(line?: any) {
    this.lines.push(this.fb.group({
      id: [line?.id || 0],
      testId: [line?.testId ? +line.testId : null, Validators.required],
      quantity: [line?.quantity || 1, [Validators.required, Validators.min(1)]]
    }));
    this.refreshParameterPreview();
  }

  removeLine(i: number) {
    this.lines.removeAt(i);
    this.recalcPackageRate();
    this.refreshParameterPreview();
  }

  onTestChange(i: number) {
    const testId = +this.lines.at(i).get('testId').value;
    if (!testId) {
      this.refreshParameterPreview();
      return;
    }

    const test = this.tests.find(t => +t.id === testId);
    if (!test) {
      this.alertService.error('Selected test is inactive or unavailable');
      this.lines.at(i).patchValue({ testId: null });
      this.refreshParameterPreview();
      return;
    }

    const duplicate = this.lines.controls.some((c, idx) => idx !== i && +c.value.testId === testId);
    if (duplicate) {
      this.alertService.error('Test already added to this profile');
      this.lines.at(i).patchValue({ testId: null });
      this.refreshParameterPreview();
      return;
    }

    this.recalcPackageRate();
    this.refreshParameterPreview();
  }

  refreshParameterPreview() {
    const testIds = this.lines.controls
      .map(c => +c.get('testId').value)
      .filter(id => id > 0);

    this.previewTests = testIds.map(testId => {
      const test = this.tests.find(t => +t.id === testId);
      const params = this.allParameters.filter(p => +(p.hisTestId || p.HisTestId) === testId);
      return {
        testId,
        quantity: this.lines.controls.find(c => +c.value.testId === testId)?.value?.quantity || 1,
        testCode: test?.hisTestCode || test?.HISTestCode,
        testName: test?.hisTestCodeDescription || test?.HISTestCodeDescription,
        parameters: params.map(p => ({
          paramCode: p.hisParamCode || p.HISParamCode,
          description: p.hisParamDescription || p.HISParamDescription,
          unit: p.hisParamUnit || p.HISParamUnit,
          method: p.hisParamMethod || p.HISParamMethod,
          ranges: this.allRanges
            .filter(r => +(r.hisParameterId || r.HisParameterId) === +(p.id || p.Id))
            .map(r => ({
              rangeCode: r.hisRangeCode || r.HISRangeCode,
              rangeValue: r.hisRangeValue || r.HISRangeValue,
              gender: r.gender || r.Gender,
              minValue: r.minValue ?? r.MinValue,
              maxValue: r.maxValue ?? r.MaxValue
            }))
        }))
      };
    });
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
      isActive: val.isActive !== false,
      profileDetails: val.lines.map(l => ({
        id: l.id || 0,
        testId: +l.testId,
        quantity: +l.quantity
      })),
      ProfileDetails: val.lines.map(l => ({
        id: l.id || 0,
        testId: +l.testId,
        quantity: +l.quantity
      }))
    };

    this.loading = true;
    const req = this.id
      ? this.masterService.editItem('TestProfile', profile)
      : this.masterService.addItem('TestProfile', profile);

    req.subscribe(
      (data) => {
        this.loading = false;
        this.alertService.success('Profile saved successfully');
        const newId = this.extractSavedId(data, profile.id);
        if (newId) {
          this.router.navigate(['/test-profiles', newId]);
        } else {
          this.router.navigate(['/test-profiles']);
        }
      },
      err => {
        this.loading = false;
        this.alertService.error(extractApiError(err, 'Save failed'));
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

  private extractSavedId(data: any, fallback: number): number {
    if (!data) {
      return fallback || 0;
    }
    if (typeof data === 'number') {
      return data;
    }
    const result = data.result ?? data.Result;
    if (typeof result === 'number') {
      return result;
    }
    if (result && typeof result === 'object') {
      const nested = result.id ?? result.Id;
      if (nested) {
        return +nested;
      }
    }
    const direct = data.id ?? data.Id;
    return direct ? +direct : (fallback || 0);
  }
}
