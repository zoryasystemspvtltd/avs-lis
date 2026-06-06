import { Component } from '@angular/core';

@Component({
  selector: 'app-equipment-heartbeat',
  template: '<app-list-module [schemma]="moduleJson"></app-list-module>'
})
export class ListEquipmentHeartbeatComponent {
  moduleJson = {
    url: 'equipment-heartbeat',
    heading: 'Equipment Heartbeat Monitor',
    module: 'EquipmentHeartbeat',
    hideAction: true,
    hideCreate: true,
    hideSearch: false,
    auto_refresh: true,
    elements: [
      { heading: 'Equipment', fieldName: 'name', sortable: true, width: '35%', type: 'label' },
      { heading: 'Model', fieldName: 'model', sortable: true, width: '20%', type: 'label' },
      { heading: 'Status', fieldName: 'isAlive', sortable: false, width: '15%', type: 'status_icon' },
      { heading: 'Last Heartbeat', fieldName: 'heartBeatTime', sortable: false, width: '30%', type: 'date', format: 'dd/MM/yyyy HH:mm:ss' }
    ]
  };
}
