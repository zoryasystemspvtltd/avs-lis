import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationToken } from '../../../_models';
import { AuthenticationService, SampleService, AlertService } from '../../../_services';
@Component({
  selector: 'app-his-sample',
  templateUrl: './his-sample.component.html',
  styleUrls: ['./his-sample.component.css']
})
export class HisSampleComponent implements OnInit {
  id: number;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: Boolean;
  selectedApplicationName: string;
  sampleForm: FormGroup;
  message: string;
  @Output() onGetOrder = new EventEmitter<boolean>();
  isInProgress: boolean = false;

  constructor(private authenticationService: AuthenticationService,
    private sampleService: SampleService,
    private route: ActivatedRoute,
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

    });
  }

  getHisSample() {
    this.isInProgress = true;
    this.sampleService.getHisSamples()
      .subscribe(data => {
        this.message = 'Orders collected from HIS';
        this.alertService.success(this.message);
        this.isInProgress = false;
        this.onGetOrder.emit(true);
      },
        (error) => {
          let message: string = error;
          this.message = (message != "") ? message : 'Error to Collect HID Order.';
          this.isInProgress = false;
          this.alertService.error(this.message);
        });
  }
}
