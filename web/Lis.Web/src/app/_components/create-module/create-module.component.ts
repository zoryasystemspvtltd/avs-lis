import { Component, OnInit, Input } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { AlertService, AuthenticationService, ModuleService } from '../../_services';
import { AuthenticationToken } from '../../_models';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-create-module',
  templateUrl: './create-module.component.html',
  styleUrls: ['./create-module.component.css']
})
export class CreateModuleComponent implements OnInit {

  private user: AuthenticationToken;
  @Input() schemma: any;


  moduleForm: FormGroup;
  loading: boolean = false;
  submitted: boolean = false;
  returnUrl: string;
  formElements: string[] = [];
  validationError: any[] = [];
  allowPublish: boolean = false;

  public message: string;

  constructor(
    private moduleService: ModuleService,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private authenticationService: AuthenticationService,
    private router: Router
  ) {

  }

  ngOnInit() {
    this.user = this.authenticationService.currentUserValue;

    let group: any = {};

    this.schemma.elements.forEach(row => {
      row.forEach(element => {
        if (element.type != 'blank'
            && element.type != 'answer-create') {
          if (element.required == true) {
            group[element.fieldName] = new FormControl(element.fieldName.value || '', Validators.required);
          }
          else {
            group[element.fieldName] = new FormControl(element.fieldName.value);
          }
          this.formElements.push(element.fieldName);
        }
        if (element.type == 'dropdown' && element.model != null) {
          element.data = [];
          this.getModuleItems(element.model)
            .subscribe(response => {
              response.items.forEach(item => {
                element.data.push({ id: item.id, name: item.name })
              });
            });;
        }
      });
    });
    this.moduleForm = new FormGroup(group);//this.formBuilder.group(JSON.parse(formConfiguration));

    this.moduleForm.controls['status'].patchValue(0);
  }

  // convenience getter for easy access to form fields
  get f() { return this.moduleForm.controls; }

  isInValid(field: string, isRequired: boolean) {
    if (this.submitted && isRequired) {
      if (this.f[field].errors) {
        return true;
      }
    }
    return false;
  }
  hasValidationError(field: string) {
    let hasError: boolean = false;

    this.validationError.forEach(item => {
      if (item.key.toLowerCase() == field.toLowerCase()) {
        hasError = true;
      }
    })
    return hasError;
  }

  showError(field: string) {
    let errorMessage: string = "";
    this.validationError.forEach(item => {
      if (item.key.toLowerCase() == field.toLowerCase()) {
        errorMessage = item.value;
      }
    })
    return errorMessage;
  }

  onSubmit() {

    this.submitted = true;
    // stop here if form is invalid
    if (this.moduleForm.invalid) {
      return;
    }
    let item = this.moduleForm.value;
    if (this.allowPublish) {
      item.status = 2;
    }

    this.moduleService.addItem(this.schemma.module, item)
      .subscribe(data => {
        this.loading = false;

        if (this.schemma.redirectto == 'details') {
          this.router.navigate(['/' + this.schemma.url,data]);
        }
        else if (this.schemma.redirectto == 'edit') {
          this.router.navigate(['/' + this.schemma.url,'edit',data]);
        }
        else {
          this.router.navigate(['/' + this.schemma.url]);
        }
      },
        (error) => {
          let message: string = "";
          this.validationError = [];
          error.forEach(m => {
            this.validationError.push({ key: m.key, value: m.value });
            message += m.value + "<br />"
          });
          this.loading = false;
          this.message = (message != "") ? message : 'Data not saved.';
          this.alertService.error(this.message);
        });

    this.loading = true;
  }

  hasAccess = function (type: number, status: number) {

    let _hasAccess: boolean = false;
    let acc = this.user.access.find(element => element.name == this.schemma.module);

    if (acc == null) {
      _hasAccess = false;
    }

    _hasAccess = ((parseInt(acc.access) & type) == type);

    if (this.schemma.hideAction && (status == 2 || status == 3)) {
      _hasAccess = false;
    }

    return _hasAccess;
  }

  getModuleItems(url: string) {
    let option = {
      'RecordPerPage': 0,
      'CurrentPage': 1,
      'SortColumnName': 'Name',
      'SortDirection': true,
      'SearchCondition': {
        'Name': 'Type',
        'Value': this.schemma.module
      }
    };

    if (url != 'category') {
      option.SearchCondition = null;
    }

    return this.moduleService.getItems(url, option);

  }

  onPublish() {
    this.allowPublish = true;
    this.onSubmit()
  }
}
