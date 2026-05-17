import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RateMasterService {
  constructor(private http: HttpClient) { }

  getAll() {
    return this.http.get<any[]>(`${environment.ApplicationServer}/api/TestRate`)
      .pipe(map(response => response));
  }

  getById(id: string) {
    return this.http.get<any>(`${environment.ApplicationServer}/api/TestRate/${id}`)
      .pipe(map(response => response));
  }

  create(data: any) {
    return this.http.post<any>(`${environment.ApplicationServer}/api/TestRate`, data)
      .pipe(map(response => response));
  }

  update(data: any) {
    return this.http.put<any>(`${environment.ApplicationServer}/api/TestRate/${data.id}`, data)
      .pipe(map(response => response));
  }

  delete(id: string) {
    return this.http.delete<any>(`${environment.ApplicationServer}/api/TestRate/${id}`)
      .pipe(map(response => response));
  }

  getTests() {
    return this.http.get<any[]>(`${environment.ApplicationServer}/api/hisParameter/`)
      .pipe(map(response => response));
  }
}
