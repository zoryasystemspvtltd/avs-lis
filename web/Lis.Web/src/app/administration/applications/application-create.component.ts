import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-application-create',
  template: '<app-create-module [schemma]="moduleJson"></app-create-module>'
})
export class ApplicationCreateComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }
  public moduleJson : any = {
    url:'client-application',
    module: 'ClientApplication',
    heading:'Application Management',
    hideAction:true,
    elements:[
      [
        {
          col:'12',type:'text',label:'Name',fieldName:'name',required:true
          ,icon:'certificate'
        }
      ],
      [
        {
          col:'12',type:'text',label:'Website Url',fieldName:'allowedOrigin',required:true
          ,icon:'certificate'
        }
      ],
      [
        {
          col:'12',type:'text',label:'Access Key',fieldName:'accessKey',required:true
          ,icon:'certificate'
        }
      ],
      [
        {
          col:'12',type:'number',label:'Token Timeout (minute)',fieldName:'refreshTokenLifeTime',required:true
          ,icon:'certificate'
        }
      ],
      [
        {
          col:'12',type:'editor',label:'Details',fieldName:'description',required:false
          
        }
      ],
      [
        {
          col:'12', type:'hidden', fieldName:'status'
        }
      ]
    ]
  };
}
