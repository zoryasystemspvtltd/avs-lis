import { Component, OnInit } from '@angular/core';
import { AlertService } from '../../../_services/alert.service';
import { SampleWorkflowService } from '../../../_services/sample-workflow.service';
import { extractApiError } from '../../../_helpers/api-error';
import { forkJoin } from 'rxjs';

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
  selectAll = false;

  searchText = '';
  barcodeNumber = '';
  orderNumber = '';
  patientName = '';
  orderDate = '';

  selectedRow: any = null;
  selectedRows: any[] = [];
  bulkCollectMode = false;
  collectDateTime = '';
  collectRemarks = '';
  rejectRemarks = '';
  showCollectModal = false;
  showRejectModal = false;

  constructor(
    private workflowService: SampleWorkflowService,
    private alertService: AlertService
  ) { }

  ngOnInit(): void {
    this.search(1);
  }

  get hasSelection(): boolean {
    return this.rows.some(r => r.selected);
  }

  search(page: number): void {
    this.currentPage = page;
    this.loading = true;
    this.workflowService.getCollectionQueue({
      currentPage: page,
      recordPerPage: this.recordPerPage,
      searchText: this.searchText,
      barcodeNumber: this.barcodeNumber,
      orderNumber: this.orderNumber,
      patientName: this.patientName,
      orderDate: this.orderDate || null
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

  reset(): void {
    this.searchText = '';
    this.barcodeNumber = '';
    this.orderNumber = '';
    this.patientName = '';
    this.orderDate = '';
    this.search(1);
  }

  get recordFrom(): number {
    return this.totalRecord === 0 ? 0 : (this.currentPage - 1) * this.recordPerPage + 1;
  }

  get recordTo(): number {
    return Math.min(this.currentPage * this.recordPerPage, this.totalRecord);
  }

  toggleSelectAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectAll = checked;
    this.rows.forEach(r => r.selected = checked);
  }

  onRowSelectionChange(): void {
    this.selectAll = this.rows.length > 0 && this.rows.every(r => r.selected);
  }

  openCollect(row: any): void {
    this.bulkCollectMode = false;
    this.selectedRow = row;
    this.selectedRows = [row];
    this.prepareCollectModal();
  }

  openBulkCollect(): void {
    this.selectedRows = this.rows.filter(r => r.selected);
    if (!this.selectedRows.length) {
      this.alertService.error('Select at least one sample.');
      return;
    }
    this.bulkCollectMode = true;
    this.selectedRow = null;
    this.prepareCollectModal();
  }

  private prepareCollectModal(): void {
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    this.collectDateTime = now.toISOString().slice(0, 16);
    this.collectRemarks = '';
    this.showCollectModal = true;
  }

  submitCollect(): void {
    if (!this.collectDateTime) {
      this.alertService.error('Collection date and time are mandatory.');
      return;
    }

    const targets = this.bulkCollectMode ? this.selectedRows : (this.selectedRow ? [this.selectedRow] : []);
    if (!targets.length) {
      this.alertService.error('No sample selected.');
      return;
    }

    const requests = targets.map(row => this.workflowService.collectSample({
      testRequestId: row.id,
      collectionDateTime: this.collectDateTime,
      remarks: this.collectRemarks,
      barcodeNumber: row.sampleNo
    }));

    forkJoin(requests).subscribe(
      () => {
        this.alertService.success(targets.length > 1
          ? `${targets.length} samples collected successfully.`
          : 'Sample collected successfully.');
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
}
