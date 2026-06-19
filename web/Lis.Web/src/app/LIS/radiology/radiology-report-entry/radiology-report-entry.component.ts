import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AlertService } from '../../../_services/alert.service';
import { SampleWorkflowService } from '../../../_services/sample-workflow.service';
import { extractApiError } from '../../../_helpers/api-error';

@Component({
  selector: 'app-radiology-report-entry',
  templateUrl: './radiology-report-entry.component.html',
  styleUrls: ['./radiology-report-entry.component.css']
})
export class RadiologyReportEntryComponent implements OnInit {
  rows: any[] = [];
  loading = false;
  searchText = '';
  modality = '';

  clinicalHistory = '';
  findings = '';
  impression = '';
  recommendation = '';
  digitalSignature = '';
  selectedId: number = null;
  reportDetail: any = null;
  showEntry = false;

  constructor(
    private workflowService: SampleWorkflowService,
    private alertService: AlertService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.loading = true;
    this.workflowService.getRadiologyQueue({
      currentPage: 1,
      recordPerPage: 100,
      searchText: this.searchText,
      modality: this.modality
    }).subscribe(
      r => {
        this.rows = r.items || [];
        this.loading = false;
      },
      err => {
        this.loading = false;
        this.alertService.error(extractApiError(err));
      }
    );
  }

  openEntry(row: any): void {
    this.selectedId = row.id;
    this.workflowService.getRadiologyReport(row.id).subscribe(
      detail => {
        this.reportDetail = detail;
        this.clinicalHistory = detail.clinicalHistory || '';
        this.findings = detail.findings || '';
        this.impression = detail.impression || '';
        this.recommendation = detail.recommendation || '';
        this.showEntry = true;
      },
      err => this.alertService.error(extractApiError(err))
    );
  }

  saveDraft(): void {
    this.save(false);
  }

  submitReview(): void {
    this.save(true);
  }

  private save(submitForReview: boolean): void {
    if (!this.findings.trim() || !this.impression.trim()) {
      this.alertService.error('Findings and impression are mandatory.');
      return;
    }
    this.workflowService.saveRadiologyReport({
      radiologyRequestId: this.selectedId,
      clinicalHistory: this.clinicalHistory,
      findings: this.findings,
      impression: this.impression,
      recommendation: this.recommendation,
      submitForReview
    }).subscribe(
      () => {
        this.alertService.success(submitForReview ? 'Submitted for review.' : 'Draft saved.');
        this.showEntry = false;
        this.search();
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
        this.showEntry = false;
        this.search();
      },
      err => this.alertService.error(extractApiError(err))
    );
  }
}
