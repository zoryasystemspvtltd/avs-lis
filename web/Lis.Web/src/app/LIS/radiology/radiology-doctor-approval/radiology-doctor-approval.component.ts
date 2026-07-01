import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AlertService } from '../../../_services/alert.service';
import { SampleWorkflowService } from '../../../_services/sample-workflow.service';
import { extractApiError } from '../../../_helpers/api-error';

@Component({
  selector: 'app-radiology-doctor-approval',
  templateUrl: './radiology-doctor-approval.component.html',
  styleUrls: ['./radiology-doctor-approval.component.css']
})
export class RadiologyDoctorApprovalComponent implements OnInit {
  rows: any[] = [];
  loading = false;
  totalRecord = 0;
  currentPage = 1;
  recordPerPage = 25;
  searchText = '';
  modality = '';
  digitalSignature = '';
  selectedId: number = null;
  reportDetail: any = null;
  showDetail = false;

  constructor(
    private workflowService: SampleWorkflowService,
    private alertService: AlertService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.search();
  }

  search(page: number = 1): void {
    this.currentPage = page;
    this.loading = true;
    this.workflowService.getRadiologyDoctorApprovalQueue({
      currentPage: page,
      recordPerPage: this.recordPerPage,
      searchText: this.searchText,
      modality: this.modality
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

  openApproval(row: any): void {
    this.selectedId = row.id;
    this.workflowService.getRadiologyDoctorApprovalReport(row.id).subscribe(
      detail => {
        this.reportDetail = detail;
        this.digitalSignature = '';
        this.showDetail = true;
      },
      err => this.alertService.error(extractApiError(err))
    );
  }

  authorize(release: boolean): void {
    if (!this.digitalSignature.trim()) {
      this.alertService.error('Digital signature is required.');
      return;
    }
    this.workflowService.authorizeRadiologyReport({
      radiologyRequestId: this.selectedId,
      digitalSignature: this.digitalSignature,
      release
    }).subscribe(
      () => {
        this.alertService.success(release ? 'Report released.' : 'Report authorized.');
        this.showDetail = false;
        this.search(this.currentPage);
      },
      err => this.alertService.error(extractApiError(err))
    );
  }

  goToPrint(): void {
    if (this.selectedId) {
      this.router.navigate(['/reports/radiology-report'], { queryParams: { id: this.selectedId } });
    }
  }
}
