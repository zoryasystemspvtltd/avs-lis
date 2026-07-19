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
      // Re-sync module/menu permissions on every full load (browser refresh)
      // so role permission changes apply without re-login; fail-soft keeps the
      // cached session when the API is unreachable.
      this.authenticationService.getUserAccess().subscribe(() => { });
    }
    this.authenticationService.getResources().subscribe(() => { });
  }


}
