import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-equipment',
  template: '<app-list-module [schemma]="moduleJson"></app-list-module>'
})

export class ListQualitySampleComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }
  public moduleJson: any = {
    url: 'quality-controls',
    heading: 'Quality Control List',
    module: 'Quality',
    hideAction: true,
    hideSearch: true,
    hideCreate: true,
    allowPaging: true,    
    elements: [
      {
        heading: 'Equipment Name', fieldName: 'equipmentName', sortable: false, width: '40%', type: 'link'
      },
      {
        heading: 'Control Period', fieldName: 'monthName', sortable: false, width: '30%', type: 'link'
      },
      {
        heading: 'Quality Control No', fieldName: 'sampleNo', sortable: false, width: '30%', type: 'link'
      }
    ]
  }
}
