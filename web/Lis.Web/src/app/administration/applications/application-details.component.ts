import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-application-view',
  template: '<app-view-module [schemma]="moduleJson"></app-view-module>'
})
export class ApplicationDetailsComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }
  public moduleJson : any = {
    url:'client-application',
    module: 'ClientApplication',
    heading:'Application Management',
    hideAction:true,
    elements:[
      {
        heading:'Name',fieldName:'name',type:'h1'
      },
      {
        heading:'Website Url',fieldName:'allowedOrigin',type:'para',
        icon:'map-marker'
      },
      {
        heading:'Access Key',fieldName:'accessKey',type:'para',
        icon:'map-marker'
      },
      {
        heading:'Token Timeout (minute)',fieldName:'refreshTokenLifeTime',type:'para',
        icon:'map-marker'
      },
      {
        heading:'Details',fieldName:'description',type:'area'
      }
    ]
  }
}
