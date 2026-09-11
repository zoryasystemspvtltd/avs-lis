import { Component, OnInit, OnDestroy } from '@angular/core';
import { AuthenticationToken } from '../../../_models';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { UserService, AuthenticationService, AlertService } from '../../../_services';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-users-create',
  templateUrl: './users-create.component.html',
  styleUrls: ['./users-create.component.css']
})
export class UsersCreateComponent implements OnInit, OnDestroy {

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
  signatureFile: File = null;
  signaturePreviewUrl: string = null;
  signatureError: string = null;

  private readonly allowedSignatureExtensions = ['.png', '.jpg', '.jpeg'];
  private readonly maxSignatureSizeBytes = 2 * 1024 * 1024;

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

  ngOnDestroy() {
    this.revokeSignaturePreview();
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
      doctor_designation: [''],
      roles: [this.item.roles],
      applications: [this.item.applications]
    });
  }

  isDoctorRoleSelected(): boolean {
    return this.item?.roles?.some(role => role.isInRole && role.name === 'Doctor');
  }

  isTechnicianRoleSelected(): boolean {
    return this.item?.roles?.some(role => role.isInRole && role.name === 'Technician');
  }

  /** Signature section: Doctor and/or Technician (one shared user signature). */
  isSignatureRoleSelected(): boolean {
    return this.isDoctorRoleSelected() || this.isTechnicianRoleSelected();
  }

  updateDoctorValidators() {
    if (!this.addUserForm) {
      return;
    }

    const designation = this.addUserForm.get('doctor_designation');
    if (this.isDoctorRoleSelected()) {
      designation.setValidators([Validators.required, Validators.maxLength(100)]);
    } else {
      designation.clearValidators();
      designation.setValue('');
    }
    designation.updateValueAndValidity();

    if (!this.isSignatureRoleSelected()) {
      this.clearSignatureSelection();
    }
  }

  onSignatureSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input?.files?.[0];
    this.signatureError = null;

    if (!file) {
      this.clearSignatureSelection();
      return;
    }

    const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
    if (!this.allowedSignatureExtensions.includes(extension)) {
      this.signatureError = 'Signature must be a PNG or JPG image.';
      this.clearSignatureSelection();
      input.value = '';
      return;
    }

    if (file.size > this.maxSignatureSizeBytes) {
      this.signatureError = 'Signature must not exceed 2 MB.';
      this.clearSignatureSelection();
      input.value = '';
      return;
    }

    this.signatureFile = file;
    this.revokeSignaturePreview();
    this.signaturePreviewUrl = URL.createObjectURL(file);
  }

  clearSignatureSelection() {
    this.signatureFile = null;
    this.revokeSignaturePreview();
  }

  revokeSignaturePreview() {
    if (this.signaturePreviewUrl) {
      URL.revokeObjectURL(this.signaturePreviewUrl);
      this.signaturePreviewUrl = null;
    }
  }

  onSubmit() {
    this.submitted = true;
    this.signatureError = null;
    this.updateDoctorValidators();

    if (this.addUserForm.invalid) {
      return;
    }

    if (this.isSignatureRoleSelected() && !this.signatureFile) {
      this.signatureError = 'Signature is required for Doctor or Technician users.';
      return;
    }

    let item = this.addUserForm.value;
    item.applications = this.item.applications;
    item.roles = this.item.roles;
    item.doctor_designation = item.doctor_designation ? item.doctor_designation.trim() : null;

    this.loading = true;
    this.userService.addUser(item)
      .subscribe(data => {
        const userId = data.id;
        if (this.isSignatureRoleSelected() && this.signatureFile) {
          this.userService.uploadDoctorSignature(userId, this.signatureFile)
            .subscribe(() => {
              this.loading = false;
              this.router.navigate(['/users']);
            }, (error) => {
              this.loading = false;
              this.message = error?.message || 'User created but signature upload failed.';
              this.alertService.error(this.message);
              this.router.navigate(['/users', 'edit', userId]);
            });
          return;
        }

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

  isInValidMaxLength(field: string) {
    if (this.submitted) {
      if (this.f[field].errors && this.f[field].errors.maxlength) {
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
    });
    this.updateDoctorValidators();
  }


}
