import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AlertService, TestMasterService } from '../../../_services';

@Component({
  selector: 'app-test-create',
  templateUrl: './test-create.component.html',
  styleUrls: ['./test-create.component.css']
})
export class TestCreateComponent implements OnInit {

  public isLoaded: boolean = false;
  submitted: boolean = false;
  loading: boolean = false;
  createTestForm: FormGroup;
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
    this.loadDropdowns();
  }

  loadDropdowns() {
    forkJoin({
      specimens: this.testMasterService.getSpecimens(),
      departments: this.testMasterService.getDepartments()
    }).subscribe(
      data => {
        this.specimens = data.specimens || [];
        this.departments = data.departments || [];
        if (this.specimens.length === 0 || this.departments.length === 0) {
          this.alertService.error('Specimen or department lookup data is empty. Check master setup.');
        }
        this.isLoaded = true;
        this.initForms();
      },
      () => {
        this.alertService.error('Failed to load specimens or departments');
        this.specimens = [];
        this.departments = [];
        this.isLoaded = true;
        this.initForms();
      }
    );
  }

  initForms() {
    this.createTestForm = this.formBuilder.group({
      hisTestCode: [{ value: '', disabled: true }, Validators.required],
      hisTestCodeDescription: ['', Validators.required],
      hisSpecimenCode: ['', Validators.required],
      departmentCode: ['', Validators.required],
      isActive: [true]
    });
    this.testMasterService.getNextTestCode().subscribe(code => {
      if (code) {
        this.createTestForm.patchValue({ hisTestCode: code });
      }
    });
  }

  get f() { return this.createTestForm.controls; }

  isInValid(fieldName: string) {
    if (this.createTestForm.get(fieldName).hasError('required') && this.createTestForm.get(fieldName).touched) {
      return true;
    }
    return false;
  }

  onSubmit() {
    this.submitted = true;

    if (this.createTestForm.invalid) {
      return;
    }

    this.loading = true;
    const formValue = this.createTestForm.getRawValue();
    const selectedSpecimen = this.specimens.find(s =>
      (s.code || s.Code) === formValue.hisSpecimenCode);
    const test: any = {
      hisTestCode: formValue.hisTestCode,
      hisTestCodeDescription: formValue.hisTestCodeDescription,
      hisSpecimenCode: formValue.hisSpecimenCode,
      hisSpecimenName: selectedSpecimen ? selectedSpecimen.name : null,
      departmentCode: formValue.departmentCode,
      isActive: formValue.isActive === true || formValue.isActive === 'true'
    };

    this.testMasterService.create(test)
      .subscribe(data => {
        this.loading = false;
        this.alertService.success('Test added successfully');
        this.router.navigate(['/test-master']);
      },
        error => {
          this.loading = false;
          this.alertService.error(error?.error?.message || 'Failed to add test');
        });
  }
}
