import { ReportFilter, ReportService } from '../_services/report.service';
import { MasterService } from '../_services/master.service';
import { AlertService } from '../_services/alert.service';

/** Shared filter, pagination, and lookup logic for operational reports. */
export abstract class ReportPageBase {
  fromDate = '';
  toDate = '';
  patientId: number = null;
  referralDoctorId: number = null;
  invoiceNo = '';
  filterError = '';

  patients: Array<{ id: number; name: string; hisPatientId: string }> = [];
  doctors: Array<{ id: number; name: string; code?: string }> = [];
  rows: any[] = [];
  loading = false;
  exporting = false;
  searched = false;

  currentPage = 1;
  recordPerPage = 25;
  totalRecord = 0;

  protected constructor(
    protected reportService: ReportService,
    protected masterService: MasterService,
    protected alertService: AlertService
  ) { }

  initReportPage(): void {
    const today = new Date();
    const first = new Date(today.getFullYear(), today.getMonth(), 1);
    this.toDate = this.formatDate(today);
    this.fromDate = this.formatDate(first);
    this.loadLookups();
  }

  loadLookups(): void {
    this.masterService.getBillingPatients({
      RecordPerPage: 500,
      CurrentPage: 1,
      SearchText: '',
      SortColumnName: 'Name',
      SortDirection: false,
      Status: 0
    }).subscribe(p => {
      const items = p?.items || p?.Items || [];
      this.patients = items
        .map((x: any) => ({
          id: x.id ?? x.Id,
          name: (x.name ?? x.Name ?? '').trim(),
          hisPatientId: x.hisPatientId ?? x.HisPatientId ?? ''
        }))
        .filter((x: any) => x.id > 0 && x.name);
    });

    this.masterService.getLookupList('ReferralDoctor').subscribe(d => {
      const list = Array.isArray(d) ? d : [];
      this.doctors = list
        .filter((x: any) => x.isActive !== false && x.IsActive !== false)
        .map((x: any) => ({
          id: x.id ?? x.Id,
          name: (x.name ?? x.Name ?? '').trim(),
          code: x.code ?? x.Code
        }))
        .filter((x: any) => x.id > 0 && x.name);
    });
  }

  validateFilters(): boolean {
    this.filterError = '';
    if (!this.fromDate || !this.toDate) {
      this.filterError = 'From Date and To Date are required.';
      return false;
    }
    if (this.fromDate > this.toDate) {
      this.filterError = 'From Date cannot be after To Date.';
      return false;
    }
    return true;
  }

  buildFilter(page: number, recordPerPage: number, sortColumn: string): ReportFilter {
    return {
      fromDate: this.fromDate,
      toDate: this.toDate,
      patientId: this.patientId || null,
      referralDoctorId: this.referralDoctorId || null,
      invoiceNo: this.invoiceNo?.trim() || null,
      currentPage: page,
      recordPerPage,
      sortColumnName: sortColumn,
      sortDirection: false
    };
  }

  resetFilters(): void {
    const today = new Date();
    const first = new Date(today.getFullYear(), today.getMonth(), 1);
    this.fromDate = this.formatDate(first);
    this.toDate = this.formatDate(today);
    this.patientId = null;
    this.referralDoctorId = null;
    this.invoiceNo = '';
    this.filterError = '';
    this.rows = [];
    this.totalRecord = 0;
    this.searched = false;
    this.currentPage = 1;
  }

  onPageChange(page: number): void {
    if (!this.searched) {
      return;
    }
    this.runSearch(page, this.recordPerPage);
  }

  onPageSizeChange(size: string | number): void {
    this.recordPerPage = +size;
    if (this.searched) {
      this.runSearch(1, this.recordPerPage);
    }
  }

  get recordFrom(): number {
    if (this.totalRecord === 0) {
      return 0;
    }
    return (this.currentPage - 1) * this.recordPerPage + 1;
  }

  get recordTo(): number {
    return Math.min(this.currentPage * this.recordPerPage, this.totalRecord);
  }

  get canExport(): boolean {
    return this.searched && this.totalRecord > 0 && !this.loading && !this.exporting;
  }

  protected formatDate(d: Date): string {
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${d.getFullYear()}-${m}-${day}`;
  }

  protected abstract runSearch(page: number, pageSize: number): void;
  protected abstract fetchAllForExport(): import('rxjs').Observable<{ items: any[]; totalRecord: number }>;
  protected abstract exportRows(rows: any[]): void;

  exportExcel(): void {
    if (!this.validateFilters()) {
      return;
    }
    if (!this.searched || this.totalRecord === 0) {
      this.alertService.error('No data to export. Run Search first.');
      return;
    }

    this.exporting = true;
    this.fetchAllForExport().subscribe(
      r => {
        const items = r?.items || [];
        if (!items.length) {
          this.alertService.error('No records found for export.');
        } else {
          this.exportRows(items);
          this.alertService.success(`Exported ${items.length} record(s).`);
        }
        this.exporting = false;
      },
      () => {
        this.exporting = false;
        this.alertService.error('Export failed. Please try again.');
      }
    );
  }
}
