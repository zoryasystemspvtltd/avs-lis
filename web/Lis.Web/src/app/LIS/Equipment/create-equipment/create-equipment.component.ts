import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationToken } from '../../../_models';
import { AuthenticationService, UserService, AlertService, EquipmentService } from '../../../_services';

@Component({
  selector: 'app-create-equipment',
  templateUrl: './create-equipment.component.html',
  styleUrls: ['./create-equipment.component.css']
})
export class CreateEquipmentComponent implements OnInit {
  submitted: boolean = false;
  id: string;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: boolean;
  addEquipmentForm: FormGroup;
  loading: boolean = false;
  public message: string;
  selectedApplicationName: string;
  selectedApplicationId: string;
  models: any[];

  constructor(private authenticationService: AuthenticationService,
    private equipmentService: EquipmentService,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private router: Router) { }

  ngOnInit() {
    this.isLoaded = false;
    this.getAvailableModels();
  }

  getAvailableModels() {
    this.equipmentService.getModels()
      .subscribe(response => {
        this.models = response;
        this.isLoaded = true;
        this.initForms();
      });
  }

  initForms() {
    this.addEquipmentForm = this.formBuilder.group({
      name: ['', Validators.required],
      model: ['', Validators.required],
      accessKey: [this.newKey(), Validators.required]
    });
  }
  get f() { return this.addEquipmentForm.controls; }

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
    if (this.addEquipmentForm.invalid) {
      return;
    }
    let item = this.addEquipmentForm.value;

    //TODO Equipment service
    this.equipmentService.addEquipment(item)
      .subscribe(data => {

        this.loading = false;
        this.router.navigate(['/equipments/edit/', data.result]);
      },
        (error) => {
          let message: string = error;
          this.loading = false;
          this.message = (message != "") ? message : 'Data not saved.';
          this.alertService.error(this.message);
        });

    this.loading = true;
  }
  GenerateKey() {
    const key = this.newKey();
    this.addEquipmentForm.controls['accessKey'].setValue(key);
  }

  newKey() {
    return 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
      var r = Math.random() * 16 | 0,
        v = c == 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }
}
