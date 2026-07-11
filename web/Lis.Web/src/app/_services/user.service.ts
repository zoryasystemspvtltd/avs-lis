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

    getLookup() {
        return this.http.get<Array<{ id: string; name: string }>>(
            `${environment.ApplicationServer}/api/Users/Lookup`
        );
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

    uploadDoctorSignature(userId: string, file: File) {
        const formData = new FormData();
        formData.append('file', file, file.name);
        return this.http.post<{ doctor_signature_path: string }>(
            `${environment.ApplicationServer}/api/Users/${userId}/DoctorSignature`,
            formData
        );
    }

    getDoctorSignatureBlob(userId: string) {
        return this.http.get(
            `${environment.ApplicationServer}/api/Users/${userId}/DoctorSignature`,
            { responseType: 'blob' }
        );
    }

    getRoleById(id:string) {
        const base = (environment.ApplicationServer || '').replace(/\/+$/, '');
        return this.http.get<any>(`${base}/api/Roles/${id}`)
            .pipe(map(response => {
                return response;
            }));
    }

    addRole(item: any) {
        const base = (environment.ApplicationServer || '').replace(/\/+$/, '');
        return this.http.post<any>(`${base}/api/Roles`, item)
        .pipe(map(response => {
            if (response && !response.id && response.Id) {
                response.id = response.Id;
            }
            return response;
        }));
    }

    editRole(item: any) {
        const base = (environment.ApplicationServer || '').replace(/\/+$/, '');
        const id = item && (item.id || item.Id);
        return this.http.put<any>(`${base}/api/Roles/${id}`, item)
        .pipe(map(response => {
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