import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ReportService } from '../../_services/report.service';
import { MasterService } from '../../_services/master.service';
import { AlertService } from '../../_services/alert.service';
import { ExcelColumn, exportRowsToExcel, reportFileName, displayValue } from '../report-excel-export.util';

@Component({
  selector: 'app-commission-report',
  templateUrl: './commission-report.component.html',
  styleUrls: ['../reports.shared.css']
})
export class CommissionReportComponent implements OnInit {
  pageTitle = 'Commission';
  kind: 'doctor' | 'collector' = 'doctor';
  format: 'Summary' | 'Detail' = 'Summary';
  searchedFormat: 'Summary' | 'Detail' = 'Summary';
  fromDate = '';
  toDate = '';
  recipientId: number = null;
  people: Array<{ id: number; name: string }> = [];
  rows: any[] = [];
  totalRecord = 0;
  totalAmount = 0;
  loading = false;
  exporting = false;
  searched = false;
  currentPage = 1;
  recordPerPage = 25;

  constructor(
    private route: ActivatedRoute,
    private reportService: ReportService,
    private masterService: MasterService,
    private alertService: AlertService
  ) { }

  ngOnInit() {
    const data = this.route.snapshot.data || {};
    this.kind = data.recipient === 'collector' ? 'collector' : 'doctor';
    this.pageTitle = data.title || (this.kind === 'doctor' ? 'Doctor Commission' : 'Collector Commission');
    const today = new Date();
    const first = new Date(today.getFullYear(), today.getMonth(), 1);
    this.toDate = this.formatDate(today);
    this.fromDate = this.formatDate(first);
    const lookup = this.kind === 'doctor' ? 'ReferralDoctor' : 'SampleCollector';
    this.masterService.getLookupList(lookup).subscribe(list => {
      const items = Array.isArray(list) ? list : [];
      this.people = items
        .filter((x: any) => x.isActive !== false && x.IsActive !== false)
        .map((x: any) => ({ id: +(x.id ?? x.Id), name: (x.name ?? x.Name ?? '').trim() }))
        .filter(x => x.id > 0 && x.name);
    });
  }

  get recordFrom(): number {
    return this.totalRecord === 0 ? 0 : (this.currentPage - 1) * this.recordPerPage + 1;
  }

  get recordTo(): number {
    return Math.min(this.currentPage * this.recordPerPage, this.totalRecord);
  }

  search(page = 1): void {
    if (!this.fromDate || !this.toDate) {
      this.alertService.error('From Date and To Date are required.');
      return;
    }
    if (this.fromDate > this.toDate) {
      this.alertService.error('From Date cannot be after To Date.');
      return;
    }
    this.currentPage = page;
    this.loading = true;
    this.reportService.getCommissionReport(this.kind, this.filter(page, this.recordPerPage)).subscribe(
      r => {
        this.searchedFormat = this.format;
        this.rows = r.items || [];
        this.totalRecord = r.totalRecord || 0;
        this.totalAmount = r.totalAmount || 0;
        this.searched = true;
        this.loading = false;
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err));
      }
    );
  }

  exportExcel(): void {
    if (!this.searched || this.totalRecord === 0) {
      this.alertService.error('No data to export. Run Search first.');
      return;
    }
    this.exporting = true;
    this.reportService.getCommissionReport(this.kind, this.filter(1, 0)).subscribe(
      r => {
        this.exporting = false;
        const items = r.items || [];
        if (!items.length) {
          this.alertService.error('No records found for export.');
          return;
        }
        exportRowsToExcel(this.pageTitle, reportFileName(this.kind === 'doctor' ? 'DoctorCommission' : 'CollectorCommission'), this.columns(), items);
        this.alertService.success(`Exported ${items.length} record(s).`);
      },
      err => {
        this.exporting = false;
        this.alertService.error(this.readError(err));
      }
    );
  }

  private filter(page: number, pageSize: number) {
    return {
      fromDate: this.fromDate,
      toDate: this.toDate,
      referralDoctorId: this.kind === 'doctor' ? this.recipientId : null,
      collectorId: this.kind === 'collector' ? this.recipientId : null,
      commissionFormat: this.format,
      currentPage: page,
      recordPerPage: pageSize
    };
  }

  private columns(): ExcelColumn<any>[] {
    if (this.format === 'Detail') {
      return [
        { header: 'Invoice No', width: 110, value: r => displayValue(r.invoiceNo) },
        { header: 'Invoice Date', width: 95, type: 'date', value: r => r.invoiceDate },
        { header: 'Recipient', width: 140, value: r => displayValue(r.recipientName) },
        { header: 'Commission Base', width: 110, type: 'currency', value: r => r.commissionBase },
        { header: 'Rate', width: 70, value: r => r.commissionPercent },
        { header: 'Amount', width: 90, type: 'currency', value: r => r.amount }
      ];
    }
    return [
      { header: 'Recipient', width: 160, value: r => displayValue(r.recipientName) },
      { header: 'Invoices', width: 80, value: r => r.invoiceCount },
      { header: 'Amount', width: 100, type: 'currency', value: r => r.amount }
    ];
  }

  private formatDate(d: Date): string {
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${d.getFullYear()}-${m}-${day}`;
  }

  private readError(err: any): string {
    if (!err) { return 'Failed to load report'; }
    if (typeof err.error === 'string') { return err.error; }
    return err.error?.Message || err.error?.message || err.message || 'Failed to load report';
  }
}
