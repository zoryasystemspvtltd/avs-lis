import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationService, AlertService, TestMasterService, SampleService, MasterService } from '../../../_services';
import * as moment from 'moment';

@Component({
  selector: 'app-create-sample',
  templateUrl: './create-sample.component.html',
  styleUrls: ['./create-sample.component.css']
})
export class CreateSampleComponent implements OnInit {
  submitted = false;
  addSampleForm: FormGroup;
  loading = false;
  message: string;
  hisTests: any[] = [];
  allTests: any[] = [];
  departments: any[] = [];
  patients: any[] = [];
  testRates: { [testId: number]: number } = {};
  isLoaded = false;

  constructor(
    private testMasterService: TestMasterService,
    private sampleService: SampleService,
    private masterService: MasterService,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private router: Router) { }

  ngOnInit() {
    this.initForms();
    this.masterService.getNextRequestNo().subscribe(no => {
      if (no) {
        this.addSampleForm.patchValue({ hisRequestNo: no });
      }
    });
    this.masterService.getBillingPatients({
      RecordPerPage: 500, CurrentPage: 1, SearchText: '', SortColumnName: 'Name', SortDirection: false, Status: 0
    }).subscribe(p => {
      this.patients = p?.items || [];
    });
    this.getDepartments();
  }

  initForms() {
    this.addSampleForm = this.formBuilder.group({
      hisRequestNo: [{ value: '', disabled: true }, Validators.required],
      patientId: ['', Validators.required],
      hisPatientId: [{ value: '', disabled: true }],
      patientName: [{ value: '', disabled: true }],
      year: ['', Validators.required],
      month: ['0', Validators.required],
      gender: ['', Validators.required],
      sampleCollectionDate: [new Date().toISOString().substring(0, 10), Validators.required],
    });
  }

  get f() { return this.addSampleForm.controls; }

  getDepartments() {
    this.testMasterService.getDepartments()
      .subscribe(response => {
        this.departments = response;
        this.getAvailableHisTests();
      });
  }

  getAvailableHisTests() {
    this.testMasterService.getAll()
      .subscribe(response => {
        this.hisTests = (response || []).map(t => Object.assign({}, t, { isActive: false, displayRate: 0 }));
        this.allTests = this.hisTests;
        this.preloadRates();
        this.isLoaded = true;
      });
  }

  preloadRates() {
    this.hisTests.forEach(test => {
      if (test.id) {
        this.masterService.getEffectiveRate(+test.id, 0).subscribe(rate => {
          test.displayRate = rate?.rate || 0;
          this.testRates[+test.id] = test.displayRate;
        });
      }
    });
  }

  onPatientSelected() {
    const patientId = +this.addSampleForm.get('patientId').value;
    const patient = this.patients.find(p => +p.id === patientId);
    if (!patient) {
      return;
    }

    this.addSampleForm.patchValue({
      hisPatientId: patient.hisPatientId,
      patientName: patient.name,
      gender: patient.gender || ''
    });

    if (patient.dateOfBirth) {
      const dob = moment(patient.dateOfBirth);
      const years = moment().diff(dob, 'years');
      const months = moment().diff(dob, 'months') % 12;
      this.addSampleForm.patchValue({ year: years, month: months });
    } else if (patient.age) {
      this.addSampleForm.patchValue({ year: Math.floor(+patient.age), month: 0 });
    }
  }

  filterMapping(mappings: any[], code: string) {
    return mappings.filter(m => m.departmentCode === code);
  }

  hasMapping(mappings: any[], code: string) {
    return mappings.filter(m => m.departmentCode === code).length > 0;
  }

  selectPanel(item) {
    item.selected = !item.selected;
  }

  hasAccess(): boolean {
    return true;
  }

  isInValid(field: string) {
    if (this.submitted) {
      if (this.f[field].errors && this.f[field].errors.required) {
        return true;
      }
    }
    return false;
  }

  selectedTest: any[] = [];

  onCheckActive(event, element) {
    element.isActive = event.target.checked;
    if (element.isActive) {
      if (this.selectedTest.indexOf(element) < 0) {
        this.selectedTest.push(element);
      }
    } else {
      this.selectedTest = this.selectedTest.filter(obj => obj !== element);
    }
  }

  deleteItem(element) {
    element.isActive = false;
    this.selectedTest = this.selectedTest.filter(obj => obj !== element);
  }

  onSubmit() {
    this.submitted = true;
    if (this.addSampleForm.invalid || this.selectedTest.length === 0) {
      if (this.selectedTest.length === 0) {
        this.alertService.error('Select at least one test.');
      }
      return;
    }

    const item = this.addSampleForm.getRawValue();
    const myDate = moment();
    const dob = myDate.clone().subtract(item.year, 'year').subtract(item.month, 'month');
    const patientDetail = {
      Name: item.patientName,
      HisPatientId: item.hisPatientId,
      DateOfBirth: dob.toDate(),
      Gender: item.gender
    };

    const testRequestDetails: any[] = [];
    for (const test of this.selectedTest) {
      if (test.isActive) {
        testRequestDetails.push({
          IPNo: '',
          MRNo: '',
          BEDNo: '',
          HISTestCode: test.hisTestCode,
          HISTestName: test.hisTestCodeDescription,
          HISRequestNo: item.hisRequestNo,
          SampleCollectionDate: item.sampleCollectionDate,
          SpecimenCode: test.hisSpecimenCode,
          SpecimenName: test.hisSpecimenName,
          Rate: this.testRates[+test.id] || test.displayRate || 0
        });
      }
    }

    const neworder = {
      PatientDetail: patientDetail,
      TestRequestDetails: testRequestDetails
    };

    this.loading = true;
    this.sampleService.createNewSample(neworder)
      .subscribe(
        () => {
          this.loading = false;
          this.alertService.success('Sample created successfully');
          this.router.navigate(['/samples/']);
        },
        (error) => {
          this.loading = false;
          const message = error?.error?.message || error?.message || 'Data not saved.';
          this.alertService.error(message);
        });
  }

  doSearch(event) {
    const searchText = event.target.value;
    if (searchText !== '') {
      this.hisTests = this.allTests.filter(s => {
        return (s.hisTestCodeDescription.toLowerCase().indexOf(searchText.toLowerCase()) >= 0
          || s.hisTestCode.toLowerCase().indexOf(searchText.toLowerCase()) >= 0);
      });
    } else {
      this.hisTests = this.allTests;
    }
  }
}
