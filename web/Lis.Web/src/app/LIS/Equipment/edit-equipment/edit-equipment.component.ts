import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationToken } from '../../../_models';
import { EquipmentService, AuthenticationService, AlertService } from '../../../_services';

@Component({
  selector: 'app-edit-equipment',
  templateUrl: './edit-equipment.component.html',
  styleUrls: ['./edit-equipment.component.css']
})
export class EditEquipmentComponent implements OnInit {
  id: string;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: Boolean;
  selectedApplicationName: string;
  editEquipmentForm: FormGroup;
  submitted: boolean = false;
  loading: boolean = false;
  public message: string;

  constructor(private equipmentService: EquipmentService,
    private authenticationService: AuthenticationService,
    private route: ActivatedRoute,
    private router: Router,
    private alertService: AlertService,
    private formBuilder: FormBuilder) { }

  ngOnInit() {
    this.sub = this.route.params.subscribe(params => {
      this.isLoaded = false;
      this.id = params['id'];
      // In a real app: dispatch action to load the details here.
      this.getUserApps();
    });
  }

  getItemDetails(id: string) {
    this.equipmentService.getById(id)
      .subscribe(response => {
        this.item = response;
        this.isLoaded = true;
        this.initForms(this.item);
      });
  }

  hasAccess(): boolean {
    return true;
  }

  getUserApps() {
    this.authenticationService.getUserApps().subscribe(val => {
      let app = val.find(app => app.accessKey == this.authenticationService.selectedApplication);
      if (app == null) {
        this.router.navigate(['/']);
      }
      this.selectedApplicationName = app.name;
      this.getItemDetails(this.id);
    });
  }

  isInValid(field: string) {
    if (this.submitted) {
      if (this.f[field].errors && this.f[field].errors.required) {
        return true;
      }
    }
    return false;
  }

  initForms(item) {
    this.editEquipmentForm = this.formBuilder.group({
      id: [item.id],
      name: [item.name, Validators.required],
      accessKey: [item.accessKey, Validators.required],
      model: [item.model, Validators.required]
    });
  }
  get f() { return this.editEquipmentForm.controls; }

  getMappings(event) {

  }

  onSubmit() {
    this.submitted = true;
    // stop here if form is invalid
    if (this.editEquipmentForm.invalid) {
      return;
    }
    let item = this.editEquipmentForm.value;
    this.equipmentService.editEquipment(item)
      .subscribe(data => {
        this.changeMappings();
      },
        (error) => {
          let message: string = error;
          this.loading = false;
          this.message = (message != "") ? message : 'Data not saved.';
          this.alertService.error(this.message);
        });

    this.loading = true;
  }

  changeMappings() {
    //this.item.mappings.forEach(m => m.equipmentId = this.item.id);
    this.item.nemapping=[];

    for (let map of this.item.mappings) {
      if (map.selectedTests != null && map.selectedTests.length > 0) {
        for (var i = 0; i < map.selectedTests.length; i++) {
          switch (i) {
            case 0:
              map.id = map.selectedTests[i].id;
              map.lisTestCode = map.selectedTests[i].code;
              map.lisTestCodeDescription = map.selectedTests[i].description;
              this.item.nemapping.push(map);
              break;
            default:
              var nemap = {
                equipmentId:map.equipmentId,
                hisTestCode: map.hisTestCode,
                id: map.selectedTests[i].id,
                lisTestCode: map.selectedTests[i].code,
                hisTestCodeDescription: map.hisTestCodeDescription,
                lisTestCodeDescription: map.selectedTests[i].description,
                isActive: map.isActive
              }
              this.item.nemapping.push(nemap);
              break;
          }
        }
      }
      else {
        map.lisTestCode = null;
        map.lisTestCodeDescription = null;
        this.item.nemapping.push(map);
      }
    }

    this.equipmentService.changeMappings(this.item.id,this.item.nemapping)
      .subscribe(data => {
        this.loading = false;
        this.router.navigate(['/equipments/', this.item.id]);
      },
        (error) => {
          let message: string = error;
          this.loading = false;
          this.message = (message != "") ? message : 'Data not saved.';
          this.alertService.error(this.message);
        });

    this.loading = true;
  }
}
