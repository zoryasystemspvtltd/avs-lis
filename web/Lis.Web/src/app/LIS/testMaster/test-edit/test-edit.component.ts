import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AlertService, TestMasterService } from '../../../_services';

@Component({
  selector: 'app-test-edit',
  templateUrl: './test-edit.component.html',
  styleUrls: ['./test-edit.component.css']
})
export class TestEditComponent implements OnInit {

  id: string;
  private sub: any;
  public isLoaded: boolean;
  submitted: boolean = false;
  loading: boolean = false;
  editTestForm: FormGroup;
  validationError: any[] = [];
  public message: string;
  item: any;
  specimens: any[] = [];
  departments: any[] = [];

  constructor(
    private testMasterService: TestMasterService,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private router: Router) { }

  ngOnInit() {
    this.sub = this.route.params.subscribe(params => {
      this.id = params['id'];
      this.loadDropdowns();
    });
  }

  loadDropdowns() {
    forkJoin({
      specimens: this.testMasterService.getSpecimens(),
      departments: this.testMasterService.getDepartments()
    }).subscribe(
      data => {
        this.specimens = data.specimens || [];
        this.departments = data.departments || [];
        this.loadTestData();
      },
      () => {
        this.alertService.error('Failed to load specimens or departments');
        this.specimens = [];
        this.departments = [];
        this.loadTestData();
      }
    );
  }

  loadTestData() {
    this.testMasterService.getById(this.id)
      .subscribe(response => {
        this.item = response;
        this.isLoaded = true;
        this.initForms();
      },
        error => {
          this.alertService.error('Failed to load test details');
          this.isLoaded = true;
        });
  }

  initForms() {
    this.editTestForm = this.formBuilder.group({
      hisTestCode: [this.item.hisTestCode, Validators.required],
      hisTestCodeDescription: [this.item.hisTestCodeDescription, Validators.required],
      hisSpecimenCode: [this.item.hisSpecimenCode, Validators.required],
      departmentCode: [this.item.departmentCode, Validators.required],
      isActive: [this.coerceBool(this.item.isActive ?? this.item.IsActive)]
    });
  }

  get f() { return this.editTestForm.controls; }

  isInValid(fieldName: string) {
    if (this.editTestForm.get(fieldName).hasError('required') && this.editTestForm.get(fieldName).touched) {
      return true;
    }
    return false;
  }

  onSubmit() {
    this.submitted = true;

    if (this.editTestForm.invalid) {
      return;
    }

    this.loading = true;
    const formValue = this.editTestForm.value;
    const test: any = {
      id: this.id,
      hisTestCode: formValue.hisTestCode,
      hisTestCodeDescription  : formValue.hisTestCodeDescription,
      hisSpecimenCode: formValue.hisSpecimenCode,
      hisSpecimenName: this.item.hisSpecimenName,
      departmentCode: formValue.departmentCode,
      createdOn: this.item.createdOn,
      isActive: formValue.isActive
    };

    this.testMasterService.update(test)
      .subscribe(
        data => {
          this.alertService.success('Test updated successfully');
          this.router.navigate(['/test-master']);
        },
        error => {
          this.loading = false;
          this.alertService.error(error?.error?.message || 'Failed to update test');
        });
  }

  private coerceBool(value: any): boolean {
    return value === true || value === 'true' || value === 1 || value === '1';
  }

  ngOnDestroy() {
    this.sub.unsubscribe();
  }
}
