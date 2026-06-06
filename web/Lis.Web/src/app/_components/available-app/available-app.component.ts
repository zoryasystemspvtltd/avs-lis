import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../../_services';
import { AvailableApps } from '../../_models';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

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
        if(!user.emailConfirmed){
          this.router.navigate(['/change-password']);
        }
        else{
          this.authenticationService.getRole().subscribe(role => {
            //console.log(this.currentUserValue);
            this.authenticationService.getUserAccess().subscribe(acess => {
              //console.log(this.currentUserValue);
              this.router.navigate(['/']);
            })
          })
        }
  }
}
