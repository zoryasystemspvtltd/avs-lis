import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationToken } from '../../../_models';
import { AuthenticationService, QualityService, AlertService } from '../../../_services';
@Component({
  selector: 'app-quality-details.',
  templateUrl: './quality-details.component.html',
  styleUrls: ['./quality-details.component.css']
})
export class QualityDetailsComponent implements OnInit {
  id: number;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: Boolean;
  selectedApplicationName: string;
  sampleForm: FormGroup;
  message: string;

  constructor(private authenticationService: AuthenticationService,
    private qualityService: QualityService,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private router: Router) { }
    private controlResult ={
      sampleNo:'',
      equipmentName:'',
      resultDate:''
    }

  ngOnInit(): void {
    this.sub = this.route.params.subscribe(params => {
      this.isLoaded = false;
      this.id = params['id'];
      // In a real app: dispatch action to load the details here.
      this.getItemDetails();
    });
  }

  getItemDetails() {
    this.qualityService.getQualityResult(this.id)
      .subscribe(response => {
        this.item = response;
        this.isLoaded = true;
      });
  }
  
}
