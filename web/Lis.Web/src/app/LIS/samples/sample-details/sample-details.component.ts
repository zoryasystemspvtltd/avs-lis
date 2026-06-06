import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationToken } from '../../../_models';
import { AuthenticationService, SampleService, AlertService } from '../../../_services';
@Component({
  selector: 'app-sample-details.',
  templateUrl: './sample-details.component.html',
  styleUrls: ['./sample-details.component.css']
})
export class RawSampleDetailsComponent implements OnInit {
  id: number;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: Boolean;
  selectedApplicationName: string;
  sampleForm: FormGroup;
  message: string;

  constructor(private authenticationService: AuthenticationService,
    private sampleService: SampleService,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private router: Router) { }

  ngOnInit(): void {
    this.sub = this.route.params.subscribe(params => {
      this.isLoaded = false;
      this.id = +params['id'];
      // In a real app: dispatch action to load the details here.
      this.getUserApps();
    });
  }

  getItemDetails(id: number) {
    this.sampleService.getSample(id)
      .subscribe(response => {
        this.item = response;
        this.item.patient.gender = this.getGender(this.item.patient.gender);
        this.isLoaded = true;
      });
  }
  getGender(gender: any): string {
    if (gender == "M" || gender == "MALE") {
      return "MALE";
    }
    else {
      return "FEMALE";
    }
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
  hasAccess(){
    if(this.item && this.item.reportStatus == 1){return true;}
    return false;
  }

  rerunSample(status: number) {
    let request = {
      status: status,
      id: this.id
    };

    this.sampleService.reviewSample(request)
      .subscribe(data => {
        this.router.navigate(['/samples/']);
      },
        (error) => {
          let message: string = error;
          this.message = (message != "") ? message : 'Data not saved.';
          this.alertService.error(this.message);
        });
  }
}
