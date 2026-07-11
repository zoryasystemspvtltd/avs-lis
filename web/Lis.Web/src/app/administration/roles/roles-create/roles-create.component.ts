import { Component, OnInit } from '@angular/core';
import { AuthenticationToken } from '../../../_models';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ModuleService, UserService, AlertService, AuthenticationService } from '../../../_services';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-roles-create',
  templateUrl: './roles-create.component.html',
  styleUrls: ['./roles-create.component.css']
})
export class RolesCreateComponent implements OnInit {

  submitted: boolean = false;
  id: string;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: boolean;
  addRoleForm: FormGroup;
  loading: boolean = false;
  public message: string;
  selectedApplicationName:string;
  selectedApplicationId:string;

  constructor(private authenticationService:AuthenticationService,
    private userService: UserService,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private router: Router) { }

  ngOnInit() {
    this.getUserApps();
    
  }

  initForms() {
    this.addRoleForm = this.formBuilder.group({
      name: ['', Validators.required]
    });
  }
  get f() { return this.addRoleForm.controls; }

  hasAccess(): boolean {
    return true;
  }

  isInValid(field: string) {
    if (this.submitted) {
      if (this.f[field].errors && this.f[field].errors.required) {
        return true;
      }
    }
    return false;
  }

  onSubmit() {
    this.submitted = true;
    if (this.addRoleForm.invalid) {
      return;
    }
    const item = this.addRoleForm.value;

    this.loading = true;
    this.userService.addRole(item)
      .subscribe(data => {
        this.loading = false;
        const id = data && (data.id || data.Id);
        if (!id) {
          this.alertService.error('Role created but id was not returned.');
          return;
        }
        this.router.navigate(['/roles/edit/', id]);
      },
        (error) => {
          this.loading = false;
          let message = '';
          if (typeof error === 'string') {
            message = error;
          } else if (error && error.message) {
            message = error.message;
          }
          this.message = message !== '' ? message : 'Data not saved.';
          this.alertService.error(this.message);
        });
  }

  getUserApps() {
    this.authenticationService.getUserApps().subscribe(val => {
      let app = val.find(app => app.accessKey == this.authenticationService.selectedApplication);
      if(app == null){
        this.router.navigate(['/']);
      }
      this.selectedApplicationName = app.name;
      this.selectedApplicationId = app.id;
      this.initForms();
      this.isLoaded = true;
    });
  }

}
