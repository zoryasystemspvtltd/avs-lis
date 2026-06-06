import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { User, ResetPassword, ChangePassword, UserInfo } from '../_models';
import { environment } from '../../environments/environment';
import { AuthenticationService } from './authentication.service';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class UserService {
    constructor(private http: HttpClient, private authenticationService: AuthenticationService) { }

    getAll() {
        return this.http.get<User[]>(`${environment.ApplicationServer}/api/Users/`)
        .pipe(map(response => {
            //console.log(response);

            return response;
        }));
    }

    getById(id: string) {
        return this.http.get<User>(`${environment.ApplicationServer}/api/Users/${id}`)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    getProfile() {
        return this.http.get<User>(`${environment.ApplicationServer}/api/UserProfile`)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    register(data: UserInfo) {
        return this.http.post<any>(`${environment.ApplicationServer}/api/Users/`,
        data)
        .pipe(map(response => {
            //console.log(response);

            return response;
        }));
    }

    
    addUser(item: any) {
        return this.http.post<any>(`${environment.ApplicationServer}/api/users/`,item)
        .pipe(map(response => {
            //console.log(response);

            return response;
        }));
    }

    editUser(item: any) {
        return this.http.put<any>(`${environment.ApplicationServer}/api/users/${item.id}`,item)
        .pipe(map(response => {
            //console.log(response);

            return response;
        }));
    }

    getRoleById(id:string) {
        return this.http.get<any>(`${environment.ApplicationServer}/api/roles/${id}`)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    addRole(item: any) {
        return this.http.post<any>(`${environment.ApplicationServer}/api/roles/`,item)
        .pipe(map(response => {
            //console.log(response);

            return response;
        }));
    }

    editRole(item: any) {
        return this.http.put<any>(`${environment.ApplicationServer}/api/roles/${item.id}`,item)
        .pipe(map(response => {
            //console.log(response);

            return response;
        }));
    }
    /*
    delete(id: string) {
        return this.http.delete(`/users/` + id);
    }
    */

    changepassword(data: ChangePassword) {
        return this.http.post<any>(`${environment.ApplicationServer}/api/Account/ChangePassword`,
            data)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    forgetpassword(data: ResetPassword) {
        return this.http.post<any>(`${environment.ApplicationServer}/api/ResetPassword`,
            data)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }

    forgetpasswordStep2(data: ResetPassword) {
        return this.http.put<any>(`${environment.ApplicationServer}/api/ResetPassword`,
            data)
            .pipe(map(response => {
                //console.log(response);

                return response;
            }));
    }
    
}