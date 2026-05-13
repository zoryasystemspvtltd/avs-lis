import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AlertService, EquipmentService, SampleService } from '../../../_services';
import * as moment from 'moment';
@Component({
  selector: 'app-edit-sample',
  templateUrl: './edit-sample.component.html',
  styleUrls: ['./edit-sample.component.css']
})
export class EditSampleComponent implements OnInit {
  submitted: boolean = false;
  reqId: number;
  id: string;
  item: any;
  sampleDetails: any;
  private sub: any;
  public isLoaded: boolean;
  editSampleForm: FormGroup;
  loading: boolean = false;
  public message: string;
  hisTests: any[];
  allTests: any[];
  departments: any[];

  constructor(
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
    if (this.reqId > 0) {
      this.getSampleDetails();
    }
    this.getDepartments();
  }

  getSampleDetails() {
    this.isLoaded = false;
    this.sampleService.getSample(this.reqId)
      .subscribe(response => {
        this.sampleDetails = response;
        this.sampleDetails.sampleCollectionDate = moment(this.sampleDetails.sampleCollectionDate).format('MM-DD-YYYY');
        this.initForm();
        this.isLoaded = true;
      });
  }
  initForm() {
    this.sampleDetails.patient.gender = this.sampleDetails.patient.gender.charAt(0);    
    this.editSampleForm = this.formBuilder.group({
      sampleNo: [this.sampleDetails.sampleNo],
      mrNo: [this.sampleDetails.mrNo],
      ipNo: [this.sampleDetails.ipNo],
      bedNo: [this.sampleDetails.bedNo],
      hisPatientId: [this.sampleDetails.patient.hisPatientId],
      hisRequestNo: [this.sampleDetails.hisRequestNo],
      patientName: [this.sampleDetails.patient.name],
      age: [this.sampleDetails.patient.age],
      gender: [this.sampleDetails.patient.gender],
      sampleCollectionDate: [this.sampleDetails.sampleCollectionDate],
    });
  }

  get f() { return this.editSampleForm.controls; }

  getAvailableHisTests() {
    this.equipmentService.getHisTests()
      .subscribe(response => {
        this.hisTests = response;
        this.allTests = response;
        for (let test of this.hisTests) {
          test.isActive = false;
        }
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
    if (this.editSampleForm.invalid) {
      return;
    }
    let item = this.editSampleForm.value;
    var age = parseFloat(item.age).toFixed(2)
    var yr = age.split('.')
    var myDate = moment();
    var dob = myDate.subtract(yr[0], 'year').subtract(yr[1], 'month');
    let patientDetail = {
      Name: item.patientName,
      HisPatientId: item.hisPatientId,
      DateOfBirth: dob,
      Gender: item.gender,
      SampleNo: item.sampleNo
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
          SampleNo: item.sampleNo,
          SampleCollectionDate: new Date(item.sampleCollectionDate),
          SpecimenCode: test.hisSpecimenCode,
          SpecimenName: test.hisSpecimenName,
          HISRequestNo: item.hisRequestNo
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
