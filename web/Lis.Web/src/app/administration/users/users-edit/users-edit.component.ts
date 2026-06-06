import { Component, OnInit } from '@angular/core';
import { AuthenticationToken } from '../../../_models';
import { UserService, AuthenticationService, AlertService } from '../../../_services';
import { ActivatedRoute, Router } from '@angular/router';
import { ɵINTERNAL_BROWSER_DYNAMIC_PLATFORM_PROVIDERS } from '@angular/platform-browser-dynamic';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';

@Component({
  selector: 'app-users-edit',
  templateUrl: './users-edit.component.html',
  styleUrls: ['./users-edit.component.css']
})
export class UsersEditComponent implements OnInit {

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
      // In a real app: dispatch action to load the details here.
      this.getProfileItemDetails();
    });

  }

  initForms() {
    this.item.is_blocked = (this.item.status == 1)?true:false;
    let dob = (this.item.dob != null)?this.item.dob.substring(0,10):null;
    this.editUserForm = this.formBuilder.group({
      id: [this.item.id, Validators.required],
      email: [this.item.email, [Validators.required,Validators.email]],
      first_name: [this.item.first_name, Validators.required],
      last_name: [this.item.last_name, Validators.required],
      phone_number: [this.item.phone_number],
      roles: [this.item.roles],
      applications: [this.item.applications]
    });
  }

  onSubmit() {
    this.submitted = true;
    // stop here if form is invalid
    if (this.editUserForm.invalid) {
      return;
    }
    let item = this.editUserForm.value;
    item.applications = this.item.applications;
    item.is_blocked = this.isUserBlocked;
    item.locked = this.isUserLocked;  
    item.email_confirmed = this.isUserEmailConfirmed;

    this.userService.editUser(item)
    .subscribe(data => { 
        this.loading = false;
        this.router.navigate(['/users']);
      },
      (error)=>{
        let message:string=error.message;
        
        this.loading = false;
        this.message = (message != "") ? message : 'Data not saved.';
        this.alertService.error(this.message);
      });

    this.loading = true;
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
    })
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
