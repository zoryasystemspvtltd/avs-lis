import { Component, OnInit } from '@angular/core';
import { ReportService } from '../../_services/report.service';
import { MasterService } from '../../_services/master.service';
import { AlertService } from '../../_services/alert.service';
import { ReportPageBase } from '../report-page.base';
import {
  ExcelColumn,
  exportRowsToExcel,
  reportFileName,
  displayValue
} from '../report-excel-export.util';

@Component({
  selector: 'app-test-booking-register',
  templateUrl: './test-booking-register.component.html',
  styleUrls: ['../reports.shared.css']
})
export class TestBookingRegisterComponent extends ReportPageBase implements OnInit {
  readonly pageTitle = 'Test Booking Register';

  constructor(
    reportService: ReportService,
    masterService: MasterService,
    alertService: AlertService
  ) {
    super(reportService, masterService, alertService);
  }

  ngOnInit() {
    this.initReportPage();
  }

  search(page = 1) {
    if (!this.validateFilters()) {
      return;
    }
    this.runSearch(page, this.recordPerPage);
  }

  reset() {
    this.resetFilters();
  }

  protected runSearch(page: number, pageSize: number): void {
    this.currentPage = page;
    this.loading = true;
    this.reportService.getTestBookingRegister(this.buildFilter(page, pageSize, 'BookingDate')).subscribe(
      r => {
        this.rows = r.items || [];
        this.totalRecord = r.totalRecord || 0;
        this.searched = true;
        this.loading = false;
      },
      () => {
        this.loading = false;
        this.alertService.error('Failed to load report');
      }
    );
  }

  protected fetchAllForExport() {
    return this.reportService.getTestBookingRegister(
      this.buildFilter(1, 0, 'BookingDate')
    );
  }

  protected exportRows(rows: any[]): void {
    const columns: ExcelColumn<any>[] = [
      { header: 'Booking Date', width: 95, type: 'date', value: r => r.bookingDate },
      { header: 'Request No.', width: 100, value: r => displayValue(r.requestNumber) },
      { header: 'Invoice No.', width: 110, value: r => displayValue(r.invoiceNumber) },
      { header: 'Patient ID', width: 90, value: r => displayValue(r.patientId) },
      { header: 'Patient Name', width: 140, value: r => displayValue(r.patientName) },
      { header: 'Test Name', width: 160, value: r => displayValue(r.testName) },
      { header: 'Department', width: 90, value: r => displayValue(r.department) },
      { header: 'Specimen', width: 90, value: r => displayValue(r.specimen) },
      { header: 'Referral Doctor', width: 120, value: r => displayValue(r.referralDoctor) },
      { header: 'Status', width: 110, value: r => displayValue(r.status) },
      { header: 'Sample No.', width: 100, value: r => displayValue(r.sampleNo) },
      { header: 'Created By', width: 100, value: r => displayValue(r.createdBy) }
    ];
    exportRowsToExcel('Test Booking Register', reportFileName('TestBookingRegister'), columns, rows);
  }
}
