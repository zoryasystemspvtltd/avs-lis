import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService,AuthenticationService, AlertService } from '../../_services';
import { AuthenticationToken, UserInfo } from '../../_models';
import { first } from 'rxjs/operators';


@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {

  registerForm: FormGroup;
    loading = false;
    submitted = false;
    returnUrl: string;

    public message: string;

    constructor(
        private formBuilder: FormBuilder,
        private route: ActivatedRoute,
        private router: Router,
        private authenticationService: AuthenticationService,
        private userService:UserService,
        private alertService: AlertService
    ) {
       
    }

    ngOnInit() {
        this.registerForm = this.formBuilder.group({
            firstname:['', Validators.required],
            lastname:['', Validators.required],
            email:['', Validators.required],
            phone_number:['', Validators.required]
        });

    }

    // convenience getter for easy access to form fields
    get f() { return this.registerForm.controls; }

    onSubmit() {

        this.submitted = true;
        // stop here if form is invalid
        if (this.registerForm.invalid) {
            return;
        }

        this.loading = true;
        const userInfo = new UserInfo();
        userInfo.email = this.f.email.value;
        userInfo.first_name = this.f.firstname.value;
        userInfo.last_name = this.f.lastname.value;
        userInfo.phone_number = this.f.phone_number.value;

        this.userService.register(userInfo)
            .pipe(first())
            .subscribe(
                data => {
                    this.loading = false;
                    this.message = "Account activation mail has been sent to your email address, please follow the instruction provided.";
                    this.alertService.success(this.message);
                },
                error => {
                    this.loading = false;
                    this.message = error ? error : 'The information provided is incorrect.';
                    this.alertService.error(this.message);
                });
    }
}
