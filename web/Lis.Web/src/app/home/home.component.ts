import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationToken } from '../_models';
import { AlertService, AuthenticationService } from '../_services';
import { hasModuleAccess } from '../_guards/permission.util';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent implements OnInit {

  emailConfirmed = true;
  user: AuthenticationToken;
  constructor(private router: Router,
    public authenticationService: AuthenticationService,
    private alertService: AlertService) {
    this.refreshUserName();
  }

  ngOnInit(): void {
    this.refreshUserName();
  }

  refreshUserName() {
    this.authenticationService.isLoggedIn().subscribe(val => {
      if (val == true) {
        this.user = this.authenticationService.currentUserValue;

        if (this.user.emailConfirmed != 'true') {
          this.emailConfirmed = false;
        }
      }
    });
  }

  refreshOrder(event) {
    // Refresh dashboard recent samples when child emits (legacy hook)
  }

  hasAccess(module: string, access: number): boolean {
    if (!this.user) {
      return false;
    }
    return hasModuleAccess(this.user, module, access);
  }

  hasGroupAccess(modules: string): boolean {
    
    let moduleArray = modules.split(',');
    for(let i=0;i<moduleArray.length;i++){
      if(this.hasAccess(moduleArray[i],63)){
        return true;
      }
    }

    return false;
  }
}
