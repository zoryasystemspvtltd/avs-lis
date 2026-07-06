import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AlertService } from '../../../_services/alert.service';
import { SampleWorkflowService } from '../../../_services/sample-workflow.service';
import { extractApiError } from '../../../_helpers/api-error';

@Component({
  selector: 'app-radiology-approved-reports',
  templateUrl: './radiology-approved-reports.component.html',
  styleUrls: ['./radiology-approved-reports.component.css']
})
export class RadiologyApprovedReportsComponent implements OnInit {
  rows: any[] = [];
  loading = false;
  totalRecord = 0;
  currentPage = 1;
  recordPerPage = 25;
  searchText = '';

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
    this.workflowService.getRadiologyApprovedQueue({
      currentPage: page,
      recordPerPage: this.recordPerPage,
      searchText: this.searchText
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

  printReport(row: any): void {
    this.router.navigate(['/reports/radiology-report'], { queryParams: { id: row.id } });
  }
}
