import { Component, OnInit } from '@angular/core';
import { AuthenticationToken } from '../../../_models';
import { ModuleService, AlertService, AuthenticationService, UserService } from '../../../_services';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-users-details',
  templateUrl: './users-details.component.html',
  styleUrls: ['./users-details.component.css']
})
export class UsersDetailsComponent implements OnInit {

  id: string;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: Boolean;

  constructor(private userService: UserService,
    private authenticationService: AuthenticationService,
    private route: ActivatedRoute,
    private alertService: AlertService,
  ) { }

  ngOnInit() {
    this.user = this.authenticationService.currentUserValue;

    this.sub = this.route.params.subscribe(params => {
      this.isLoaded = false;
      this.id = params['id'];
      // In a real app: dispatch action to load the details here.
      this.getItemDetails(this.id);
    });
  }

  getItemDetails(id: string) {
    this.userService.getById(id)
      .subscribe(response => {
        this.item = response;
        this.isLoaded = true;
      });
  }

  hasAccess(): boolean {
    return this.item.email != this.user.userName;
  }

  resetPassword() {
    this.isLoaded = false;
    let data = {
      email: this.item.email,
      password: 'DUMMY',
      code: 'DUMMY',
    }
    this.userService.forgetpasswordStep2(data)
      .subscribe(response => {
        let message = 'Password reset successful.';
        this.alertService.success(message);
        this.isLoaded = true;
      });
  }
}
