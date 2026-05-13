import { Injectable, EventEmitter } from '@angular/core';
import { HttpClient, HttpParams, HttpHeaders, HttpParameterCodec } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthenticationToken, UserInfo, ChangePassword, ResetPassword, KeyValuePair } from '../_models';
import { environment } from '../../environments/environment';


export class HttpFormEncodingCodec implements HttpParameterCodec {
    encodeKey(k: string): string { return encodeURIComponent(k).replace(/%20/g, '+'); }

    encodeValue(v: string): string { return encodeURIComponent(v).replace(/%20/g, '+'); }

    decodeKey(k: string): string { return decodeURIComponent(k.replace(/\+/g, ' ')); }

    decodeValue(v: string) { return decodeURIComponent(v.replace(/\+/g, ' ')); }
}

@Injectable({ providedIn: 'root' })
export class AuthenticationService {
    private currentUserSubject: BehaviorSubject<AuthenticationToken>;
    public currentUser: Observable<AuthenticationToken>;

    public selectedApplication: string = environment.ClientId;
    constructor(private http: HttpClient) {
        this.currentUserSubject = new BehaviorSubject<AuthenticationToken>(JSON.parse(localStorage.getItem('currentUser')));
        this.currentUser = this.currentUserSubject.asObservable();
    }

    isExpired(): Observable<boolean> {
        if (this.currentUserValue) {
            if (this.currentUserValue.expires) {
                const expire = new Date(this.currentUserValue.expires);
                const now = new Date();
                return of(expire < now);
            }

            return of(false);
        }

        return of(false);
    }

    isAuthenticated: boolean = false;
    isLoggedIn(): Observable<boolean> {
        //console.log(this.currentUserValue);

        if (this.currentUserValue && this.currentUserValue.accessToken) {
            this.isAuthenticated = true;
            this.userChangeEvent.emit(true);
            return of(true);
        }
        this.userChangeEvent.emit(false);
        return of(false);
    }

    public get currentUserValue(): AuthenticationToken {
        return this.currentUserSubject.value;
    }

    login(username: string, password: string) {
        let formBody: any;
        if (environment.IsOldApplicationServer) {
            formBody = new HttpParams({ encoder: new HttpFormEncodingCodec() })
                .append('grant_type', 'password')
                .append('username', username)
                .append('password', password)
                .toString();
        }
        else {
            formBody = {
                "userName": username,
                "password": password,
                "grant_type": "password"
            };
        }


        return this.http.post<any>(`${environment.ApplicationServer}/TOKEN`,
            formBody)
            .pipe(map(user => {
                // login successful if there's a jwt token in the response
                //console.log(user);
                if (user && user.access_token) {
                    // store user details and jwt token in local storage to keep user logged in between page refreshes
                    user.accessToken = user.access_token;
                    user.expires = user.expires;
                    user.issued = user.issued;
                    user.refreshToken = user.refresh_token;
                    localStorage.setItem('currentUser', JSON.stringify(user));
                    localStorage.setItem('applicationServae', environment.ApplicationServer);
                    this.currentUserSubject.next(user);
                }

                return user;
            }));
    }

    relogin() {
        //console.log('Relogin requested');

        let formBody: any;
        if (environment.IsOldApplicationServer) {
            formBody = new HttpParams({ encoder: new HttpFormEncodingCodec() })
                .append('grant_type', 'refresh_token')
                .append('refresh_token', this.currentUserValue.refreshToken)
                .toString();
        }
        else {
            formBody = {
                "refresh_token": this.currentUserValue.refreshToken,
                "grant_type": "refresh_token"
            };
        }

        return this.http.post<any>(`${environment.ApplicationServer}/TOKEN`,
            formBody)
            .pipe(map(user => {
                // login successful if there's a jwt token in the response
                //console.log(user);
                if (user && user.access_token) {
                    // store user details and jwt token in local storage to keep user logged in between page refreshes
                    localStorage.setItem('currentUser', JSON.stringify(user));
                    localStorage.setItem('applicationServae', environment.ApplicationServer);


                    this.getRole().subscribe(role => {
                        //console.log(this.currentUserValue);
                        this.getUserAccess().subscribe(acess => {

                        })
                    })

                    this.currentUserSubject.next(user);
                }

                return user;
            }));
    }

    loginExternal(user: AuthenticationToken) {
        //console.log(user);
        localStorage.setItem('currentUser', JSON.stringify(user));
        localStorage.setItem('applicationServae', environment.ApplicationServer);
        this.currentUserSubject.next(user);
        return this.isLoggedIn();
    }

    logout() {
        return this.http.post<any>(`${environment.ApplicationServer}/api/Account/Logout`, {})
            .pipe(map(roles => {
                localStorage.removeItem('currentUser');
                this.currentUserSubject = new BehaviorSubject<AuthenticationToken>(JSON.parse(localStorage.getItem('currentUser')));
                this.currentUser = this.currentUserSubject.asObservable();
                this.isAuthenticated = false;
                this.hideSideNav = true;
                this.currentUserSubject.next(null);
                return true;
            },
                error => {
                    localStorage.removeItem('currentUser');
                    this.currentUserSubject = new BehaviorSubject<AuthenticationToken>(JSON.parse(localStorage.getItem('currentUser')));
                    this.currentUser = this.currentUserSubject.asObservable();
                    this.isAuthenticated = false;
                    this.hideSideNav = true;
                    this.currentUserSubject.next(null);
                    return true;
                }));
        // remove user from local storage to log user out
    }

    getRole() {

        return this.http.get<any>(`${environment.ApplicationServer}/api/Roles/`)
            .pipe(map(roles => {
                //console.log(roles);
                const rolenames = roles.items.map(function (role) {
                    return role.name;
                });

                const user = this.currentUserValue;
                user.roles = rolenames;
                localStorage.setItem('currentUser', JSON.stringify(user));
                this.currentUserSubject.next(user);

                return true;
            }));
    }

    getUserAccess() {
        return this.http.get<any>(`${environment.ApplicationServer}/api/UserAccess/${this.selectedApplication}`)
            .pipe(map(access => {
                //console.log(access);
                const user = this.currentUserValue;
                if (environment.IsOldApplicationServer) {
                    user.access = JSON.parse(access);
                }
                else {
                    user.access = access;
                }

                localStorage.setItem('currentUser', JSON.stringify(user));
                this.currentUserSubject.next(user);

                return user.access;
            }));

    }

    getUserApps() {
        return this.http.get<any>(`${environment.ApplicationServer}/api/UserAccess/`)
            .pipe(map(apps => {
                //console.log(access);
                return apps;
            }));

    }

    getResources() {
        return this.http.get<any>(`${environment.ApplicationServer}/api/Resources/`)
            .pipe(map(resources => {
                //console.log(resources);
                localStorage.setItem('resources', JSON.stringify(resources));
            }));
    }

    getResource(key: string) {
        let resources = <KeyValuePair[]>JSON.parse(localStorage.getItem('resources'));
        const resource = resources.find(p => p.key === key)
        return resource ? resource.value : '';
    }

    hideSideNav: boolean = true;
    toggleSideNav(): void {
        this.hideSideNav = !this.hideSideNav;
    }

    userChangeEvent: EventEmitter<boolean> = new EventEmitter();

    isUserChanged() {
        return this.userChangeEvent;
    }

    selectedStatus: number = -1;
    RecordPerPage: number = 10;
    CurrentPage: number = 1;
    SortColumnName: string = 'Name';
    SortDirection: boolean = false;
}