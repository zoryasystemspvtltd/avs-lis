import { Component, OnInit } from '@angular/core';
import { AlertService } from '../../../_services/alert.service';
import { SampleWorkflowService } from '../../../_services/sample-workflow.service';
import { extractApiError } from '../../../_helpers/api-error';

@Component({
  selector: 'app-sample-collection',
  templateUrl: './sample-collection.component.html',
  styleUrls: ['./sample-collection.component.css']
})
export class SampleCollectionComponent implements OnInit {
  rows: any[] = [];
  loading = false;
  totalRecord = 0;
  currentPage = 1;
  recordPerPage = 25;

  searchText = '';
  uhid = '';
  barcodeNumber = '';
  orderNumber = '';
  patientName = '';
  collectionDate = '';

  selectedRow: any = null;
  collectDateTime = '';
  collectRemarks = '';
  rejectRemarks = '';
  showCollectModal = false;
  showRejectModal = false;
  showBarcodeModal = false;
  barcodeValue = '';
  barcodeText = '';

  constructor(
    private workflowService: SampleWorkflowService,
    private alertService: AlertService
  ) { }

  ngOnInit(): void {
    this.search(1);
  }

  search(page: number): void {
    this.currentPage = page;
    this.loading = true;
    this.workflowService.getCollectionQueue({
      currentPage: page,
      recordPerPage: this.recordPerPage,
      searchText: this.searchText,
      uhid: this.uhid,
      barcodeNumber: this.barcodeNumber,
      orderNumber: this.orderNumber,
      patientName: this.patientName,
      collectionDate: this.collectionDate || null
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

  reset(): void {
    this.searchText = '';
    this.uhid = '';
    this.barcodeNumber = '';
    this.orderNumber = '';
    this.patientName = '';
    this.collectionDate = '';
    this.search(1);
  }

  openCollect(row: any): void {
    this.selectedRow = row;
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    this.collectDateTime = now.toISOString().slice(0, 16);
    this.collectRemarks = '';
    this.showCollectModal = true;
  }

  submitCollect(): void {
    if (!this.selectedRow || !this.collectDateTime) {
      this.alertService.error('Collection date and time are mandatory.');
      return;
    }
    this.workflowService.collectSample({
      testRequestId: this.selectedRow.id,
      collectionDateTime: this.collectDateTime,
      remarks: this.collectRemarks,
      barcodeNumber: this.selectedRow.sampleNo
    }).subscribe(
      () => {
        this.alertService.success('Sample collected successfully.');
        this.showCollectModal = false;
        this.search(this.currentPage);
      },
      err => this.alertService.error(extractApiError(err))
    );
  }

  openReject(row: any): void {
    this.selectedRow = row;
    this.rejectRemarks = '';
    this.showRejectModal = true;
  }

  submitReject(): void {
    if (!this.selectedRow || !this.rejectRemarks.trim()) {
      this.alertService.error('Rejection reason is mandatory.');
      return;
    }
    this.workflowService.rejectCollection({
      testRequestId: this.selectedRow.id,
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
    if (!confirm('Initiate recollection for this sample?')) {
      return;
    }
    this.workflowService.recollectCollection(row.id).subscribe(
      () => {
        this.alertService.success('Recollection initiated.');
        this.search(this.currentPage);
      },
      err => this.alertService.error(extractApiError(err))
    );
  }

  printBarcode(row: any): void {
    this.workflowService.ensureBarcode(row.id).subscribe(
      r => {
        this.barcodeValue = r.barcode || row.sampleNo;
        const p = row.patientName || '';
        this.barcodeText = `${p}####`;
        this.showBarcodeModal = true;
      },
      err => this.alertService.error(extractApiError(err))
    );
  }

  scanBarcode(): void {
    if (!this.barcodeNumber.trim()) {
      return;
    }
    this.workflowService.getByBarcode('collection', this.barcodeNumber.trim()).subscribe(
      row => {
        this.rows = [row];
        this.totalRecord = 1;
      },
      err => this.alertService.error(extractApiError(err))
    );
  }
}
