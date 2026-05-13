import { Component } from '@angular/core';
import { AuthenticationService } from '../../_services';
import { User } from '../../_models';
import { Router, NavigationEnd } from '@angular/router';
import { first } from 'rxjs/operators';

@Component({
	selector: 'app-nav-menu',
	templateUrl: './nav-menu.component.html',
	styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent {
	isExpanded = false;

	public userName: string;
	public displayName: string;
	private user: User;
	public loginText: string;
	public isAuthenticated: boolean;
	isReLoginTrigerred: boolean = false;

	constructor(private router: Router,
		public authenticationService: AuthenticationService) {
		this.refreshUserName();
	}


	refreshUserName() {


		this.router.events.subscribe((val) => {
			this.loginText = "login";
			this.isAuthenticated = false;
			this.authenticationService.isLoggedIn().subscribe(val => {
				//console.log(val);
				if (val == true) {
					this.isAuthenticated = true;
					this.loginText = "logout";
					this.user = this.authenticationService.currentUserValue;
					this.userName = this.user.userName;
					this.displayName = (this.user.displayName != null) ? this.user.displayName : this.user.userName;

				}
			});

			this.authenticationService.isExpired().subscribe(val => {
				if (val) {
					if (!this.isReLoginTrigerred) {
						this.authenticationService.relogin()
							.subscribe(
								data => {
									this.isReLoginTrigerred = false;
								});
						this.isReLoginTrigerred = true;
					}
				}
			});

		});
	}

	ngOnInit() {
		this.refreshUserName();
	}

	logout() {
		this.authenticationService.isLoggedIn().subscribe(val => {
			//console.log(val);
			if (val == true) {
				this.authenticationService.logout().subscribe(val => {
					//console.log(val);
					if (val == true) {
						this.router.navigate(['/login']);
					}
				});
			}
			else{
				this.router.navigate(['/login']);
			}
		});
		
		
	}

	collapse() {
		this.isExpanded = false;
	}

	toggle() {
		this.isExpanded = !this.isExpanded;
	}


}
