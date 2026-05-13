import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

import { User, ResetPassword, ChangePassword, UserInfo } from '../_models';
import { environment } from '../../environments/environment';
import { AuthenticationService } from './authentication.service';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class NewsService {
    constructor(private http: HttpClient, private authenticationService: AuthenticationService) { }

    getAll(option:any) {
       

        const requestOptions = {                                                                                                                                                                                 
            headers: new HttpHeaders({ 'ApiOption':JSON.stringify(option)}), 
          };

        return this.http.get<any>(`${environment.ApplicationServer}/api/News/`,requestOptions)
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

        return this.http.get<any>(`${environment.ApplicationServer}/api/News/`,requestOptions)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    getOthers(id: string) {
        if (id == null) {
            id = '0';
        }

        var option = {
            'RecordPerPage': 5,
            'CurrentPage': 1,
            'SortColumnName': 'Name',
            'SortDirection': true,
            'SearchCondition': {
                'Name': 'Id',
                'Value': id,
                'IsNotEqual': true
            }
        };

        const requestOptions = {                                                                                                                                                                                 
            headers: new HttpHeaders({ 'ApiOption':JSON.stringify(option)}), 
          };

        return this.http.get<any>(`${environment.ApplicationServer}/api/News/`,requestOptions)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    };
}