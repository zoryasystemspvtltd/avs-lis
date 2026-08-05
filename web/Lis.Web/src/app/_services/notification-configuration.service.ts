import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class NotificationConfigurationService {
  private readonly baseUrl = `${environment.ApplicationServer}/api/NotificationConfiguration`;

  constructor(private http: HttpClient) { }

  getConfiguration() {
    return this.http.get<any>(this.baseUrl);
  }

  saveConfiguration(config: any) {
    return this.http.post<any>(this.baseUrl, config);
  }

  getTemplates() {
    return this.http.get<any[]>(`${this.baseUrl}/Templates`);
  }

  getAudit(take = 100) {
    return this.http.get<any[]>(`${this.baseUrl}/Audit?take=${take}`);
  }
}
