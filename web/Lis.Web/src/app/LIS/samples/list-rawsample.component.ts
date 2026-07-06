import { Component, OnInit, ViewChild } from '@angular/core';
import { ListModuleComponent } from '../../_components';

@Component({
  selector: 'app-equipment',
  template: '<br><app-his-sample [hideCreateButton]="true" (onGetOrder)="refreshOrder($event)"></app-his-sample><br><app-list-module [schemma]="moduleJson"></app-list-module>'
})

export class ListRawSampleComponent implements OnInit {
  @ViewChild(ListModuleComponent) chield : ListModuleComponent;
  constructor() { }

  ngOnInit() {
  }
  public moduleJson: any = {
    url: 'samples',
    heading: 'List of sample',
    module: 'Patients',
    hideAction: false,
    allowPrint:true,
    hideSearch: false,
    hideCreate: true,
    allowPaging: true,
    receivedOnly: true,
    auto_refresh:false,
    isNew:true,
    elements: [
      {
        heading: 'Barcode No', fieldName: 'sampleNo', sortable: true, width: '12%', type: 'link'
      },
      {
        heading: 'Lab No', fieldName: 'hisRequestNo', sortable: false, width: '10%', type: 'label'
      },
      {
        heading: 'Test Name', fieldName: 'hisTestName',chieldFieldName:'name', sortable: false, width: '14%', type: 'label'
      },
      {
        heading: 'Department', fieldName: 'department',chieldFieldName:'name', sortable: false, width: '14%', type: 'label'
      },
      {
        heading: 'Collection Date', fieldName: 'sampleCollectionDate', sortable: true, width: '16%', type: 'date', format: 'dd/MM/yyyy'
      },
      {
        heading: 'Patient Name', fieldName: 'patient', sortable: false, width: '18%', type: 'chield'
      },
      {
        heading: 'Results', fieldName: 'sampleNo', linkBase: 'lab-result-entry', linkLabel: 'Enter', sortable: false, width: '8%', type: 'link_route'
      }
    ]
  }

  refreshOrder(event) {
    setTimeout(() => this.chield.getItems(),2000);
  }
}
