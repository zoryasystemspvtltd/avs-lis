import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationToken } from '../../../_models';
import { AuthenticationService, SampleService, AlertService } from '../../../_services';
@Component({
  selector: 'app-tech-search-details.',
  templateUrl: './sample-search.component.html',
  styleUrls: ['./sample-search.component.css']
})
export class TechnicianSampleSearchComponent implements OnInit {
  sampleNumber:string;
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
      this.sampleNumber = params['id'];
      // In a real app: dispatch action to load the details here.
      this.getUserApps();
    });
  }

  getItemDetails(id: string) {
    this.sampleService.getSearchResult(id)
      .subscribe(response => {
        this.item = response;
        this.isLoaded = true;
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

      this.getItemDetails(this.sampleNumber);
    });
  }

  
}
