import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TestResultEditSearchOptions {
  sampleNo?: string;
  invoiceNo?: string;
  patientName?: string;
  fromDate?: string;
  toDate?: string;
}

@Injectable({ providedIn: 'root' })
export class TestResultEditService {
  private baseUrl = `${environment.ApplicationServer}/api/TestResultEdit`;

  constructor(private http: HttpClient) { }

  search(options: TestResultEditSearchOptions): Observable<any[]> {
    const headers = new HttpHeaders({ ApiOption: JSON.stringify(options || {}) });
    return this.http.get<any[]>(`${this.baseUrl}/search`, { headers });
  }

  getBySampleNo(sampleNo: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/${encodeURIComponent(sampleNo)}`);
  }

  save(request: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}`, request);
  }
}
