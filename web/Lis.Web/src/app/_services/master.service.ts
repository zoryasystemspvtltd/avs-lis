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
    if (apiName === 'EquipmentHeartbeat') {
      return this.http.get<any>(`${this.baseUrl}/api/EquipmentHeartbeat`).pipe(
        catchError(() => of([]))
      );
    }
    const headers = new HttpHeaders({ ApiOption: JSON.stringify(option) });
    return this.http.get<any>(`${this.baseUrl}/api/${apiName}/`, { headers });
  }

  getItem(apiName: string, id: any): Observable<any> {
    if (apiName === 'Specimens') {
      return this.http.get<any>(`${this.baseUrl}/api/Specimens/${id}`).pipe(
        catchError(() => of(null))
      );
    }
    return this.http.get<any>(`${this.baseUrl}/api/${apiName}/${id}`).pipe(
      catchError(() => of(null))
    );
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

  getEquipments(): Observable<any[]> {
    return this.http.get<any>(`${this.baseUrl}/api/Equipments`).pipe(
      map(response => {
        if (Array.isArray(response)) {
          return response;
        }
        return response?.items || response?.Items || [];
      }),
      catchError(() => of([]))
    );
  }

  getNextPatientId(): Observable<string> {
    return this.http.get<any>(`${this.baseUrl}/api/PatientMaster/NextPatientId`).pipe(
      map(r => (typeof r === 'string' ? r : r?.patientId || r?.PatientId || '')),
      catchError(() => of(''))
    );
  }

  getNextRequestNo(): Observable<string> {
    return this.http.get<any>(`${this.baseUrl}/api/NewSample/NextRequestNo`).pipe(
      map(r => (typeof r === 'string' ? r : r?.requestNo || r?.RequestNo || '')),
      catchError(() => of(''))
    );
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

  getEffectiveRate(testId: number, rateType: number, corporateId?: number, referralDoctorId?: number, profileId?: number, invoiceDate?: string): Observable<any> {
    let url = `${this.baseUrl}/api/TestRate/GetEffective?testId=${testId}&rateType=${rateType}`;
    if (corporateId) { url += `&corporateId=${corporateId}`; }
    if (referralDoctorId) { url += `&referralDoctorId=${referralDoctorId}`; }
    if (profileId) { url += `&profileId=${profileId}`; }
    if (invoiceDate) { url += `&invoiceDate=${invoiceDate}`; }
    return this.http.get<any>(url);
  }

  /** Invoice rate: Corporate → Doctor → Profile → Standard; uses invoice date for effective range. */
  getEffectiveRateForInvoice(testId: number, invoiceDate: string, corporateId?: number, referralDoctorId?: number, profileId?: number, emergency = false): Observable<any> {
    let url = `${this.baseUrl}/api/TestRate/GetEffectiveForInvoice?testId=${testId}&invoiceDate=${invoiceDate}`;
    if (corporateId) { url += `&corporateId=${corporateId}`; }
    if (referralDoctorId) { url += `&referralDoctorId=${referralDoctorId}`; }
    if (profileId) { url += `&profileId=${profileId}`; }
    if (emergency) { url += `&emergency=true`; }
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
