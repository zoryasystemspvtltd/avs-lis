import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

import { User, ResetPassword, ChangePassword, UserInfo } from '../_models';
import { environment } from '../../environments/environment';
import { AuthenticationService } from './authentication.service';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class EventService {
    constructor(private http: HttpClient, private authenticationService: AuthenticationService) { }

    getAll() {
        let option = {
            'RecordPerPage': 1,
            'CurrentPage': 1,
            'SortColumnName': 'Name',
            'SortDirection': true
        };

        const requestOptions = {                                                                                                                                                                                 
            headers: new HttpHeaders({ 'ApiOption':JSON.stringify(option)}), 
          };

        return this.http.get<any>(`${environment.ApplicationServer}/api/Event/`,requestOptions)
        .pipe(map(response => {
            //console.log(response);

            return response;
        }));

       
    }

    getById(id: string) {
        let option = {
            'RecordPerPage': 1,
            'CurrentPage': 1,
            'SortColumnName': 'Name',
            'SortDirection': true,
            'SearchCondition': {
                'Name': 'Id',
                'Value': id
            }
        };

        const requestOptions = {                                                                                                                                                                                 
            headers: new HttpHeaders({ 'ApiOption':JSON.stringify(option)}), 
          };

        return this.http.get<any>(`${environment.ApplicationServer}/api/Event/`,requestOptions)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    getUpcoming() {
        let option = {
            'RecordPerPage': 4,
            'CurrentPage': 1,
            'SortColumnName': 'EventStartDate',
            'SortDirection': true
        };
 
        const requestOptions = {                                                                                                                                                                                 
            headers: new HttpHeaders({ 'ApiOption':JSON.stringify(option)}), 
          };

        return this.http.get<any>(`${environment.ApplicationServer}/api/Event/`,requestOptions)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }
}