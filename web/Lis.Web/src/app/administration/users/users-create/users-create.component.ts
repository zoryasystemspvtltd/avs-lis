import { Component, OnInit } from '@angular/core';
import { AuthenticationToken } from '../../../_models';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { UserService, AuthenticationService, AlertService } from '../../../_services';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-users-create',
  templateUrl: './users-create.component.html',
  styleUrls: ['./users-create.component.css']
})
export class UsersCreateComponent implements OnInit {

  id: string;
  private user: AuthenticationToken;

  private sub: any;
  public isLoaded: Boolean;
  submitted: boolean = false;
  loading: boolean = false;
  addUserForm: FormGroup;
  validationError: any[] = [];
  public message: string;
  profileitem: any;
  item: any = {};
  constructor(private userService: UserService,
    private authenticationService: AuthenticationService,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private router: Router) { }

  ngOnInit() {
    this.user = this.authenticationService.currentUserValue;
    this.getProfileItemDetails();

  }

  getProfileItemDetails() {
    this.userService.getProfile()
      .subscribe(response => {
        this.profileitem = response;
        this.item.applications = [];
        this.profileitem.applications.forEach(app => {
          if (app.isInApp == true) {
            app.inProfile = true;
          }
          else {
            app.inProfile = false;
          }

          this.item.applications.push({
            key: app.key,
            name: app.name,
            inProfile: app.inProfile,
            isInApp: true
          });
        });

        this.item.roles = [];
        this.profileitem.roles.forEach(role => {
          if (role.isInRole == true) {
            role.inProfile = true;
          }
          else {
            role.inProfile = false;
          }
          this.item.roles.push({
            id: role.id,
            name: role.name,
            isInRole: false,
            inProfile: role.inProfile
          });
        })
        this.isLoaded = true;
        this.initForms();
      });
  }

  initForms() {
    this.addUserForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      first_name: ['', Validators.required],
      last_name: ['', Validators.required],
      phone_number: [''],
      roles: [this.item.roles],
      applications: [this.item.applications]
    });
  }

  onSubmit() {
    this.submitted = true;
    // stop here if form is invalid
    if (this.addUserForm.invalid) {
      return;
    }
    let item = this.addUserForm.value;
    item.applications =this.item.applications;
    this.userService.addUser(item)
      .subscribe(data => {
        this.loading = false;
        this.router.navigate(['/users']);
      },
        (error) => {
          if (error.length > 0) {
            error.forEach(element => {
              this.alertService.error(element.value);
            });

          }
          else {
            let message: string = error.message;

            this.loading = false;
            this.message = (message != "") ? message : 'Data not saved.';
            this.alertService.error(this.message);
          }
        });

    this.loading = true;
  }

  hasAccess(): boolean {
    return true;
  }
  // convenience getter for easy access to form fields
  get f() { return this.addUserForm.controls; }

  isInValid(field: string) {
    if (this.submitted) {
      if (this.f[field].errors && this.f[field].errors.required) {
        return true;
      }
    }
    return false;
  }
  isInValidEmail(field: string) {
    if (this.submitted) {
      if (this.f[field].errors && this.f[field].errors.email) {
        return true;
      }
    }
    return false;
  }

  onCheckApplication(event: any) {
    this.item.applications.forEach(app => {
      if (app.key == event.target.getAttribute('data-key')) {
        app.isInApp = event.target.checked;
      }
    })
  }

  onCheckRole(event: any) {
    this.item.roles.forEach(role => {
      if (role.id == event.target.getAttribute('data-id')) {
        role.isInRole = event.target.checked;
      }
    })
  }


}
