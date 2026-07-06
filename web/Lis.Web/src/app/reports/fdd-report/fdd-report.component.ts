import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ReportService } from '../../_services/report.service';
import { MasterService } from '../../_services/master.service';
import { AlertService } from '../../_services/alert.service';
import { ReportPageBase } from '../report-page.base';
import { ExcelColumn, exportRowsToExcel, reportFileName, displayValue } from '../report-excel-export.util';

@Component({
  selector: 'app-fdd-report',
  templateUrl: './fdd-report.component.html',
  styleUrls: ['../reports.shared.css']
})
export class FddReportComponent extends ReportPageBase implements OnInit {
  pageTitle = 'Report';
  endpoint = '';
  defaultSort = 'CollectionDate';
  columns: Array<{ header: string; field: string; type?: string }> = [];
  exportName = 'Report';
  showPatientFilter = true;
  showCollectorFilter = false;
  showModalityFilter = false;
  collectorName = '';
  modality = '';

  constructor(
    private route: ActivatedRoute,
    reportService: ReportService,
    masterService: MasterService,
    alertService: AlertService
  ) {
    super(reportService, masterService, alertService);
  }

  ngOnInit(): void {
    const data = this.route.snapshot.data || {};
    this.pageTitle = data.title || 'Report';
    this.endpoint = data.endpoint;
    this.defaultSort = data.defaultSort || 'CollectionDate';
    this.columns = data.columns || [];
    this.exportName = data.exportName || 'Report';
    this.showPatientFilter = data.showPatientFilter !== false;
    this.showCollectorFilter = !!data.showCollectorFilter;
    this.showModalityFilter = !!data.showModalityFilter;
    this.initReportPage();
  }

  reset(): void {
    this.collectorName = '';
    this.modality = '';
    this.resetFilters();
  }

  protected runSearch(page: number, pageSize: number): void {
    this.currentPage = page;
    this.loading = true;
    const filter = this.buildFilter(page, pageSize, this.defaultSort);
    (filter as any).collectorName = this.collectorName || null;
    (filter as any).modality = this.modality || null;
    this.reportService.getFddReport(this.endpoint, filter).subscribe(
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
    const filter = this.buildFilter(1, 0, this.defaultSort);
    (filter as any).collectorName = this.collectorName || null;
    (filter as any).modality = this.modality || null;
    return this.reportService.getFddReport(this.endpoint, filter);
  }

  protected exportRows(rows: any[]): void {
    const excelCols: ExcelColumn<any>[] = this.columns.map(c => ({
      header: c.header,
      width: 100,
      type: c.type === 'date' ? 'date' : undefined,
      value: r => displayValue(r[c.field])
    }));
    exportRowsToExcel(this.pageTitle, reportFileName(this.exportName), excelCols, rows);
  }
}
