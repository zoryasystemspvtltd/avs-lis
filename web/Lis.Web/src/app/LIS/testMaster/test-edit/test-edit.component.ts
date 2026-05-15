import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
    this.testMasterService.getSpecimens().subscribe(
      response => {
        this.specimens = response;
        this.loadDepartments();
      },
      error => {
        this.alertService.error('Failed to load specimens');
      }
    );
  }

  loadDepartments() {
    this.testMasterService.getDepartments().subscribe(
      response => {
        this.departments = response;
        this.loadTestData();
      },
      error => {
        this.alertService.error('Failed to load departments');
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
      isActive: [this.item.isActive]
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

  ngOnDestroy() {
    this.sub.unsubscribe();
  }
}
