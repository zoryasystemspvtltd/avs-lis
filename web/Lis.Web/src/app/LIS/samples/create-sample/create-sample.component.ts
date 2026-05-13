import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationToken } from '../../../_models';
import { AuthenticationService, AlertService, EquipmentService, SampleService } from '../../../_services';
import * as moment from 'moment';
@Component({
  selector: 'app-create-sample',
  templateUrl: './create-sample.component.html',
  styleUrls: ['./create-sample.component.css']
})
export class CreateSampleComponent implements OnInit {
  submitted: boolean = false;
  reqId: number;
  id: string;
  private user: AuthenticationToken;
  item: any;
  response: any;
  private sub: any;
  public isLoaded: boolean;
  addSampleForm: FormGroup;
  loading: boolean = false;
  public message: string;
  hisTests: any[];
  allTests: any[];
  departments: any[];

  constructor(private authenticationService: AuthenticationService,
    private equipmentService: EquipmentService,
    private sampleService: SampleService,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private router: Router) { }

  ngOnInit() {
    this.sub = this.route.params.subscribe(params => {
      this.isLoaded = false;
      this.reqId = +params['id'];
    });

    this.isLoaded = false;
    this.getDepartments();
  }

  getAvailableHisTests() {
    this.equipmentService.getHisTests()
      .subscribe(response => {
        this.hisTests = response;
        this.allTests = response;
        for (let test of this.hisTests) {
          test.isActive = false;
        }
        this.isLoaded = true;
        this.initForms();
      });
  }

  getDepartments() {
    this.equipmentService.getDepartments()
      .subscribe(response => {
        this.departments = response;
        this.getAvailableHisTests();
      });
  }

  filterMapping(mappings: any[], code: string) {
    let filteredMapping = mappings.filter(m => m.departmentCode == code);
    return filteredMapping;
  }

  hasMapping(mappings: any[], code: string) {
    let filteredMapping = mappings.filter(m => m.departmentCode == code);
    return filteredMapping.length > 0;
  }

  selectPanel(item) {
    item.selected = !item.selected
  }

  initForms() {
    this.addSampleForm = this.formBuilder.group({
      hisRequestNo: ['', Validators.required],
      mrNo: ['', Validators.required],
      patientName: ['', Validators.required],
      hisPatientId: ['', Validators.required],
      year: ['', Validators.required],
      month: ['', Validators.required],
      gender: ['', Validators.required],
      sampleCollectionDate: ['', Validators.required],
      ipNo: [''],
      bedNo: [''],
    });
  }
  get f() { return this.addSampleForm.controls; }

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
    }
    else {
      this.selectedTest = this.selectedTest.filter(obj => obj !== element);
    }
  }

  deleteItem(element) {
    this.selectedTest = this.selectedTest.filter(obj => obj !== element);
  }

  onSubmit() {
    this.submitted = true;
    // stop here if form is invalid
    if (this.addSampleForm.invalid) {
      return;
    }
    let item = this.addSampleForm.value;
    var myDate = moment();
    var dob = myDate.subtract(item.year, 'year').subtract(item.month, 'month');
    let patientDetail = {
      Name: item.patientName,
      HisPatientId: item.hisPatientId,
      DateOfBirth: dob,
      Gender: item.gender
    }

    let testRequestDetails: any[] = [];
    for (let test of this.selectedTest) {
      if (test.isActive) {
        let order = {
          IPNo: item.ipNo,
          MRNo: item.mrNo,
          BEDNo: item.bedNo,
          HISTestCode: test.hisTestCode,
          HISTestName: test.hisTestCodeDescription,
          HISRequestNo: item.hisRequestNo,
          SampleCollectionDate: item.sampleCollectionDate,
          SpecimenCode: test.hisSpecimenCode,
          SpecimenName: test.hisSpecimenName
        }
        testRequestDetails.push(order);
      }
    }

    let neworder = {
      PatientDetail: patientDetail,
      TestRequestDetails: testRequestDetails
    }
    this.sampleService.createNewSample(neworder)
      .subscribe(data => {
        this.loading = false;
        this.router.navigate(['/samples/']);
      },
        (error) => {
          let message: string = error;
          this.loading = false;
          this.message = (message != "") ? message : 'Data not saved.';
          this.alertService.error(this.message);
        });


    this.loading = true;
  }

  doSearch(event) {
    let searchText = event.target.value;
    if (searchText !== '') {
      this.hisTests = this.allTests.filter(s => {
        return (s.hisTestCodeDescription.toLowerCase().indexOf(searchText.toLowerCase()) >= 0
          || s.hisTestCode.toLowerCase().indexOf(searchText.toLowerCase()) >= 0)
      });
    } else {
      this.hisTests = this.allTests
    }
  }
}
