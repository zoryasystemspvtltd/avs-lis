import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-equipment',
  template: '<app-list-module [schemma]="moduleJson" [listModuleKey]="\'equipments\'"></app-list-module>'
})

export class ListEquipmentComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }
  public moduleJson: any = {
    url: 'equipments',
    heading: 'Equipment Management',
    pageDescription: 'Maintain equipment master records — create, edit, and activate/deactivate analyzers.',
    module: 'Equipments',
    hideAction: true,
    hideSearch: false,
    hideCreate: false,
    allowPaging: true,
    auto_refresh: false,
    elements: [
      { heading: 'Equipment Name', fieldName: 'name', sortable: true, width: '35%', type: 'link' },
      { heading: 'Model', fieldName: 'model', sortable: true, width: '25%', type: 'label' },
      { heading: 'Access Key', fieldName: 'accessKey', sortable: false, width: '25%', type: 'label' },
      { heading: 'Active', fieldName: 'isActive', sortable: false, width: '15%', type: 'label' }
    ]
  }
}
