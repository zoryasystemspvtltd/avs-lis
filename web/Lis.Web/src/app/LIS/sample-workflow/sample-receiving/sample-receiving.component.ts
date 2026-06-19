import { Component, OnInit } from '@angular/core';
import { AlertService } from '../../../_services/alert.service';
import { SampleWorkflowService } from '../../../_services/sample-workflow.service';
import { extractApiError } from '../../../_helpers/api-error';

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

  barcodeNumber = '';
  orderNumber = '';
  patientName = '';
  searchText = '';
  rejectionReasons: any[] = [];

  selectedRow: any = null;
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
        this.rows = r.items || [];
        this.totalRecord = r.totalRecord || 0;
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

  scanBarcode(): void {
    if (!this.barcodeNumber.trim()) {
      return;
    }
    this.workflowService.getByBarcode('receiving', this.barcodeNumber.trim()).subscribe(
      row => {
        this.rows = [row];
        this.totalRecord = 1;
      },
      err => this.alertService.error(extractApiError(err))
    );
  }

  openReceive(row: any): void {
    this.selectedRow = row;
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    this.receiveDateTime = now.toISOString().slice(0, 16);
    this.receiveRemarks = '';
    this.showReceiveModal = true;
  }

  submitReceive(): void {
    this.workflowService.receiveSample({
      testRequestId: this.selectedRow.id,
      receivedDateTime: this.receiveDateTime,
      remarks: this.receiveRemarks,
      barcodeNumber: this.selectedRow.sampleNo
    }).subscribe(
      () => {
        this.alertService.success('Sample received successfully.');
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

  recollect(row: any): void {
    if (!confirm('Trigger recollection for this sample?')) {
      return;
    }
    this.workflowService.recollectReceiving(row.id).subscribe(
      () => {
        this.alertService.success('Recollection initiated.');
        this.search(this.currentPage);
      },
      err => this.alertService.error(extractApiError(err))
    );
  }
}
