import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AlertService, TestMasterService } from '../../../_services';
import { RateMasterService } from '../../../_services/rate-master.service';

@Component({
  selector: 'app-rate-master-edit',
  templateUrl: './rate-master-edit.component.html',
  styleUrls: ['./rate-master-edit.component.css']
})
export class RateMasterEditComponent implements OnInit, OnDestroy {
  id: string;
  private sub: any;
  public rateMasterForm: FormGroup;
  public tests: any[] = [];
  public item: any;
  public isLoaded = false;
  public submitted = false;
  public loading = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private formBuilder: FormBuilder,
    private rateMasterService: RateMasterService,
    private testMasterService: TestMasterService,
    private alertService: AlertService
  ) { }

  ngOnInit() {
    this.sub = this.route.params.subscribe(params => {
      this.id = params['id'];
      this.loadTests();
    });
  }

  loadTests() {
    this.testMasterService.getAll()
      .subscribe(response => {
        this.tests = response || [];
        this.loadItem();
      },
      error => {
        this.alertService.error(error?.error?.message || 'Failed to load test list');
        this.isLoaded = true;
      });
  }

  loadItem() {
    this.rateMasterService.getById(this.id)
      .subscribe(response => {
        this.item = response;
        this.initForm();
        this.isLoaded = true;
      },
      error => {
        this.alertService.error(error?.error?.message || 'Failed to load rate master record');
        this.isLoaded = true;
      });
  }

  initForm() {
    this.rateMasterForm = this.formBuilder.group({
      rate: [this.item?.rate || null, Validators.required],
      effectiveStart: [this.item?.effectiveStart ? this.item.effectiveStart.toString().substring(0, 10) : '', Validators.required],
      effectiveEnd: [this.item?.effectiveEnd ? this.item.effectiveEnd.toString().substring(0, 10) : '', Validators.required],
      isActive: [this.item?.isActive !== false],
      testId: [this.item?.testId || '', Validators.required]
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
      id: this.id,
      rate: parseFloat(formValue.rate),
      effectiveStart: formValue.effectiveStart,
      effectiveEnd: formValue.effectiveEnd,
      isActive: formValue.isActive === true || formValue.isActive === 'true',
      testId: parseInt(formValue.testId, 10)
    };

    this.rateMasterService.update(payload)
      .subscribe(() => {
        this.alertService.success('Rate master record updated successfully');
        this.router.navigate(['/rate-master']);
      },
      error => {
        this.loading = false;
        this.alertService.error(error?.error?.message || 'Failed to update rate master record');
      });
  }

  ngOnDestroy() {
    this.sub.unsubscribe();
  }
}
