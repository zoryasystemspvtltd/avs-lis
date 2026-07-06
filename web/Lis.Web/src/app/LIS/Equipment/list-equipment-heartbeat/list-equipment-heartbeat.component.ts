import { Component } from '@angular/core';

@Component({
  selector: 'app-equipment-heartbeat',
  template: '<app-list-module [schemma]="moduleJson" [listModuleKey]="\'equipment-heartbeat\'"></app-list-module>'
})
export class ListEquipmentHeartbeatComponent {
  moduleJson = {
    url: 'equipment-heartbeat',
    heading: 'Equipment Heartbeat Monitor',
    pageDescription: 'Live connectivity monitor — online status and last heartbeat for active equipment only (read-only).',
    module: 'EquipmentHeartbeat',
    hideAction: true,
    hideCreate: true,
    hideSearch: false,
    allowPaging: true,
    auto_refresh: true,
    elements: [
      { heading: 'Equipment', fieldName: 'name', sortable: true, width: '25%', type: 'label' },
      { heading: 'Model', fieldName: 'model', sortable: true, width: '15%', type: 'label' },
      { heading: 'Access Key', fieldName: 'accessKey', sortable: false, width: '20%', type: 'label' },
      { heading: 'Online', fieldName: 'isAlive', sortable: false, width: '10%', type: 'status_icon' },
      { heading: 'Last Heartbeat', fieldName: 'heartBeatTime', sortable: false, width: '30%', type: 'date', format: 'dd/MM/yyyy HH:mm:ss' }
    ]
  };
}
