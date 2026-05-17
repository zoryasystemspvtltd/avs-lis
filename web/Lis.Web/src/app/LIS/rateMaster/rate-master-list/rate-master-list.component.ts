import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-equipment',
  template: '<app-list-module [schemma]="moduleJson"></app-list-module>'
})
export class RateMasterListComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }

  public moduleJson: any = {
    url: 'rate-master',
    heading: 'Rate Master',
    module: 'TestRate',
    hideCreate: false,
    hideAction: true,
    hideSearch: false,
    allowPaging: true,
    elements: [
      
      {
        heading: 'Test Name', fieldName: 'testName', sortable: true, width: '25%', type: 'label'
      },
      {
        heading: 'Rate', fieldName: 'Rate', sortable: true, width: '20%', type: 'link'
      },
      {
        heading: 'Effective Start Date', fieldName: 'effectiveStart', sortable: true, width: '16%', type: 'date', format: 'dd/MM/yyyy'
      },
      {
        heading: 'Effective End Date', fieldName: 'effectiveEnd', sortable: true, width: '16%', type: 'date', format: 'dd/MM/yyyy'
      },
    ]
  }
}
