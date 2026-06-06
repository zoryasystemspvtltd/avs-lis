import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { map, catchError } from 'rxjs/operators';
import { Observable, of } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TestMasterService {
  constructor(private http: HttpClient) { }

  private normalizeList(response: any): any[] {
    if (response == null) {
      return [];
    }
    if (Array.isArray(response)) {
      return response;
    }
    return response.items || response.Items || [];
  }

  private lookupListOption(sortColumn: string) {
    return {
      RecordPerPage: 500,
      CurrentPage: 1,
      SortColumnName: sortColumn,
      SortDirection: true
    };
  }

  private hisTestListOption() {
    return this.lookupListOption('HISTestCode');
  }

  private isActiveRecord(item: any): boolean {
    return item && item.isActive !== false && item.IsActive !== false;
  }

  getAll() {
    const headers = new HttpHeaders({ ApiOption: JSON.stringify(this.hisTestListOption()) });
    return this.http.get<any>(`${environment.ApplicationServer}/api/HisTest/`, { headers })
      .pipe(
        map(response => this.normalizeList(response)),
        catchError(() => of([]))
      );
  }

  getById(id: string) {
    return this.http.get<any>(`${environment.ApplicationServer}/api/HisTest/${id}`)
      .pipe(catchError(() => of(null)));
  }

  create(data: any) {
    return this.http.post<any>(`${environment.ApplicationServer}/api/HisTest/`, data);
  }

  update(data: any) {
    return this.http.post<any>(`${environment.ApplicationServer}/api/HisTest/Put`, data);
  }

  delete(id: string) {
    return this.http.post<any>(`${environment.ApplicationServer}/api/HisTest/Delete`, { id: +id });
  }

  getSpecimens() {
    const headers = new HttpHeaders({ ApiOption: JSON.stringify(this.lookupListOption('Name')) });
    return this.http.get<any>(`${environment.ApplicationServer}/api/Specimens/`, { headers })
      .pipe(
        map(response => this.normalizeList(response).filter(s => this.isActiveRecord(s))),
        catchError(() => of([]))
      );
  }

  getDepartments() {
    const headers = new HttpHeaders({ ApiOption: JSON.stringify(this.lookupListOption('Name')) });
    return this.http.get<any>(`${environment.ApplicationServer}/api/Department/`, { headers })
      .pipe(
        map(response => this.normalizeList(response).filter(d => this.isActiveRecord(d))),
        catchError(() => of([]))
      );
  }

  ensureSpecimenInList(specimens: any[], code: string, name?: string): any[] {
    if (!code) {
      return specimens || [];
    }
    const list = specimens || [];
    const normalized = (code || '').trim();
    if (list.some(s => (s.code || s.Code || '').toString().trim() === normalized)) {
      return list;
    }
    return [...list, { code: normalized, name: name || normalized, isActive: true }];
  }

  getNextTestCode(): Observable<string> {
    return this.http.get<any>(`${environment.ApplicationServer}/api/HisTest/NextTestCode`)
      .pipe(
        map(r => r?.testCode || r?.TestCode || ''),
        catchError(() => of(''))
      );
  }
}
