import { Component, OnInit } from '@angular/core';
import { Subscription, timer } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { ModuleService } from '../../../_services';

@Component({
  selector: 'app-equipment-alert',
  template: `<div style="float:left;" *ngFor="let item of items;">
    <span class="glyphicon glyphicon glyphicon-ok-sign icon-large" 
    [title]="item.name" 
    [ngClass]="{'icon-green': item.isAlive === true,'icon-red': item.isAlive !== true}"></span>
  <div>`
})

export class AlertEquipmentComponent implements OnInit {
  items:any[];
  public option = {
    'RecordPerPage': 10,
    'CurrentPage': 1,
    'SortColumnName': 'name',
    'SortDirection': true
  };
  subscription: Subscription
  
  constructor(private moduleService: ModuleService) { }

  ngOnInit() {
    this.subscription = timer(0, 10000).pipe(
      switchMap(() => this.loadEquipments())
    ).subscribe(response => this.items = response.items);

  }
  
  loadEquipments(){
    return this.moduleService.getItems('equipments', this.option)
      .pipe(map(response => {
        return response;
      }));
  }
}
