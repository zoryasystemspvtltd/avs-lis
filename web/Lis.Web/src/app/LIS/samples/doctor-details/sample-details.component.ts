import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationToken } from '../../../_models';
import { AuthenticationService, SampleService, AlertService } from '../../../_services';
import {SafeHtml,DomSanitizer} from '@angular/platform-browser';

@Component({
  selector: 'app-doc-sample-details.',
  templateUrl: './sample-details.component.html',
  styleUrls: ['./sample-details.component.css']
})
export class DoctorSampleDetailsComponent implements OnInit {
  id: number;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: Boolean;
  selectedApplicationName: string;
  sampleForm: FormGroup;
  message: string;
  selectedRunIndex: number = 0;

  constructor(private authenticationService: AuthenticationService,
    private sampleService: SampleService,
	private sanitizer: DomSanitizer,
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

    this.user = this.authenticationService.currentUserValue;
  }

  getItemDetails(id: number) {
    this.sampleService.getResult(id)
      .subscribe(response => {
        this.item = response;
        this.item.gender = this.getGender(this.item.gender);
        this.item.testRuns.forEach(r => {
          r.isSelected = false;
          if(r.reportStatus === 3){
            r.isSelected = true;
            this.selectedRunIndex = r.runIndex;
          }
        }) 
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

  hasAccess(access: number): boolean {
    if(!this.user){
      return false;
    }
    if(!this.user.access){
      return false;
    }

    let acc = this.user.access.find(a => a.name === 'DoctorsApprovals');
    if (acc == null) {
      return false;
    }

    return ((acc.access & access) == access);
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

  authorize(status: number) {
    let request = {
      note: this.item.doctorNote,
      status: status,
      id: this.id,
      runIndex: this.selectedRunIndex
    };


    //TODO Equipment service
    this.sampleService.authorizeSample(request)
      .subscribe(data => {
        this.router.navigate(['/doctorapprovals/']);
      },
        (error) => {
          let message: string = error;
          this.message = (message != "") ? message : 'Data not saved.';
          this.alertService.error(this.message);
        });
  }
  
  trustedHtml(note){
	  return this.sanitizer.bypassSecurityTrustHtml(note);
  }
}
