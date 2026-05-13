import { Component, OnInit, Input } from '@angular/core';
import { AuthenticationToken } from '../../_models';
import { ModuleService, AlertService, AuthenticationService } from '../../_services';
import { OperatorType } from '../../_constants';

@Component({
  selector: 'app-chield-list',
  templateUrl: './chield-list.component.html',
  styleUrls: ['./chield-list.component.css']
})
export class ChieldListComponent implements OnInit {

  private user: AuthenticationToken;
  @Input() schemma: any;
  @Input() filterValue: any;
  @Input() filterName: any;

  constructor(private moduleService: ModuleService,
    private alertService: AlertService,
    private authenticationService: AuthenticationService) {

  }

  public items: any[];
  public isLoaded: Boolean;

  public option = {
    'RecordPerPage': 10,
    'CurrentPage': 1,
    'SortColumnName': 'Name',
    'SortDirection': true
  };

  public recordFrom: number;
  public totalRecord: number;
  public recordTo: number;
  public isSelectAll: boolean = false;
  public searchText: string = '';
  public filterStatus: number;

  ngOnInit() {
    this.user = this.authenticationService.currentUserValue;

    this.getItems();
  }

  getItems() {
    
    this.setFilter();

    this.moduleService.getItems(this.schemma.module, this.option)
      .subscribe(response => {
        this.items = response.items;
        this.totalRecord = response.totalRecord;
        this.recordFrom = this.option.RecordPerPage * (this.option.CurrentPage - 1) + 1;
        this.recordTo = this.option.RecordPerPage * this.option.CurrentPage;
        this.recordTo = (this.recordTo < this.totalRecord) ? this.recordTo : this.totalRecord;
        this.recordFrom = (this.totalRecord == 0) ? 0 : this.recordFrom;

        this.isLoaded = true;
      });
  }

  sort = function (columnName: string) {
    this.option.SortColumnName = columnName;
    this.option.SortDirection = !this.option.SortDirection;
    this.getItems();
  }

  showSortIcon = function (columnName: string, direction: boolean) {
    return !(this.option.SortColumnName == columnName && this.option.SortDirection == direction);
  }

  selectAll = function () {
    for (var i = 0; i < this.items.length; i++) {
      this.items[i].IsSelected = this.isSelectAll;
    }
  }

  deSelectAll = function () {
    var isAllSelected = true;

    for (var i = 0; i < this.items.length; i++) {
      if (this.items[i].IsSelected !== true) {
        isAllSelected = false;
        break;
      }
    }

    this.isSelectAll = isAllSelected;
  }

  setFilter = function () {
    if (this.option.RecordPerPage == null) {
      this.option.RecordPerPage = 1;
    }

    this.option.SearchCondition = null;

    this.option.SearchCondition = {
      'Name': this.filterName,
      'Value': this.filterValue
    };

    if (this.filterStatus > 0) {
      this.option.SearchCondition = {
        'Name': 'Status',
        'Value': this.filterStatus,
        'And': this.option.SearchCondition
      };
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
  }

  doSearch = function () {
    this.getItems();
  }

  setFilterStatus = function (status) {
    this.filterStatus = status;

    this.getItems();
  }

  editAll = function (status: number) {

    var Ids = [];
    for (var i = 0; i < this.items.length; i++) {
      if (this.items[i].IsSelected == true) {
        Ids.push(this.items[i].id);
        this.items[i].status = status;
      }
    }

    this.moduleService.deleteItem(this.schemma.module, Ids, status)
      .subscribe(response => {
        this.getItems();
      });
  }

  hasAccess = function (type: number) {
    let _hasAccess: boolean = false;
    let acc = this.user.access.find(element => element.name == this.schemma.module);

    if (acc == null) {
      _hasAccess = false;
    }

    _hasAccess = ((parseInt(acc.access) & type) == type);

    return _hasAccess;
  }

}
