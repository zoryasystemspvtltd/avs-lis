import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';
import { UserService, AlertService, AuthenticationService } from '../../_services';
import { ResetPassword } from '../../_models';
import { first } from 'rxjs/operators';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css']
})
export class ForgotPasswordComponent implements OnInit {

  forgotPasswordForm: FormGroup;
  loading = false;
  submitted = false;
  message: string;
  isError = false;
  

  constructor(private formBuilder: FormBuilder,
    private userService: UserService,
    private alertService: AlertService,
    private authenticationService: AuthenticationService) { }

  ngOnInit() {
    this.forgotPasswordForm = this.formBuilder.group({
      email: ['', Validators.required]
    });
  }

  // convenience getter for easy access to form fields
  get f() { return this.forgotPasswordForm.controls; }

  onSubmit() {
    this.submitted = true;

    // stop here if form is invalid
    if (this.forgotPasswordForm.invalid) {
      return;
    }

    this.loading = true;
    const data = new ResetPassword();
    data.email = this.f.email.value;
    this.userService.forgetpassword(data)
      .pipe(first())
      .subscribe(
        data => {
          //console.log(data);
          
          this.message = this.authenticationService.getResource('FORGETPASSWORD_MAIL');
          this.alertService.success(this.message);
        },
        error => {
          //console.log(error.error);
          //this.alertService.error(error);
          this.loading = false;
          this.isError = true;
          //this.message = error.error ? error.error.error_description : 'The email is incorrect.';
          this.message = this.authenticationService.getResource('FORGETPASSWORD_MAIL_NOTFOUND');
          this.alertService.error(this.message);
        });
  }

}
