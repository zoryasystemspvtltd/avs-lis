import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface WorkflowSearchOptions {
  currentPage?: number;
  recordPerPage?: number;
  sortColumnName?: string;
  sortDirection?: boolean;
  searchText?: string;
  patientId?: number;
  uhid?: string;
  barcodeNumber?: string;
  orderNumber?: string;
  patientName?: string;
  collectionDate?: string;
  modality?: string;
}

@Injectable({ providedIn: 'root' })
export class SampleWorkflowService {
  private baseUrl = environment.ApplicationServer;

  constructor(private http: HttpClient) { }

  getCollectionQueue(options: WorkflowSearchOptions): Observable<{ items: any[]; totalRecord: number }> {
    return this.fetchList('SampleCollection/PendingQueue', options);
  }

  getReceivingQueue(options: WorkflowSearchOptions): Observable<{ items: any[]; totalRecord: number }> {
    return this.fetchList('SampleReceiving/Queue', options);
  }

  getByBarcode(module: 'collection' | 'receiving', barcode: string): Observable<any> {
    const prefix = module === 'collection' ? 'SampleCollection' : 'SampleReceiving';
    return this.http.get<any>(`${this.baseUrl}/api/${prefix}/ByBarcode?barcode=${encodeURIComponent(barcode)}`);
  }

  collectSample(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/SampleCollection/Collect`, payload);
  }

  rejectCollection(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/SampleCollection/Reject`, payload);
  }

  recollectCollection(id: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/SampleCollection/Recollect/${id}`, {});
  }

  ensureBarcode(id: number): Observable<{ barcode: string }> {
    return this.http.get<{ barcode: string }>(`${this.baseUrl}/api/SampleCollection/EnsureBarcode/${id}`);
  }

  receiveSample(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/SampleReceiving/Receive`, payload);
  }

  rejectReceiving(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/SampleReceiving/Reject`, payload);
  }

  recollectReceiving(id: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/SampleReceiving/Recollect/${id}`, {});
  }

  getRejectionReasons(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/api/SampleReceiving/RejectionReasons`);
  }

  getRadiologyQueue(options: WorkflowSearchOptions): Observable<{ items: any[]; totalRecord: number }> {
    return this.fetchList('RadiologyReport/PendingQueue', options);
  }

  getRadiologyReport(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/RadiologyReport/${id}`);
  }

  saveRadiologyReport(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/RadiologyReport/Save`, payload);
  }

  authorizeRadiologyReport(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/RadiologyReport/Authorize`, payload);
  }

  createRadiologyRequest(payload: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/RadiologyReport`, payload);
  }

  private fetchList(endpoint: string, options: WorkflowSearchOptions): Observable<{ items: any[]; totalRecord: number }> {
    const option = {
      CurrentPage: options.currentPage || 1,
      RecordPerPage: options.recordPerPage || 25,
      SortColumnName: options.sortColumnName || '',
      SortDirection: options.sortDirection !== undefined ? options.sortDirection : false,
      SearchText: options.searchText || '',
      PatientId: options.patientId || null,
      Uhid: options.uhid || null,
      BarcodeNumber: options.barcodeNumber || null,
      OrderNumber: options.orderNumber || null,
      PatientName: options.patientName || null,
      CollectionDate: options.collectionDate || null,
      Modality: options.modality || null
    };
    const headers = new HttpHeaders({ ApiOption: JSON.stringify(option) });
    return this.http.get<any>(`${this.baseUrl}/api/${endpoint}`, { headers }).pipe(
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
  });
  return normalized;
}
