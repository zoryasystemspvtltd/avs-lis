import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { AuthenticationService } from '../_services';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
    private logoutInProgress = false;

    constructor(private authenticationService: AuthenticationService) {}

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        
        return next.handle(request).pipe(catchError(err => {
            const body = err?.error;
            const privilegeMessage = typeof body === 'string' && /insufficient privilege/i.test(body);
            const url = (request.url || '').toLowerCase();
            const isAuthEndpoint = url.indexOf('/token') >= 0
                || url.indexOf('/account/logout') >= 0
                || url.indexOf('/account/externallogin') >= 0;

            if (err.status === 401 && !privilegeMessage && !isAuthEndpoint && !this.logoutInProgress) {
                // auto logout if 401 response returned from api (not permission denial)
                this.logoutInProgress = true;
                this.authenticationService.logout()
                .subscribe(() => {
                    this.logoutInProgress = false;
                    location.reload(true);
                }, () => {
                    this.logoutInProgress = false;
                    location.reload(true);
                });
            }
            
            const error = err.error || err.message;
            return throwError(error);
        }))
    }
}