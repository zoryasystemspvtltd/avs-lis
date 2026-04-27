import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-application-edit',
  template: '<app-edit-module [schemma]="moduleJson"></app-edit-module>'
})
export class ApplicationEditComponent implements OnInit {

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
          col:'3', type:'hidden', fieldName:'status'
        },
        {
          col:'3', type:'hidden', fieldName:'id'
        },
        {
          col:'3', type:'hidden', fieldName:'activityMember'
        },
        {
          col:'3', type:'hidden', fieldName:'applicationId'
        }
      ]
    ]
  };
  
}
