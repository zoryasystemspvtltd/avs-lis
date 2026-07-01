import { Component, OnInit, OnDestroy } from '@angular/core';
import { AuthenticationToken } from '../../../_models';
import { UserService, AuthenticationService, AlertService } from '../../../_services';
import { ActivatedRoute, Router } from '@angular/router';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';

@Component({
  selector: 'app-users-edit',
  templateUrl: './users-edit.component.html',
  styleUrls: ['./users-edit.component.css']
})
export class UsersEditComponent implements OnInit, OnDestroy {

  id: string;
  private user: AuthenticationToken;
  item: any;
  profileitem:any;
  private sub: any;
  public isLoaded: Boolean;
  submitted: boolean = false;
  loading:boolean = false;
  editUserForm: FormGroup;
  validationError:any[]=[];
  public message: string;
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

    this.sub = this.route.params.subscribe(params => {
      this.isLoaded = false;
      this.id = params['id'];
      this.getProfileItemDetails();
    });

  }

  ngOnDestroy() {
    this.revokeSignaturePreview();
  }

  initForms() {
    this.item.is_blocked = (this.item.status == 1)?true:false;
    this.editUserForm = this.formBuilder.group({
      id: [this.item.id, Validators.required],
      email: [this.item.email, [Validators.required,Validators.email]],
      first_name: [this.item.first_name, Validators.required],
      last_name: [this.item.last_name, Validators.required],
      phone_number: [this.item.phone_number],
      doctor_designation: [this.item.doctor_designation || ''],
      roles: [this.item.roles],
      applications: [this.item.applications]
    });
    this.updateDoctorValidators();
    this.loadExistingSignaturePreview();
  }

  isDoctorRoleSelected(): boolean {
    return this.item?.roles?.some(role => role.isInRole && role.name === 'Doctor');
  }

  updateDoctorValidators() {
    if (!this.editUserForm) {
      return;
    }

    const designation = this.editUserForm.get('doctor_designation');
    if (this.isDoctorRoleSelected()) {
      designation.setValidators([Validators.required, Validators.maxLength(100)]);
    } else {
      designation.clearValidators();
      designation.setValue('');
      this.clearSignatureSelection();
    }
    designation.updateValueAndValidity();
  }

  loadExistingSignaturePreview() {
    if (!this.item?.doctor_signature_path) {
      return;
    }

    this.userService.getDoctorSignatureBlob(this.id)
      .subscribe(blob => {
        this.revokeSignaturePreview();
        this.signaturePreviewUrl = URL.createObjectURL(blob);
      });
  }

  onSignatureSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input?.files?.[0];
    this.signatureError = null;

    if (!file) {
      return;
    }

    const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
    if (!this.allowedSignatureExtensions.includes(extension)) {
      this.signatureError = 'Doctor signature must be a PNG or JPG image.';
      input.value = '';
      return;
    }

    if (file.size > this.maxSignatureSizeBytes) {
      this.signatureError = 'Doctor signature must not exceed 2 MB.';
      input.value = '';
      return;
    }

    this.signatureFile = file;
    this.revokeSignaturePreview();
    this.signaturePreviewUrl = URL.createObjectURL(file);
  }

  clearSignatureSelection() {
    this.signatureFile = null;
    if (!this.item?.doctor_signature_path) {
      this.revokeSignaturePreview();
    } else {
      this.loadExistingSignaturePreview();
    }
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

    if (this.editUserForm.invalid) {
      return;
    }

    if (this.isDoctorRoleSelected() && !this.signatureFile && !this.item.doctor_signature_path) {
      this.signatureError = 'Doctor signature is required for Doctor users.';
      return;
    }

    let item = this.editUserForm.value;
    item.applications = this.item.applications;
    item.roles = this.item.roles;
    item.is_blocked = this.isUserBlocked;
    item.locked = this.isUserLocked;  
    item.email_confirmed = this.isUserEmailConfirmed;
    item.doctor_designation = item.doctor_designation ? item.doctor_designation.trim() : null;

    this.loading = true;
    this.userService.editUser(item)
    .subscribe(data => { 
        if (this.isDoctorRoleSelected() && this.signatureFile) {
          this.userService.uploadDoctorSignature(this.id, this.signatureFile)
            .subscribe(() => {
              this.loading = false;
              this.router.navigate(['/users']);
            }, (error) => {
              this.loading = false;
              this.message = error?.message || 'User saved but doctor signature upload failed.';
              this.alertService.error(this.message);
            });
          return;
        }

        this.loading = false;
        this.router.navigate(['/users']);
      },
      (error)=>{
        let message:string=error.message;
        
        this.loading = false;
        this.message = (message != "") ? message : 'Data not saved.';
        this.alertService.error(this.message);
      });
  }

  getProfileItemDetails() {
    this.userService.getProfile()
      .subscribe(response => {
        this.profileitem = response;
        this.getItemDetails(this.id);
      });
  }

  getItemDetails(id: string) {
    this.userService.getById(id)
      .subscribe(response => {
        this.item = response;

        this.item.applications.forEach(app=>{
          let vapp = this.profileitem.applications.find(papp=>papp.key == app.key);
          if(vapp == null || vapp.isInApp == false){
            app.inProfile = false;
          }
          else{
            app.inProfile = true;
          }
        });

        this.item.roles.forEach(role=>{
          let vrole = this.profileitem.roles.find(prole=>prole.id == role.id);
          if(vrole == null || vrole.isInRole == false){
            role.inProfile = false;
          }
          else{
            role.inProfile = true;
          }
        })

        this.isLoaded = true;
        this.initForms();
      });
  }

  hasAccess(): boolean {
    return this.item.email != this.user.userName;
  }
  // convenience getter for easy access to form fields
  get f() { return this.editUserForm.controls; }

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

  onCheckApplication(event:any){
    this.item.applications.forEach(app=>{
      if(app.key == event.target.getAttribute('data-key')){
        app.isInApp = event.target.checked;
      }
    })
  }

  onCheckRole(event:any){
    this.item.roles.forEach(role=>{
      if(role.id == event.target.getAttribute('data-id')){
        role.isInRole = event.target.checked;
      }
    });
    this.updateDoctorValidators();
  }

  isUserLocked:boolean;
  isUserEmailConfirmed:boolean;
  isUserBlocked:boolean;

  onLockedChecked(value:boolean){
    this.isUserLocked = value;
  }

  onEmailConfirmedChecked(value:boolean){
    this.isUserEmailConfirmed = value;
  }

  onBlockedChecked(value:boolean){
    this.isUserBlocked = value;
  }
}
