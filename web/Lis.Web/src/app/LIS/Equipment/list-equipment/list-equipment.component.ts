import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-equipment',
  template: '<app-list-module [schemma]="moduleJson"></app-list-module>'
})

export class ListEquipmentComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }
  public moduleJson: any = {
    url: 'equipments',
    heading: 'Equipment Management',
    module: 'Equipments',
    hideAction:true,
    hideSearch:true,
    auto_refresh:true,
    elements: [
      {
        heading: 'Equipment Name', fieldName: 'name', sortable: true, width: '60%', type: 'link'
      },
      {
        heading: 'Equipment Status', fieldName: 'isAlive', sortable: false, width: '15%', type: 'status_icon'
      },
      {
        heading: 'Last Heart beat', fieldName: 'heartBeatTime', sortable: false, width: '25%', type: 'date', format: 'dd/MM/yyyy HH:mm:ss' 
      },
    ]
  }
}
