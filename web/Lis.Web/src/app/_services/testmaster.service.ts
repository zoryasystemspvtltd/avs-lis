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

  private hisTestListOption() {
    return {
      RecordPerPage: 500,
      CurrentPage: 1,
      SortColumnName: 'HISTestCode',
      SortDirection: true
    };
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
    return this.http.get<any>(`${environment.ApplicationServer}/api/Specimens/`)
      .pipe(
        map(response => this.normalizeList(response)),
        catchError(() => of([]))
      );
  }

  getDepartments() {
    return this.http.get<any>(`${environment.ApplicationServer}/api/Department/`)
      .pipe(
        map(response => this.normalizeList(response)),
        catchError(() => of([]))
      );
  }

  getNextTestCode(): Observable<string> {
    return this.http.get<any>(`${environment.ApplicationServer}/api/HisTest/NextTestCode`)
      .pipe(
        map(r => r?.testCode || r?.TestCode || ''),
        catchError(() => of(''))
      );
  }
}
