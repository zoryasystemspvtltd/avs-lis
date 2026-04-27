import { Component, OnInit } from '@angular/core';
import { AuthenticationService } from '../../_services';
import { AuthenticationToken } from '../../_models';


@Component({
  selector: 'app-left-nav-menu',
  templateUrl: './left-nav-menu.component.html',
  styleUrls: ['./left-nav-menu.component.css']
})
export class LeftNavMenuComponent implements OnInit {

  public isAuthenticated: boolean;
  user: AuthenticationToken;

  constructor(public authenticationService: AuthenticationService) {

  }


  ngOnInit() {
    this.authenticationService.isUserChanged().subscribe(data => {
      //console.log(data);
      this.isAuthenticated = data;
      if (this.isAuthenticated) {
        this.user = this.authenticationService.currentUserValue;
      }
    });


    if (this.authenticationService.isAuthenticated) {
      this.user = this.authenticationService.currentUserValue;
    }
  }

  hasAccess(module: string, access: number): boolean {
    if(!this.user){
      return false;
    }
    if(!this.user.access){
      return false;
    }
    let acc = this.user.access.find(a => a.name === module);
    if (acc == null) {
      return false;
    }
    return (acc.access > 0);
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
