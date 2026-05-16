import { Component } from '@angular/core';
import { AuthenticationService } from './_services';
import { User } from './_models';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {


  constructor(
    private router: Router,
    public authenticationService: AuthenticationService
  ) {
    const user = this.authenticationService.currentUserValue;
    if (user && user.accessToken) {
      this.authenticationService.isAuthenticated = true;
      this.authenticationService.hideSideNav = false;
    }
    this.authenticationService.getResources().subscribe(() => { });
  }


}
