import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { environment } from '../../environments/environment';
import { DashboardAlert, DashboardMetric, DashboardQueue } from './dashboard.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private http: HttpClient) { }

  getMetrics(endpoint: string): Observable<DashboardMetric[]> {
    return this.http.get<DashboardMetric[]>(this.url(endpoint))
      .pipe(map(response => response || []));
  }

  getAlerts(endpoint: string): Observable<DashboardAlert[]> {
    return this.http.get<DashboardAlert[]>(this.url(endpoint))
      .pipe(map(response => response || []));
  }

  /** Reads the head of a pending-work queue plus its full pending count. */
  getQueue(endpoint: string, take: number): Observable<DashboardQueue> {
    return this.http.get<DashboardQueue>(`${this.url(endpoint)}?take=${take}`)
      .pipe(map(response => ({
        totalRecord: response?.totalRecord || 0,
        items: response?.items || []
      })));
  }

  private url(endpoint: string): string {
    const server = (environment.ApplicationServer || '').replace(/\/+$/, '');
    const path = (endpoint || '').replace(/^\/+/, '');
    return `${server}/${path}`;
  }
}
