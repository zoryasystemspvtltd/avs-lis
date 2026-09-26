import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CommissionPayoutService {
  private baseUrl = environment.ApplicationServer;

  constructor(private http: HttpClient) { }

  schedule(recipientType: number, recipientId: number): Observable<any> {
    return this.http.get<any>(
      `${this.baseUrl}/api/CommissionPayout/schedule?recipientType=${recipientType}&recipientId=${recipientId}`);
  }

  preview(body: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/CommissionPayout/preview`, body);
  }

  confirm(body: any): Observable<number> {
    return this.http.post<number>(`${this.baseUrl}/api/CommissionPayout/confirm`, body);
  }

  list(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/api/CommissionPayout`);
  }

  memo(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/CommissionPayout/${id}`);
  }
}
