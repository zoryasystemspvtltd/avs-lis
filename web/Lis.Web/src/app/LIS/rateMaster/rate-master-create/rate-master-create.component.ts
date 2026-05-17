import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AlertService, TestMasterService } from '../../../_services';
import { RateMasterService } from '../../../_services/rate-master.service';

@Component({
  selector: 'app-rate-master-create',
  templateUrl: './rate-master-create.component.html',
  styleUrls: ['./rate-master-create.component.css']
})
export class RateMasterCreateComponent implements OnInit {
  public rateMasterForm: FormGroup;
  public tests: any[] = [];
  public isLoaded = false;
  public submitted = false;
  public loading = false;

  constructor(
    private formBuilder: FormBuilder,
    private rateMasterService: RateMasterService,
    private testMasterService: TestMasterService,
    private alertService: AlertService,
    private router: Router
  ) { }

  ngOnInit() {
    this.loadTests();
  }

  loadTests() {
    this.testMasterService.getAll()
      .subscribe(response => {
        this.tests = response || [];
        this.isLoaded = true;
        this.initForm();
      },
      error => {
        this.isLoaded = true;
        this.alertService.error(error?.error?.message || 'Failed to load test list');
      });
  }

  initForm() {
    this.rateMasterForm = this.formBuilder.group({
      rate: [null, Validators.required],
      effectiveStart: ['', Validators.required],
      effectiveEnd: ['', Validators.required],
      isActive: [true],
      testId: ['', Validators.required]
    });
  }

  get f() { return this.rateMasterForm.controls; }

  onSubmit() {
    this.submitted = true;

    if (this.rateMasterForm.invalid) {
      return;
    }

    this.loading = true;
    const formValue = this.rateMasterForm.value;
    const payload = {
      rate: parseFloat(formValue.rate),
      effectiveStart: formValue.effectiveStart,
      effectiveEnd: formValue.effectiveEnd,
      isActive: formValue.isActive === true || formValue.isActive === 'true',
      testId: parseInt(formValue.testId, 10)
    };

    this.rateMasterService.create(payload)
      .subscribe(() => {
        this.alertService.success('Rate master record added successfully');
        this.router.navigate(['/rate-master']);
      },
      error => {
        this.loading = false;
        this.alertService.error(error?.error?.message || 'Failed to add rate master record');
      });
  }
}
