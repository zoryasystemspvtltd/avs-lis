import { Component, OnInit } from '@angular/core';
import { AuthenticationToken } from '../../_models';
import { UserService, AuthenticationService } from '../../_services';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  id: string;
  private user: AuthenticationToken;
  item: any;
  private sub: any;
  public isLoaded: Boolean;

  constructor(private userService: UserService,
    private authenticationService: AuthenticationService) { }

  ngOnInit() {
    this.user = this.authenticationService.currentUserValue;
    
    this.isLoaded = false;
    // In a real app: dispatch action to load the details here.
    this.getItemDetails();
  }

  getItemDetails() {
    this.userService.getProfile()
      .subscribe(response => {
        this.item = response;
        this.isLoaded = true;
      });
  }

}
