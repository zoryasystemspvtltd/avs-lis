import { Component, OnInit } from '@angular/core';
import { AuthenticationToken } from '../../../_models';
import { UserService, AuthenticationService, ModuleService } from '../../../_services';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-roles-details',
  templateUrl: './roles-details.component.html',
  styleUrls: ['./roles-details.component.css']
})
export class RolesDetailsComponent implements OnInit {

  id: string;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: Boolean;
  selectedApplicationName:string;

  constructor(private userService: UserService,
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

    this.userService.getRoleById(id)
      .subscribe(response => {
        this.item = response.items[0];
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
