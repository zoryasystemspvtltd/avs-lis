import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class MasterService {
  private baseUrl = environment.ApplicationServer;

  constructor(private http: HttpClient) { }

  getItems(apiName: string, option: any): Observable<any> {
    const headers = new HttpHeaders({ ApiOption: JSON.stringify(option) });
    return this.http.get<any>(`${this.baseUrl}/api/${apiName}/`, { headers });
  }

  getItem(apiName: string, id: any): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/${apiName}/${id}`);
  }

  addItem(apiName: string, item: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/${apiName}/`, item);
  }

  editItem(apiName: string, item: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/${apiName}/Put`, item);
  }

  deleteItem(apiName: string, item: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/${apiName}/Delete`, item);
  }

  getAll(apiName: string): Observable<any> {
    if (apiName === 'HisTest') {
      return this.getLookupList('HisTest');
    }
    return this.http.get<any>(`${this.baseUrl}/api/${apiName}/GetAll`);
  }

  /** Master lookup lists for dropdowns (GetAll or paginated Get). */
  getLookupList(apiName: string): Observable<any[]> {
    if (apiName === 'HisTest') {
      const option = {
        RecordPerPage: 500,
        CurrentPage: 1,
        SortColumnName: 'HISTestCode',
        SortDirection: true
      };
      return this.getItems('HisTest', option).pipe(
        map(response => {
          if (Array.isArray(response)) {
            return response;
          }
          if (response && (response.items || response.Items)) {
            return response.items || response.Items;
          }
          return [];
        }),
        catchError(() => of([]))
      );
    }

    return this.getAll(apiName).pipe(
      map(response => Array.isArray(response) ? response : [])
    );
  }

  getEffectiveRate(testId: number, rateType: number, corporateId?: number, referralDoctorId?: number, profileId?: number): Observable<any> {
    let url = `${this.baseUrl}/api/TestRate/GetEffective?testId=${testId}&rateType=${rateType}`;
    if (corporateId) { url += `&corporateId=${corporateId}`; }
    if (referralDoctorId) { url += `&referralDoctorId=${referralDoctorId}`; }
    if (profileId) { url += `&profileId=${profileId}`; }
    return this.http.get<any>(url);
  }

  getInvoice(id: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/SaleInvoice/${id}`);
  }

  saveInvoice(dto: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/SaleInvoice/`, dto);
  }

  getNextInvoiceNo(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/SaleInvoice/NextInvoiceNo`);
  }

  getBillingPatients(option: any): Observable<any> {
    const headers = new HttpHeaders({ ApiOption: JSON.stringify(option) });
    return this.http.get<any>(`${this.baseUrl}/api/Patients/Billing`, { headers });
  }

  updateInvoiceStatus(id: number, invoiceStatus: number, paymentStatus: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/SaleInvoice/Status`, { id, invoiceStatus, paymentStatus });
  }

  cancelInvoice(id: number): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/api/SaleInvoice/Cancel/${id}`, null);
  }
}
