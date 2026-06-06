import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface ReportFilter {
  fromDate: string;
  toDate: string;
  patientId?: number | null;
  referralDoctorId?: number | null;
  invoiceNo?: string;
  currentPage?: number;
  recordPerPage?: number;
  sortColumnName?: string;
  sortDirection?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private baseUrl = environment.ApplicationServer;

  constructor(private http: HttpClient) { }

  getSaleInvoiceRegister(filter: ReportFilter): Observable<{ items: any[]; totalRecord: number }> {
    return this.fetchReport('SaleInvoiceRegister', filter, 'InvoiceDate');
  }

  getTestBookingRegister(filter: ReportFilter): Observable<{ items: any[]; totalRecord: number }> {
    return this.fetchReport('TestBookingRegister', filter, 'BookingDate');
  }

  getTestReportLabNumbers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/api/Reports/TestReportLabNumbers`);
  }

  getTestReport(labNo: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/Reports/TestReport?labNo=${encodeURIComponent(labNo)}`);
  }

  private fetchReport(endpoint: string, filter: ReportFilter, defaultSort: string): Observable<{ items: any[]; totalRecord: number }> {
    const option = {
      FromDate: filter.fromDate,
      ToDate: filter.toDate,
      PatientId: filter.patientId || null,
      ReferralDoctorId: filter.referralDoctorId || null,
      InvoiceNo: filter.invoiceNo || null,
      CurrentPage: filter.currentPage || 1,
      RecordPerPage: filter.recordPerPage || 25,
      SortColumnName: filter.sortColumnName || defaultSort,
      SortDirection: filter.sortDirection !== undefined ? filter.sortDirection : false
    };
    const headers = new HttpHeaders({ ApiOption: JSON.stringify(option) });
    return this.http.get<any>(`${this.baseUrl}/api/Reports/${endpoint}`, { headers }).pipe(
      map(r => ({
        items: (r?.items || r?.Items || []).map(normalizeRow),
        totalRecord: r?.totalRecord ?? r?.TotalRecord ?? 0
      }))
    );
  }
}

function normalizeRow(row: any): any {
  if (!row || typeof row !== 'object') {
    return row;
  }
  const normalized: any = {};
  Object.keys(row).forEach(key => {
    const camel = key.charAt(0).toLowerCase() + key.slice(1);
    normalized[camel] = row[key];
    if (key !== camel) {
      normalized[key] = row[key];
    }
  });
  return normalized;
}
