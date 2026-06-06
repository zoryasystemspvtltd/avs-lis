import { Component, OnInit, Input } from '@angular/core';
import { ModuleService } from '../../_services';

@Component({
  selector: 'app-log-list',
  templateUrl: './log-list.component.html'
})
export class LogListComponent implements OnInit {

  public items:any[];
  public isLoaded:Boolean;
  public recordFrom:number;
  public totalRecord:number;
  public recordTo:number;

  @Input() entityId:string;
  @Input() module:string;

  public option:any;

  constructor(private moduleService: ModuleService) { }

  ngOnInit() {
    this.option = {
      'RecordPerPage': 10,
      'CurrentPage': 1,
      'SortColumnName': 'ActivityDate',
      'SortDirection': false,
      'SearchCondition': {
        'Name': 'EntityID',
        'Value': this.entityId,
          'And': {
              'Name': 'Name',
              'Value': this.module
          }
      }
    };
    this.getItems();
  }

  sort = function (columnName:string) {
    this.option.SortColumnName = columnName;
    this.option.SortDirection = !this.option.SortDirection;
    this.getItems();
  }

  showSortIcon = function (columnName:string, direction:boolean) {
    return !(this.option.SortColumnName == columnName && this.option.SortDirection == direction);
  }
  
  getItems(){

    this.moduleService.getItems('activitylog',this.option)
    .subscribe(response => { 
      
      this.items = response.items;
      this.totalRecord =  response.totalRecord;
      this.recordFrom = this.option.RecordPerPage * (this.option.CurrentPage - 1) + 1;
      this.recordTo = this.option.RecordPerPage * this.option.CurrentPage;
      this.recordTo = (this.recordTo < this.totalRecord) ? this.recordTo : this.totalRecord;
      this.recordFrom = (this.totalRecord == 0) ? 0 : this.recordFrom;

      this.isLoaded = true;
    });
  }


  
}
