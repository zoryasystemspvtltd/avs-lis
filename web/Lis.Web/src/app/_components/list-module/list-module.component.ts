import { Component, OnInit, Input } from '@angular/core';
import { ModuleService, AlertService, AuthenticationService, SampleService } from '../../_services';
import { OperatorType } from '../../_constants';
import { AuthenticationToken } from '../../_models';
import { map, switchMap } from 'rxjs/operators';
import { Observable, Subscription, timer } from 'rxjs';
import { MapOperator } from 'rxjs/internal/operators/map';

@Component({
  selector: 'app-list-module',
  templateUrl: './list-module.component.html',
  styleUrls: ['./list-module.component.css']
})
export class ListModuleComponent implements OnInit {
  private user: AuthenticationToken;
  @Input() schemma: any;

  constructor(private moduleService: ModuleService,
    private alertService: AlertService,
    private authenticationService: AuthenticationService,
    private sampleService: SampleService) {

  }

  public items: any[];
  public isLoaded: Boolean;

  public option = {
    'RecordPerPage': this.authenticationService.RecordPerPage,
    'CurrentPage': this.authenticationService.CurrentPage,
    'SortColumnName': this.authenticationService.SortColumnName,
    'SortDirection': this.authenticationService.SortDirection
  };

  public recordFrom: number;
  public totalRecord: number;
  public recordTo: number;
  public isSelectAll: boolean = false;
  public searchText: string = '';
  public filterStatus: number = 0;

  ngOnInit() {
    this.user = this.authenticationService.currentUserValue;
    if (this.schemma.filterStatus != null) {
      this.filterStatus = this.schemma.filterStatus;
    }

    if (this.schemma.allowedFilter) {
      let filter = this.schemma.allowedFilter.find(e => e === this.authenticationService.selectedStatus)
      if (filter != null) {
        this.filterStatus = this.authenticationService.selectedStatus;
      }
    }

    this.getItems();
  }

  subscription: Subscription

  getItems() {
    if (!this.schemma.auto_refresh) {
      this.subscription = this.refreshItems().subscribe(() => { });
    }
    else {
      this.subscription = timer(0, 60000).pipe(
        switchMap(() => this.refreshItems())
      ).subscribe(() => { });
    }
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }

  refreshItems() {

    this.setFilter();
    this.authenticationService.CurrentPage = this.option.CurrentPage;

    return this.moduleService.getItems(this.schemma.module, this.option)
      .pipe(map(response => {
        if(!this.items){
          this.items = [];
        }
        let oldItems = this.items.map(x => x);
        this.items = response.items;
        this.items.forEach(i => {
          let oldItem = oldItems.filter(o => o.id === i.id);

          if (oldItem && oldItem.length > 0) {
            i.IsSelected = oldItem[0].IsSelected;
          }
        })
        this.totalRecord = response.totalRecord;
        this.recordFrom = this.option.RecordPerPage * (this.option.CurrentPage - 1) + 1;
        this.recordTo = this.option.RecordPerPage * this.option.CurrentPage;
        this.recordTo = (this.recordTo < this.totalRecord) ? this.recordTo : this.totalRecord;
        this.recordFrom = (this.totalRecord == 0) ? 0 : this.recordFrom;

        this.isLoaded = true;
        return response;
      }));
  }

  sort = function (columnName: string) {
    this.option.SortColumnName = columnName;
    this.option.SortDirection = !this.option.SortDirection;

    this.authenticationService.SortColumnName = columnName,
      this.authenticationService.SortDirection = this.option.SortDirection;

    this.getItems();
  }

  showSortIcon = function (columnName: string, direction: boolean) {
    return !(this.option.SortColumnName == columnName && this.option.SortDirection == direction);
  }

  selectAll = function () {
    for (var i = 0; i < this.items.length; i++) {
      this.items[i].IsSelected = this.isSelectAll;
    }

    this.generateBarcode();
  }

  deSelectAll = function () {
    var isAllSelected = true;

    for (var i = 0; i < this.items.length; i++) {
      if (this.items[i].IsSelected !== true) {
        isAllSelected = false;
        break;
      }
    }

    this.generateBarcode();
    this.isSelectAll = isAllSelected;
  }

  get isAnySelected(){
    for (var i = 0; i < this.items.length; i++) {
      if (this.items[i].IsSelected === true) {
        return true;
      }
    }
    return false;
  }
  setFilter = function () {
    if (this.option.RecordPerPage == null) {
      this.option.RecordPerPage = 1;
    }

    this.option.SearchCondition = null;

    if (this.filterStatus >= 0) {
      this.option.SearchCondition = {
        'Name': 'Status',
        'Value': this.filterStatus
      };

      this.option.Status = this.filterStatus;
    }

    if (this.searchText != '') {
      if (this.option.SearchCondition == null) {
        this.option.SearchCondition = {
          'Name': 'Name',
          'Value': this.searchText,
          'Operator': OperatorType.Likelihood
        };
      }
      else {
        this.option.SearchCondition = {
          'Name': 'Name',
          'Value': this.searchText,
          'Operator': OperatorType.Likelihood,
          'And': this.option.SearchCondition
        };
      }
    }

    this.option.SearchText = this.searchText;
  }

  doSearch = function () {
    this.getItems();
  }

  get isCorrectStatus(){
    if(this.filterStatus == 1
      || this.filterStatus == 2
      || this.filterStatus == 3
      || this.filterStatus == 4
      || this.filterStatus == 5
      || this.filterStatus == 6){
      return true;
    }
    return false
  }

  setFilterStatus = function (status) {
    this.filterStatus = status;
    this.authenticationService.selectedStatus = status;
    this.getItems();
    this.isSelectAll = false;
  }
  note:string;
  editAll = function (status: number) {

    var Ids = [];
    for (var i = 0; i < this.items.length; i++) {
      if (this.items[i].IsSelected == true) {

        let request = {
          note: this.note,
          status: status,
          id: this.items[i].id,
          runIndex: 0
        };
    
        Ids.push(request);
        this.items[i].status = status;
      }
    }

    this.moduleService.editItem(this.schemma.module, Ids)
      .subscribe(response => {
        this.getItems();
        this.isSelectAll = false;
      });
  }

  hasAccess = function (type: number) { 

    let _hasAccess: boolean = false;
    let module = this.schemma.module;
    if(this.schemma.module == 'Patients'){
      module = 'Samples'
    }
    let acc = this.user.access.find(element => element.name == module);

    if (acc == null) {
      return false;
    }

    _hasAccess = ((parseInt(acc.access) & type) == type);

    return _hasAccess;
  }

  allowFilter(filter: number) {
    let allowed = false;
    if (this.schemma.allowedFilter) {
      this.schemma.allowedFilter.forEach(e => {
        if (e === filter) {
          allowed = true;
          return;
        }
      })
    }
    return allowed;
  }

  onChangePageSize(val) {
    this.option.RecordPerPage = val;
    this.authenticationService.RecordPerPage = val,

      this.getItems();
  }

  printAbleItems: any[] = [];
  print() {
    if (this.printAbleItems.length === 0) {
      let message = 'Select item to print';
      this.alertService.error(message);
    }
  }

  generateBarcode() {

    this.printAbleItems = [];
    this.items.forEach(e => {
      if (e.IsSelected) {
        this.sampleService.getBarcode(e.sampleNo)
          .subscribe(response => {
            let existing = this.printAbleItems.find(i => i.sampleNo === e.sampleNo);
            if (!existing) {
              this.printAbleItems.push({
                sampleNo: e.sampleNo,
                barcodeText: response
              });
            }
          });
      }
    });
  }

  
}
