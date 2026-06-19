import { Component, OnInit } from '@angular/core';
import { AlertService } from '../../../_services/alert.service';
import { SampleWorkflowService } from '../../../_services/sample-workflow.service';
import { extractApiError } from '../../../_helpers/api-error';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-sample-receiving',
  templateUrl: './sample-receiving.component.html',
  styleUrls: ['./sample-receiving.component.css']
})
export class SampleReceivingComponent implements OnInit {
  rows: any[] = [];
  loading = false;
  totalRecord = 0;
  currentPage = 1;
  recordPerPage = 25;
  selectAll = false;

  barcodeNumber = '';
  orderNumber = '';
  patientName = '';
  searchText = '';
  rejectionReasons: any[] = [];

  selectedRow: any = null;
  selectedRows: any[] = [];
  bulkReceiveMode = false;
  receiveDateTime = '';
  receiveRemarks = '';
  rejectReasonCode = '';
  rejectRemarks = '';
  showReceiveModal = false;
  showRejectModal = false;

  constructor(
    private workflowService: SampleWorkflowService,
    private alertService: AlertService
  ) { }

  ngOnInit(): void {
    this.workflowService.getRejectionReasons().subscribe(r => this.rejectionReasons = r || []);
    this.search(1);
  }

  get hasSelection(): boolean {
    return this.rows.some(r => r.selected);
  }

  search(page: number): void {
    this.currentPage = page;
    this.loading = true;
    this.workflowService.getReceivingQueue({
      currentPage: page,
      recordPerPage: this.recordPerPage,
      searchText: this.searchText,
      barcodeNumber: this.barcodeNumber,
      orderNumber: this.orderNumber,
      patientName: this.patientName
    }).subscribe(
      r => {
        this.rows = (r.items || []).map(row => ({ ...row, selected: false }));
        this.totalRecord = r.totalRecord || 0;
        this.selectAll = false;
        this.loading = false;
      },
      err => {
        this.loading = false;
        this.alertService.error(extractApiError(err));
      }
    );
  }

  get recordFrom(): number {
    return this.totalRecord === 0 ? 0 : (this.currentPage - 1) * this.recordPerPage + 1;
  }

  get recordTo(): number {
    return Math.min(this.currentPage * this.recordPerPage, this.totalRecord);
  }

  reset(): void {
    this.barcodeNumber = '';
    this.orderNumber = '';
    this.patientName = '';
    this.searchText = '';
    this.search(1);
  }

  toggleSelectAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectAll = checked;
    this.rows.forEach(r => r.selected = checked);
  }

  onRowSelectionChange(): void {
    this.selectAll = this.rows.length > 0 && this.rows.every(r => r.selected);
  }

  openReceive(row: any): void {
    this.bulkReceiveMode = false;
    this.selectedRow = row;
    this.selectedRows = [row];
    this.prepareReceiveModal();
  }

  openBulkReceive(): void {
    this.selectedRows = this.rows.filter(r => r.selected);
    if (!this.selectedRows.length) {
      this.alertService.error('Select at least one sample.');
      return;
    }
    this.bulkReceiveMode = true;
    this.selectedRow = null;
    this.prepareReceiveModal();
  }

  private prepareReceiveModal(): void {
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    this.receiveDateTime = now.toISOString().slice(0, 16);
    this.receiveRemarks = '';
    this.showReceiveModal = true;
  }

  submitReceive(): void {
    const targets = this.bulkReceiveMode ? this.selectedRows : (this.selectedRow ? [this.selectedRow] : []);
    if (!targets.length) {
      this.alertService.error('No sample selected.');
      return;
    }

    const requests = targets.map(row => this.workflowService.receiveSample({
      testRequestId: row.id,
      receivedDateTime: this.receiveDateTime,
      remarks: this.receiveRemarks,
      barcodeNumber: row.sampleNo
    }));

    forkJoin(requests).subscribe(
      () => {
        this.alertService.success(targets.length > 1
          ? `${targets.length} samples received successfully.`
          : 'Sample received successfully.');
        this.showReceiveModal = false;
        this.search(this.currentPage);
      },
      err => this.alertService.error(extractApiError(err))
    );
  }

  openReject(row: any): void {
    this.selectedRow = row;
    this.rejectReasonCode = '';
    this.rejectRemarks = '';
    this.showRejectModal = true;
  }

  submitReject(): void {
    if (!this.rejectReasonCode) {
      this.alertService.error('Rejection reason is mandatory.');
      return;
    }
    this.workflowService.rejectReceiving({
      testRequestId: this.selectedRow.id,
      rejectionReasonCode: this.rejectReasonCode,
      remarks: this.rejectRemarks
    }).subscribe(
      () => {
        this.alertService.success('Sample rejected.');
        this.showRejectModal = false;
        this.search(this.currentPage);
      },
      err => this.alertService.error(extractApiError(err))
    );
  }
}
