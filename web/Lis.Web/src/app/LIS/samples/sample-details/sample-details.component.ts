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
        this.item = response || {};
        if (!this.item.parameters) {
          this.item.parameters = [];
        }
        if (this.item.patient) {
          this.item.patient.gender = this.getGender(this.item.patient.gender);
        }
        if (!this.item.barcodeText) {
          this.item.barcodeText = this.sampleService.buildBarcodeAnnotation(this.item);
        }
        this.isLoaded = true;
      }, () => {
        this.isLoaded = true;
        this.alertService.error('Unable to load sample details.');
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

  printBarcode() {
    const section = document.getElementById('print-section');
    if (!section) {
      this.alertService.error('Barcode is not ready to print.');
      return;
    }
    const popup = window.open('', '_blank', 'top=0,left=0,width=480,height=360');
    if (!popup) {
      this.alertService.error('Popup blocked. Allow popups to print barcode.');
      return;
    }
    popup.document.open();
    popup.document.write(
      '<html><head><title>Print Barcode</title>' +
      '<style>body{margin:8px;font-family:Arial,Helvetica,sans-serif;} @media print{body{margin:0;}}</style>' +
      '</head><body>' + section.innerHTML +
      '<script>window.onload=function(){window.focus();window.print();}</script>' +
      '</body></html>'
    );
    popup.document.close();
  }
}
