import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-application-list',
  template: '<app-list-module [schemma]="moduleJson"></app-list-module>'
})
export class ApplicationListComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }

  public moduleJson: any = {
    url: 'client-application',
    module: 'ClientApplication',
    heading: 'Application Management',
    hideAction:true,
    elements: [
      {
        heading: 'Name', fieldName: 'name', sortable: true, width: '20%', type: 'link'
      },
      {
        heading: 'Url', fieldName: 'allowedOrigin', sortable: true, width: '20%', type: 'label'
      },
      {
        heading: 'Details', fieldName: 'description', sortable: true, width: '60%', type: 'frame', length: '150', textOnly: true
      }
    ]
  }
}
