import { Component, OnInit } from '@angular/core';
import { AuthenticationToken } from '../../../_models';
import { ModuleService, AuthenticationService, AlertService, UserService } from '../../../_services';
import { ActivatedRoute, Router } from '@angular/router';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';

@Component({
  selector: 'app-roles-edit',
  templateUrl: './roles-edit.component.html',
  styleUrls: ['./roles-edit.component.css']
})
export class RolesEditComponent implements OnInit {

  submitted: boolean = false;
  id: string;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: Boolean;
  editRoleForm: FormGroup;
  loading: boolean = false;
  public message: string;
  selectedApplicationName: string;
  selectedApplicationId: string;

  constructor(
    private userService: UserService,
    private authenticationService: AuthenticationService,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private router: Router) { }

  ngOnInit() {
    this.sub = this.route.params.subscribe(params => {
      this.isLoaded = false;
      this.id = params['id'];
      // In a real app: dispatch action to load the details here.
      this.getUserApps();
    });
  }

  initForms() {
    this.editRoleForm = this.formBuilder.group({
      id: [this.item.id, Validators.required],
      name: [this.item.name, Validators.required],
      rolePermission: [this.item.rolePermission]
    });
  }
  get f() { return this.editRoleForm.controls; }

  getItemDetails(id: string) {

    this.userService.getRoleById(id)
      .subscribe(response => {
        this.item = response.items[0];
        this.initForms();
        this.isLoaded = true;
      });
  }

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
    // stop here if form is invalid
    if (this.editRoleForm.invalid) {
      return;
    }
    let item = this.editRoleForm.value;
    item.rolePermission.forEach(per => {
      per.applicationId = this.selectedApplicationId;
    })
    this.userService.editRole(item)
      .subscribe(data => {
        this.loading = false;
        this.router.navigate(['/roles']);
      },
        (error) => {
          let message: string = error.message;

          this.loading = false;
          this.message = (message != "") ? message : 'Data not saved.';
          this.alertService.error(this.message);
        });

    this.loading = true;
  }

  selectAll(element: any, status: boolean) {
    element.canAdd = status;
    element.canEdit = status;
    element.canAuthorize = status;
    element.canReject = status;
    element.canDelete = status;
    element.canView = status;
  }

  isAllSelected(element: any) {
    return element.canAdd &&
      element.canEdit &&
      element.canAuthorize &&
      element.canReject &&
      element.canDelete &&
      element.canView;
  }
  isNoneSelected(element: any) {
    return element.canAdd ||
      element.canEdit ||
      element.canAuthorize ||
      element.canReject ||
      element.canDelete ||
      element.canView;
  }
  onCheckRole(event: any, element: any, type: number) {
    switch (type) {
      case 1:
        element.canAdd = event.target.checked;
        break;
      case 2:
        element.canEdit = event.target.checked;
        break;
      case 4:
        element.canAuthorize = event.target.checked;
        break;
      case 8:
        element.canReject = event.target.checked;
        break;
      case 16:
        element.canDelete = event.target.checked;
        break;
      case 32:
        element.canView = event.target.checked;
        break;
    }
  }

  getUserApps() {
    this.authenticationService.getUserApps().subscribe(val => {
      let app = val.find(app => app.accessKey == this.authenticationService.selectedApplication);
      if (app == null) {
        this.router.navigate(['/']);
      }
      this.selectedApplicationName = app.name;
      this.selectedApplicationId = app.id;
      this.getItemDetails(this.id);
    });
  }
}
