import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-users-list',
  template: '<app-list-module [schemma]="moduleJson"></app-list-module>'
})
export class UsersListComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }
  public moduleJson: any = {
    url: 'users',
    heading: 'User Management',
    module: 'Users',
    hideAction:true,
    elements: [
      {
        heading: 'First Name', fieldName: 'first_name', sortable: true, width: '20%', type: 'link'
      },
      {
        heading: 'Last Name', fieldName: 'last_name', sortable: true, width: '20%', type: 'link'
      },
      {
        heading: 'Email Address', fieldName: 'email', sortable: true, width: '20%', type: 'label'
      },
      {
        heading: 'Role', fieldName: 'role', sortable: true, width: '20%', type: 'label'
      },
      {
        heading: 'Phone Number', fieldName: 'phone_number', sortable: true, width: '20%', type: 'label'
      }
    ]
  }
}
