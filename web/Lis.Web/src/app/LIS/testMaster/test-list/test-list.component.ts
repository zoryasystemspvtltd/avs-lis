import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-equipment',
  template: '<app-list-module [schemma]="moduleJson" [listModuleKey]="\'test-master\'"></app-list-module>'
})
export class TestListComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }

  public moduleJson: any = {
    url: 'test-master',
    heading: 'Test Master',
    module: 'HisTest',
    hideCreate: false,
    hideAction: true,
    hideSearch: false,
    allowPaging: true,
    elements: [
      {
        heading: 'Test Code', fieldName: 'hisTestCode', sortable: true, width: '20%', type: 'link'
      },
      {
        heading: 'Test Name', fieldName: 'hisTestCodeDescription', sortable: true, width: '25%', type: 'label'
      },
      {
        heading: 'Specimen', fieldName: 'hisSpecimenName', sortable: true, width: '25%', type: 'label'
      },
      {
        heading: 'Department', fieldName: 'departmentName', sortable: true, width: '20%', type: 'label'
      },
      {
        heading: 'Status', fieldName: 'isActive', sortable: true, width: '10%', type: 'label'
      }
    ]
  }
}
