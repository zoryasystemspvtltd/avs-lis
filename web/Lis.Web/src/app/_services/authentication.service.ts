import { Injectable, EventEmitter } from '@angular/core';
import { HttpClient, HttpParams, HttpParameterCodec } from '@angular/common/http';
import { BehaviorSubject, Observable, of, forkJoin } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { AuthenticationToken, KeyValuePair } from '../_models';
import { normalizeModuleAccess, normalizeMenuAccess } from '../_guards/permission.util';
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
        let stored: AuthenticationToken = null;
        try {
            const raw = localStorage.getItem('currentUser');
            // Guard against corrupted values like the literal string "null" from older login page bugs.
            if (raw && raw !== 'null') {
                stored = JSON.parse(raw);
                if (!stored || !stored.accessToken) {
                    stored = null;
                }
            }
        } catch {
            stored = null;
            try { localStorage.removeItem('currentUser'); } catch { /* ignore */ }
        }
        this.currentUserSubject = new BehaviorSubject<AuthenticationToken>(stored);
        this.currentUser = this.currentUserSubject.asObservable();
        if (stored) {
            this.isAuthenticated = true;
            this.hideSideNav = false;
        }
    }

    /** Persist session; never throw (incognito / blocked storage must not break login). */
    private persistCurrentUser(user: AuthenticationToken): void {
        try {
            localStorage.setItem('currentUser', JSON.stringify(user));
            localStorage.setItem('applicationServae', environment.ApplicationServer);
        } catch {
            // Memory session via BehaviorSubject still works for this tab.
        }
    }

    private apiUrl(path: string): string {
        const base = (environment.ApplicationServer || '').replace(/\/+$/, '');
        const suffix = path.startsWith('/') ? path : '/' + path;
        return base + suffix;
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

        return this.http.post<any>(this.apiUrl('/Token'), formBody)
            .pipe(map(user => {
                if (user && user.access_token) {
                    user.accessToken = user.access_token;
                    user.userName = user.userName || user.username;
                    user.emailConfirmed = user.emailConfirmed === true || user.emailConfirmed === 'true'
                        ? 'true'
                        : 'false';
                    user.expires = user.expires;
                    user.issued = user.issued;
                    user.refreshToken = user.refresh_token;
                    this.persistCurrentUser(user);
                    this.isAuthenticated = true;
                    this.currentUserSubject.next(user);
                }

                return user;
            }));
    }

    relogin() {
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

        return this.http.post<any>(this.apiUrl('/Token'), formBody)
            .pipe(map(user => {
                if (user && user.access_token) {
                    user.accessToken = user.access_token;
                    user.userName = user.userName || user.username;
                    this.persistCurrentUser(user);
                    this.getRole().subscribe(() => {
                        this.getUserAccess().subscribe(() => { });
                    });
                    this.currentUserSubject.next(user);
                }

                return user;
            }));
    }

    loginExternal(user: AuthenticationToken) {
        if (user && (user as any).access_token && !user.accessToken) {
            user.accessToken = (user as any).access_token;
        }
        this.persistCurrentUser(user);
        this.currentUserSubject.next(user);
        return this.isLoggedIn();
    }

    logout() {
        const clearSession = () => {
            try { localStorage.removeItem('currentUser'); } catch { /* ignore */ }
            this.isAuthenticated = false;
            this.hideSideNav = true;
            this.currentUserSubject.next(null);
            this.userChangeEvent.emit(false);
        };

        return this.http.post<any>(this.apiUrl('/api/Account/Logout'), {})
            .pipe(
                map(() => {
                    clearSession();
                    return true;
                }),
                catchError(() => {
                    clearSession();
                    return of(true);
                })
            );
    }

    getRole() {
        // Must not require Roles module permission — used by every user at login.
        return this.http.get<any>(this.apiUrl('/api/Roles'))
            .pipe(
                map(roles => {
                    const items = (roles && Array.isArray(roles.items)) ? roles.items : [];
                    const rolenames = items.map(function (role) {
                        return role.name;
                    });

                    const user = this.currentUserValue;
                    if (user) {
                        user.roles = rolenames;
                        this.persistCurrentUser(user);
                        this.currentUserSubject.next(user);
                    }

                    return true;
                }),
                catchError(() => of(true))
            );
    }

    getUserAccess() {
        const clientId = this.selectedApplication;
        // Fail-soft: never block login on permission API/storage errors.
        return forkJoin([
            this.http.get<any>(this.apiUrl(`/api/UserAccess/${clientId}`))
                .pipe(catchError(() => of('[]'))),
            this.http.get<any>(this.apiUrl(`/api/UserAccess/${clientId}/menus`))
                .pipe(catchError(() => of('[]')))
        ]).pipe(
            map(([modules, menus]) => {
                const user = this.currentUserValue;
                if (!user) {
                    return [];
                }
                try {
                    user.access = normalizeModuleAccess(modules);
                    user.menuAccess = normalizeMenuAccess(menus);
                    this.persistCurrentUser(user);
                    this.isAuthenticated = true;
                    this.hideSideNav = false;
                    this.currentUserSubject.next(user);
                    this.userChangeEvent.emit(true);
                } catch {
                    this.isAuthenticated = true;
                    this.hideSideNav = false;
                    this.userChangeEvent.emit(true);
                }
                return user.access || [];
            }),
            catchError(() => {
                const user = this.currentUserValue;
                if (user) {
                    if (!user.access) {
                        user.access = [];
                    }
                    if (!user.menuAccess) {
                        user.menuAccess = [];
                    }
                    this.isAuthenticated = true;
                    this.hideSideNav = false;
                    this.currentUserSubject.next(user);
                    this.userChangeEvent.emit(true);
                }
                return of([]);
            })
        );
    }

    getUserMenuAccess() {
        return this.http.get<any>(this.apiUrl(`/api/UserAccess/${this.selectedApplication}/menus`))
            .pipe(
                map(menus => {
                    const user = this.currentUserValue;
                    if (!user) {
                        return [];
                    }
                    user.menuAccess = normalizeMenuAccess(menus);
                    this.persistCurrentUser(user);
                    this.currentUserSubject.next(user);
                    this.userChangeEvent.emit(true);
                    return user.menuAccess;
                }),
                catchError(() => of([]))
            );
    }

    getUserApps() {
        return this.http.get<any>(this.apiUrl('/api/UserAccess/'))
            .pipe(map(apps => apps));
    }

    getResources() {
        return this.http.get<any>(this.apiUrl('/api/Resources/'))
            .pipe(
                map(resources => {
                    try {
                        localStorage.setItem('resources', JSON.stringify(resources));
                    } catch { /* ignore */ }
                }),
                catchError(() => of(null))
            );
    }

    getResource(key: string) {
        try {
            const resources = <KeyValuePair[]>JSON.parse(localStorage.getItem('resources'));
            const resource = resources && resources.find(p => p.key === key);
            return resource ? resource.value : '';
        } catch {
            return '';
        }
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
