import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-roles',
  template: '<app-list-module [schemma]="moduleJson"></app-list-module>'
})
export class RolesListComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }
  public moduleJson: any = {
    url: 'roles',
    heading: 'Role Management',
    module: 'Roles',
    hideAction:true,
    hideSearch:true,
    elements: [
      {
        heading: 'Role Name', fieldName: 'name', sortable: true, width: '100%', type: 'link'
      }
    ]
  }
}
