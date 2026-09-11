import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ReportLayoutConfigurationDto {
  id?: number;
  reportType: string;
  pageSize: string;
  orientation: string;
  headerHeightMm: number;
  footerHeightMm: number;
  leftMarginMm: number;
  rightMarginMm: number;
  doctorSignatureEnabled: boolean;
  doctorSignatureHorizontal: string;
  doctorSignatureVertical: string;
  doctorSignatureWidthMm: number;
  doctorSignatureHeightMm: number;
  technicianSignatureEnabled: boolean;
  technicianSignatureHorizontal: string;
  technicianSignatureVertical: string;
  technicianSignatureWidthMm: number;
  technicianSignatureHeightMm: number;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ReportLayoutConfigurationService {
  private readonly baseUrl = `${environment.ApplicationServer}/api/ReportLayoutConfiguration`;

  constructor(private http: HttpClient) { }

  getByReportType(reportType: string): Observable<ReportLayoutConfigurationDto> {
    return this.http.get<ReportLayoutConfigurationDto>(`${this.baseUrl}/${encodeURIComponent(reportType)}`);
  }

  getDefaults(reportType: string): Observable<ReportLayoutConfigurationDto> {
    return this.http.get<ReportLayoutConfigurationDto>(`${this.baseUrl}/defaults/${encodeURIComponent(reportType)}`);
  }

  save(dto: ReportLayoutConfigurationDto): Observable<any> {
    return this.http.post(this.baseUrl, dto);
  }

  reset(reportType: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/reset/${encodeURIComponent(reportType)}`, {});
  }
}
