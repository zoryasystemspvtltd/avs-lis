import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-equipment',
  template: '<app-list-module [schemma]="moduleJson"></app-list-module>'
})

export class ListRejectedSampleComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }
  public moduleJson: any = {
    url: 'rejected-samples',
    heading: 'List of rejected sample',
    module: 'Patients',
    hideAction: false,
    hideSearch: false,
    hideCreate: true,
    allowPaging: true,
    filterStatus:4,
    allowedFilter: [4, 6, 7], // TODO Removel Doctor's approval: [4, 6, 7]
    auto_refresh:true,
    isRejected:true,
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
        heading: 'Collection Date', fieldName: 'sampleCollectionDate', sortable: true, width: '12%', type: 'date', format: 'dd/MM/yyyy'
      },
      {
        heading: 'Patient Name', fieldName: 'patient', sortable: false, width: '18%', type: 'chield'
      }
    ]
  }
}
