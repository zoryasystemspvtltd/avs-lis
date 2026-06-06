import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthenticationToken } from '../../../_models';
import { AuthenticationService, ParameterService } from '../../../_services';

@Component({
  selector: 'app-details-parameter',
  templateUrl: './details-parameter.component.html',
  styleUrls: ['./details-parameter.component.css']
})
export class DetailsParameterComponent implements OnInit {
  id: string;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: Boolean;
  selectedApplicationName:string;

  constructor(private parameterService: ParameterService,
    private authenticationService: AuthenticationService,
    private route: ActivatedRoute,
    private router: Router) { }

  ngOnInit() {
    this.sub = this.route.params.subscribe(params => {
      this.isLoaded = false;
      this.id = params['id'];
      // In a real app: dispatch action to load the details here.
      this.getUserApps();
    });
  }

  getItemDetails(id: string) {
    this.parameterService.getById(id)
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
      if(app == null){
        this.router.navigate(['/']);
      }
      this.selectedApplicationName = app.name;
      
      this.getItemDetails(this.id);
    });
  }
}
