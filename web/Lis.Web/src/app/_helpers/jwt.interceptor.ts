import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor } from '@angular/common/http';
import { Observable } from 'rxjs';

import { AuthenticationService } from '../_services';
import { environment } from '../../environments/environment';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
    constructor(private authenticationService: AuthenticationService) { }

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        // add authorization header with jwt token if available
        let currentUser = this.authenticationService.currentUserValue;

        if (environment.IsOldApplicationServer) {
            if (currentUser && currentUser.accessToken) {
                request = request.clone({
                    setHeaders: {
                        contentType: 'application/x-www-form-urlencoded',
                        accessKey: this.authenticationService.selectedApplication,
                        authorization: `Bearer ${currentUser.accessToken}`
                    }
                });
            } else {
                request = request.clone({
                    setHeaders: {
                        contentType: 'application/x-www-form-urlencoded',
                        accessKey: this.authenticationService.selectedApplication
                    }
                });
            }
        }
        else {
            if (currentUser && currentUser.accessToken) {
                if (request.url == 'api/FileUpload') {
                    request = request.clone({
                        setHeaders: {
                            //'Content-Type': 'application/json',
                            'accessKey': this.authenticationService.selectedApplication,
                            'Authorization': `Bearer ${currentUser.accessToken}`
                        }
                    });
                }
                else {
                    request = request.clone({
                        setHeaders: {
                            'Content-Type': 'application/json',
                            'accessKey': this.authenticationService.selectedApplication,
                            'Authorization': `Bearer ${currentUser.accessToken}`
                        }
                    });
                }
            } else {
                request = request.clone({
                    setHeaders: {
                        'Content-Type': 'application/json',
                        'accessKey': this.authenticationService.selectedApplication
                    }
                });
            }
        }
        return next.handle(request);
    }
}