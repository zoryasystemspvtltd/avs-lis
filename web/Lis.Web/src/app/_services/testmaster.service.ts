import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { environment } from '../../environments/environment';
import { AuthenticationService } from './authentication.service';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class TestMasterService {
     constructor(private http: HttpClient, private authenticationService: AuthenticationService) { }

    getAll() {
        return this.http.get<any[]>(`${environment.ApplicationServer}/api/hisParameter/`)
            .pipe(map(response => {
                return response;
            }));
    }

    getById(id: string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/histest/${id}`)
            .pipe(map(response => {
                return response;
            }));
    }

    create(data: any) {
        debugger;
        return this.http.post<any>(`${environment.ApplicationServer}/api/histest/`, data)
            .pipe(map(response => {
                return response;
            }));
    }

    update(data: any) {
        return this.http.put<any>(`${environment.ApplicationServer}/api/histest/`, data)
            .pipe(map(response => {
                return response;
            }));
    }

    delete(id: string) {
        return this.http.delete<any>(`${environment.ApplicationServer}/api/histest/${id}`)
            .pipe(map(response => {
                return response;
            }));
    }

    getSpecimens() {
        return this.http.get<any[]>(`${environment.ApplicationServer}/api/specimens/`)
            .pipe(map(response => {
                return response;
            }));
    }

    getDepartments() {
        return this.http.get<any[]>(`${environment.ApplicationServer}/api/department/`)
            .pipe(map(response => {
                return response;
            }));
    }
}
