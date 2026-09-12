import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Phase 3: Admin template workspace + designer APIs.
 * Does not call production Diagnostic/Radiology print endpoints.
 */
@Injectable({ providedIn: 'root' })
export class ReportTemplateConfigurationService {
  private baseUrl = environment.ApplicationServer + '/api/ReportTemplateConfiguration';

  constructor(private http: HttpClient) { }

  list(reportType?: string): Observable<any[]> {
    const q = reportType ? `?reportType=${encodeURIComponent(reportType)}` : '';
    return this.http.get<any[]>(`${this.baseUrl}${q}`);
  }

  ensureDefaults(): Observable<any[]> {
    return this.http.post<any[]>(`${this.baseUrl}/ensure-defaults`, {});
  }

  resolve(body: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/resolve`, body);
  }

  listVersions(templateId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/${templateId}/versions`);
  }

  listComponents(reportType?: string): Observable<any[]> {
    const q = reportType ? `?reportType=${encodeURIComponent(reportType)}` : '';
    return this.http.get<any[]>(`${this.baseUrl}/components${q}`);
  }

  previewSample(reportType: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/preview/sample`, { reportType, useSampleData: true });
  }

  previewVersion(versionId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/preview/version/${versionId}`, {});
  }

  previewDefinition(reportType: string, definitionJson: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/preview/definition`, {
      reportType, definitionJson, useSampleData: true
    });
  }

  getMode(reportType: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/mode?reportType=${encodeURIComponent(reportType)}`);
  }

  setMode(reportType: string, mode: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/mode`, { reportType, mode });
  }

  workspace(reportType: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/workspace?reportType=${encodeURIComponent(reportType)}`);
  }

  systemDefault(reportType: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/system-default?reportType=${encodeURIComponent(reportType)}`);
  }

  createCustom(body: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/designer/create`, body);
  }

  saveDesign(body: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/designer/save`, body);
  }

  validateDesign(reportType: string, definitionJson: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/designer/validate`, { reportType, definitionJson });
  }

  activate(templateId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/designer/${templateId}/activate`, {});
  }

  deactivate(templateId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/designer/${templateId}/deactivate`, {});
  }

  designFields(reportType?: string): Observable<any[]> {
    const q = reportType ? `?reportType=${encodeURIComponent(reportType)}` : '';
    return this.http.get<any[]>(`${this.baseUrl}/design-fields${q}`);
  }

  targets(search?: string): Observable<any[]> {
    const q = search ? `?search=${encodeURIComponent(search)}` : '';
    return this.http.get<any[]>(`${this.baseUrl}/targets${q}`);
  }
}
