import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { first } from 'rxjs/operators';

import { AlertService, AuthenticationService } from '../../_services';
import { AuthenticationToken } from '../../_models/user';
import { environment } from '../../../environments/environment';

@Component({
    selector: 'login',
    templateUrl: 'login.component.html'
})

export class LoginComponent implements OnInit {
    loginForm: FormGroup;
    loading: boolean = false;
    submitted: boolean = false;
    returnUrl: string;
    isRemember: boolean = false;
    rememberUserName:string;
    public message: string;

    constructor(
        private formBuilder: FormBuilder,
        private route: ActivatedRoute,
        private router: Router,
        private authenticationService: AuthenticationService,
        private alertService: AlertService
    ) {
        // redirect to home if already logged in
        const currentUser = this.authenticationService.currentUserValue;
        if (currentUser && currentUser.userName != null) {

            //this.authenticationService.currentUserValue.em
            //resetpassword
            this.router.navigate(['/']);
        }

        const user = <AuthenticationToken>this.getFragment();
        //console.log(user);
        if (user) {
            this.authenticationService.loginExternal(user).subscribe(data => {
                //console.log(data);
                if (data) {
                    const redirectUri = location.protocol + '//' + location.host;
                    window.location.href = redirectUri;
                    //this.router.navigate(['/']);
                }
                else {
                    const redirectUri = location.protocol + '//' + location.host + environment.VDName + '/login';
                    //window.location.href = redirectUri;
                    this.router.navigate(['/login']);
                }
            });
        }

        localStorage.setItem('currentUser', JSON.stringify(user));
        let rememberUser = localStorage.getItem('rememberUser');
        if(rememberUser && rememberUser == '1'){
            this.isRemember = true;
            this.rememberUserName = localStorage.getItem('rememberUserName');
        }
        this.intiForm();
    }

    intiForm(){
        this.loginForm = this.formBuilder.group({
            username: [this.rememberUserName, Validators.required],
            password: ['', Validators.required]
        });
    }
    ngOnInit() {
        

        // get return url from route parameters or default to '/'
        this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
    }

    // convenience getter for easy access to form fields
    get f() { return this.loginForm.controls; }

    onSubmit() {
        this.submitted = true;

        // stop here if form is invalid
        if (this.loginForm.invalid) {
            return;
        }
        this.loading = true;
        this.authenticationService.login(this.f.username.value, this.f.password.value)
            .pipe(first())
            .subscribe(
                data => {
                    //console.log(data);
                    // this.router.navigate([this.returnUrl]);

                    this.getUserRole();
                },
                error => {
                    //console.log(error.error);
                    this.loading = false;

                    let message: string = "";
                    try{
                        error.forEach(m => {
                            message += m.value + "<br />"
                        });
                    }catch{}
                    
                    this.message = (message != "") ? message : 'Invalid username or password.';

                    this.alertService.error(this.message);
                });
    }


    authExternalProvider(provider: string) {
        let redirectUri = ''
        if(environment.VDName != ''){
            redirectUri = `${location.protocol}//${location.host}/${environment.VDName}/login`;
        }
        else{
            redirectUri = `${location.protocol}//${location.host}/login`;
        }

        let externalProviderUrl = environment.ApplicationServer + "/api/Account/ExternalLogin?provider=" + provider
            + "&response_type=token&client_id=self&accesskey=" + this.authenticationService.selectedApplication
            + "&redirect_uri=" + redirectUri;

        //window.$windowScope = $scope;
        window.location.href = externalProviderUrl;
        //window.open(externalProviderUrl, "Authenticate Account", "location=0,status=0,width=600,height=750");
    }
    getUserRole() {
        this.authenticationService.getRole().subscribe(role => {
            //console.log(this.authenticationService.currentUserValue);
            //this.router.navigate([this.returnUrl]);
            this.getUserAccess();
        })

    }

    getUserAccess() {
        this.authenticationService.getUserAccess().subscribe(access => {
            //console.log(this.authenticationService.currentUserValue);
            if (this.isRemember) {
                localStorage.setItem('rememberUser', "1");
                const currentUser = this.authenticationService.currentUserValue;
                if (currentUser && currentUser.userName != null) {
                    localStorage.setItem('rememberUserName', currentUser.userName);
                }
                else{
                    localStorage.setItem('rememberUser', "0");
                    localStorage.setItem('rememberUserName', '');
                }
            }
            else {
                localStorage.setItem('rememberUser', "0");
                localStorage.setItem('rememberUserName', '');
            }
            this.router.navigate([this.returnUrl]);
        })

    }

    getFragment() {
        if (window.location.search.indexOf('?') === 0) {
            return this.parseQueryString(window.location.search.substr(1));
        }
        else if (window.location.hash.indexOf('#') === 0) {
            return this.parseQueryString(window.location.hash.substr(1));
        } else {
            return null;
        }
    }

    parseQueryString(queryString: string) {
        let data = {},
            pairs, separatorIndex, escapedKey, escapedValue, key, value;

        if (queryString === null) {
            return data;
        }

        pairs = queryString.split('&');

        for (const pair of pairs) {

            separatorIndex = pair.indexOf('=');

            if (separatorIndex === -1) {
                escapedKey = pair;
                escapedValue = null;
            } else {
                escapedKey = pair.substr(0, separatorIndex);
                escapedValue = pair.substr(separatorIndex + 1);
            }

            key = decodeURIComponent(escapedKey);
            value = decodeURIComponent(escapedValue);

            data[key] = value;
        }

        return data;
    }

    AllowRememberMe(event:any) {
        this.isRemember = event.target.checked;
    }
}