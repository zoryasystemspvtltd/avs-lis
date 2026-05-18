import { Component, OnInit, OnChanges, SimpleChanges, Input } from '@angular/core';
import { ModuleService, AlertService, AuthenticationService, SampleService, MasterService } from '../../_services';
import { OperatorType } from '../../_constants';
import { AuthenticationToken } from '../../_models';
import { map, switchMap, catchError } from 'rxjs/operators';
import { Subscription, timer, of } from 'rxjs';

@Component({
  selector: 'app-list-module',
  templateUrl: './list-module.component.html',
  styleUrls: ['./list-module.component.css']
})
export class ListModuleComponent implements OnInit, OnChanges {
  private user: AuthenticationToken;
  @Input() schemma: any;

  private readonly getAllApiModules = [
    'ReferralDoctor', 'Corporate', 'TestGroup', 'TestCategory', 'Unit', 'Method',
    'SampleType', 'Container', 'TestProfile'
  ];

  constructor(
    private moduleService: ModuleService,
    private masterService: MasterService,
    private alertService: AlertService,
    private authenticationService: AuthenticationService,
    private sampleService: SampleService) { }

  public items: any[];
  public isLoaded: Boolean;
  public loadError: string;

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
    this.initList();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes.schemma && !changes.schemma.firstChange && this.schemma) {
      this.initList();
    }
  }

  private initList() {
    if (!this.schemma) {
      return;
    }
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
    this.items = [];
    this.isLoaded = false;
    this.loadError = null;
    if (this.schemma.module === 'HisTest') {
      this.option.SortColumnName = 'HISTestCode';
    }
    if (this.schemma.module === 'TestRate') {
      this.option.SortColumnName = 'EffectiveStart';
    }
    if (this.schemma.module === 'SaleInvoice') {
      this.option.SortColumnName = 'InvoiceDate';
    }
    if (this.schemma.module === 'HisParameterMaster') {
      this.option.SortColumnName = 'HISParamCode';
    }
    if (this.schemma.module === 'TestMappingMaster') {
      this.option.SortColumnName = 'HISTestCode';
    }
    if (this.schemma.module === 'PatientMaster') {
      this.option.SortColumnName = 'Name';
    }
    if (this.schemma.module === 'Patients') {
      this.option.SortColumnName = 'SampleCollectionDate';
      this.option.SortDirection = false;
    }
    if (this.schemma.module === 'Quality') {
      this.option.SortColumnName = 'SampleNo';
    }
    if (this.schemma.filterStatus != null) {
      this.filterStatus = this.schemma.filterStatus;
    }
    if (this.schemma.allowedFilter) {
      const filter = this.schemma.allowedFilter.find(e => e === this.authenticationService.selectedStatus);
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
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  refreshItems() {
    this.setFilter();
    this.authenticationService.CurrentPage = this.option.CurrentPage;

    const masterApiModules = [
      'HisTest', 'TestRate', 'SaleInvoice', 'HisParameterMaster', 'HisParameterRangeMaster',
      'TestMappingMaster', 'PatientMaster', 'EquipmentHeartbeat'
    ];
    const source$ = this.shouldUseGetAll()
      ? this.masterService.getAll(this.schemma.module)
      : (masterApiModules.indexOf(this.schemma.module) >= 0
        ? this.masterService.getItems(this.schemma.module, this.option)
        : this.moduleService.getItems(this.schemma.module, this.option));

    return source$.pipe(
      map(response => this.normalizeListResponse(response)),
      map(response => {
        if (response.items && response.items.length > 0) {
          this.loadError = null;
        }
        if (!this.items) {
          this.items = [];
        }

        const oldItems = this.items.map(x => x);
        this.items = response.items || [];
        this.items.forEach(i => {
          const oldItem = oldItems.filter(o => o.id === i.id);
          if (oldItem && oldItem.length > 0) {
            i.IsSelected = oldItem[0].IsSelected;
          }
        });
        this.totalRecord = response.totalRecord;
        this.recordFrom = this.option.RecordPerPage * (this.option.CurrentPage - 1) + 1;
        this.recordTo = this.option.RecordPerPage * this.option.CurrentPage;
        this.recordTo = (this.recordTo < this.totalRecord) ? this.recordTo : this.totalRecord;
        this.recordFrom = (this.totalRecord === 0) ? 0 : this.recordFrom;
        this.isLoaded = true;
        if (this.items.length === 0 && !this.loadError) {
          this.loadError = null;
        }
        return response;
      }),
      catchError(() => {
        this.items = [];
        this.totalRecord = 0;
        this.recordFrom = 0;
        this.recordTo = 0;
        this.isLoaded = true;
        this.loadError = 'Unable to load records. Check API connection and permissions.';
        return of(null);
      })
    );
  }

  private shouldUseGetAll(): boolean {
    return this.schemma.useGetAll === true
      || this.getAllApiModules.indexOf(this.schemma.module) >= 0;
  }

  private normalizeListResponse(response: any): { items: any[], totalRecord: number } {
    if (response == null) {
      this.loadError = 'No data returned from server.';
      return { items: [], totalRecord: 0 };
    }

    if (Array.isArray(response)) {
      return this.paginateClientList(response);
    }

    const items = response.items || response.Items || [];
    const totalRecord = response.totalRecord != null
      ? response.totalRecord
      : (response.TotalRecord != null ? response.TotalRecord : items.length);

    return { items, totalRecord };
  }

  private paginateClientList(allItems: any[]): { items: any[], totalRecord: number } {
    let filtered = allItems.slice();
    if (this.searchText) {
      const q = this.searchText.toLowerCase();
      filtered = filtered.filter(item => {
        const haystack = [
          item.name, item.Name, item.code, item.Code,
          item.hisTestCode, item.hisTestCodeDescription,
          item.hisParamCode, item.hisParamDescription,
          item.hisSpecimenName, item.departmentName,
          item.invoiceNo, item.patientName, item.phone,
          item.hisPatientId, item.lisTestCode
        ].filter(v => v != null).join(' ').toLowerCase();
        return haystack.indexOf(q) >= 0;
      });
    }

    const totalRecord = filtered.length;
    const start = this.option.RecordPerPage * (this.option.CurrentPage - 1);
    const items = filtered.slice(start, start + this.option.RecordPerPage);
    return { items, totalRecord };
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

    let module = this.schemma.module;
    if (this.schemma.module == 'Patients') {
      module = 'Samples';
    }
    let acc = this.user.access.find(element => element.name == module);
    if (acc == null) {
      const setupApiModules = ['Department', 'Specimens', 'ReferralDoctor', 'Corporate', 'TestGroup',
        'TestCategory', 'Unit', 'Method', 'SampleType', 'Container', 'TestProfile',
        'HisParameterMaster', 'HisParameterRangeMaster', 'TestMappingMaster', 'PatientMaster'];
      if (setupApiModules.indexOf(module) >= 0) {
        acc = this.user.access.find(element => element.name == 'Masters');
      } else if (module === 'TestRate') {
        acc = this.user.access.find(element => element.name == 'TestRates');
      } else if (module === 'SaleInvoice') {
        acc = this.user.access.find(element => element.name == 'SaleInvoices');
      }
    }
    if (acc == null) {
      return false;
    }
    return ((parseInt(acc.access, 10) & type) === type);
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
