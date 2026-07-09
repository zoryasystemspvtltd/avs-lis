import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../../_services';
import { AvailableApps } from '../../_models';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { isEmailConfirmed } from '../../_guards/permission.util';

@Component({
  selector: 'app-available-app',
  templateUrl: './available-app.component.html',
  styleUrls: ['./available-app.component.css']
})
export class AvailableAppComponent implements OnInit {

  public availableapps: AvailableApps[];
  public selectedApplication: string;
  showApplication:boolean=false;
  constructor(private router: Router, public authenticationService: AuthenticationService) { }

  ngOnInit() {
    this.getUserApps();

    this.selectedApplication = environment.ClientId;
  }

  getUserApps() {
    this.authenticationService.getUserApps().subscribe(val => {
      this.availableapps = val;
      this.showApplication = (this.availableapps.length > 1);
      this.changeApplication(this.availableapps[0]);
    });
  }

  changeApplication(app) {
    if(app == null){
      return;
    }
    
    this.selectedApplication = app.accessKey;
    this.authenticationService.selectedApplication = this.selectedApplication;

    let user = this.authenticationService.currentUserValue;
        if (!isEmailConfirmed(user?.emailConfirmed)) {
          this.router.navigate(['/change-password']);
        }
        else if (!user?.access || (typeof user.access === 'string') || (Array.isArray(user.access) && user.access.length === 0)) {
          this.authenticationService.getRole().subscribe(() => {
            this.authenticationService.getUserAccess().subscribe(() => {
              // Permissions loaded; stay on current route (do not force redirect).
            });
          });
        }
  }
}
