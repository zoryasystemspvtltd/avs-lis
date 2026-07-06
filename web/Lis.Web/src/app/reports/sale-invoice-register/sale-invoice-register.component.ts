import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../_services/report.service';
import { MasterService } from '../../_services/master.service';
import { UserService } from '../../_services/user.service';
import { AlertService } from '../../_services/alert.service';
import { ReportPageBase } from '../report-page.base';
import { ReportFilter } from '../../_services/report.service';
import {
  ExcelColumn,
  exportRowsToExcel,
  reportFileName,
  displayValue
} from '../report-excel-export.util';

@Component({
  selector: 'app-sale-invoice-register',
  templateUrl: './sale-invoice-register.component.html',
  styleUrls: ['../reports.shared.css']
})
export class SaleInvoiceRegisterComponent extends ReportPageBase implements OnInit {
  readonly pageTitle = 'Sale Invoice Register';
  createdById: string = null;
  creators: Array<{ id: string; name: string }> = [];

  constructor(
    reportService: ReportService,
    masterService: MasterService,
    private userService: UserService,
    alertService: AlertService
  ) {
    super(reportService, masterService, alertService);
  }

  ngOnInit() {
    this.initReportPage();
    this.loadCreatorLookups();
  }

  loadCreatorLookups(): void {
    this.userService.getLookup().subscribe(
      list => {
        const items = Array.isArray(list) ? list : [];
        this.creators = items
          .map((x: any) => ({
            id: (x.id ?? x.Id ?? '').toString(),
            name: (x.name ?? x.Name ?? '').trim()
          }))
          .filter((x: { id: string; name: string }) => x.id && x.name);
      },
      () => {
        this.creators = [];
      }
    );
  }

  reset() {
    this.createdById = null;
    this.resetFilters();
  }

  buildFilter(page: number, recordPerPage: number, sortColumn: string): ReportFilter {
    return {
      ...super.buildFilter(page, recordPerPage, sortColumn),
      createdById: this.createdById || null
    };
  }

  protected runSearch(page: number, pageSize: number): void {
    this.currentPage = page;
    this.loading = true;
    this.reportService.getSaleInvoiceRegister(this.buildFilter(page, pageSize, 'InvoiceDate')).subscribe(
      r => {
        this.rows = r.items || [];
        this.totalRecord = r.totalRecord || 0;
        this.searched = true;
        this.loading = false;
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readReportError(err));
      }
    );
  }

  protected fetchAllForExport() {
    return this.reportService.getSaleInvoiceRegister(
      this.buildFilter(1, 0, 'InvoiceDate')
    );
  }

  protected exportRows(rows: any[]): void {
    const columns: ExcelColumn<any>[] = [
      { header: 'Invoice Date', width: 95, type: 'date', value: r => r.invoiceDate },
      { header: 'Invoice No.', width: 110, value: r => displayValue(r.invoiceNo) },
      { header: 'Patient ID', width: 90, value: r => displayValue(r.patientId) },
      { header: 'Patient Name', width: 140, value: r => displayValue(r.patientName) },
      { header: 'Referral Doctor', width: 120, value: r => displayValue(r.referralDoctor) },
      { header: 'Corporate', width: 100, value: r => displayValue(r.corporate) },
      { header: 'Gross', width: 80, type: 'currency', value: r => r.grossAmount },
      { header: 'Discount', width: 80, type: 'currency', value: r => r.discountAmount },
      { header: 'Tax', width: 70, type: 'currency', value: r => r.taxAmount },
      { header: 'Net', width: 80, type: 'currency', value: r => r.netAmount },
      { header: 'Status', width: 90, value: r => r.invoiceStatusName || r.invoiceStatus },
      { header: 'Payment', width: 80, value: r => r.paymentStatusName || r.paymentStatus },
      { header: 'Created By', width: 100, value: r => displayValue(r.createdBy) }
    ];
    exportRowsToExcel('Sale Invoice Register', reportFileName('SaleInvoiceRegister'), columns, rows);
  }
}
