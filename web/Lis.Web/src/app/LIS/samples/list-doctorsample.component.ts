import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-equipment',
  template: '<app-list-module [schemma]="moduleJson"></app-list-module>'
})

export class ListDoctorSampleComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }
  public moduleJson: any = {
    url: 'doctor-samples',
    heading: 'Samples to be reviewed by doctor',
    module: 'Patients',
    hideAction: false,
    hideSearch: false,
    hideCreate: true,
    allowPaging: true,
    filterStatus:3,
    //allowedFilter: [0, 1],
    auto_refresh:true,
    isDoctor:true,
    elements: [
      {
        heading: 'Barcode No', fieldName: 'sampleNo', sortable: true, width: '12%', type: 'link'
      },
      {
        heading: 'Lab No', fieldName: 'hisRequestNo', sortable: false, width: '10%', type: 'label'
      },
      {
        heading: 'IP No', fieldName: 'ipNo', sortable: false, width: '10%', type: 'label'
      },
      {
        heading: 'Test Name', fieldName: 'hisTestName',chieldFieldName:'name', sortable: false, width: '16%', type: 'label'
      },
      {
        heading: 'Department', fieldName: 'department',chieldFieldName:'name', sortable: false, width: '14%', type: 'label'
      },
      {
        heading: 'Collection Date', fieldName: 'sampleCollectionDate', sortable: true, width: '14%', type: 'date', format: 'dd/MM/yyyy'
      },
      {
        heading: 'Patient Name', fieldName: 'patient', sortable: false, width: '18%', type: 'chield'
      }
    ]
  }
}
