import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { environment } from '../../environments/environment';
import { AuthenticationService } from './authentication.service';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class TestMasterService {
     constructor(private http: HttpClient, private authenticationService: AuthenticationService) { }

    getAll() {
        return this.http.get<any[]>(`${environment.ApplicationServer}/api/HisTest/`)
            .pipe(map(response => {
                return response;
            }));
    }

    getById(id: string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/HisTest/${id}`)
            .pipe(map(response => {
                return response;
            }));
    }

    create(data: any) {
        return this.http.post<any>(`${environment.ApplicationServer}/api/HisTest/`, data)
            .pipe(map(response => {
                return response;
            }));
    }

    update(id: string, data: any) {
        return this.http.put<any>(`${environment.ApplicationServer}/api/HisTest/${id}`, data)
            .pipe(map(response => {
                return response;
            }));
    }

    delete(id: string) {
        return this.http.delete<any>(`${environment.ApplicationServer}/api/HisTest/${id}`)
            .pipe(map(response => {
                return response;
            }));
    }

    getSpecimens() {
        return this.http.get<any[]>(`${environment.ApplicationServer}/api/Specimen/`)
            .pipe(map(response => {
                return response;
            }));
    }

    getDepartments() {
        return this.http.get<any[]>(`${environment.ApplicationServer}/api/Department/`)
            .pipe(map(response => {
                return response;
            }));
    }
}
