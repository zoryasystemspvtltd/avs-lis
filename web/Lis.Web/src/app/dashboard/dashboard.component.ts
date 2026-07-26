import { Component, OnInit } from '@angular/core';

import { AuthenticationToken } from '../_models';
import { AuthenticationService } from '../_services';
import { isAdministrator, isEmailConfirmed } from '../_guards/permission.util';
import { DashboardWidgetDef } from './dashboard.model';
import { resolveVisibleWidgets } from './widget-registry';

/**
 * Dashboard framework host. It owns no business data: it resolves which widgets the
 * signed-in user may see from the registry and renders them in order. Adding or
 * removing widgets is a registry change, and new roles work by assigning permissions.
 */
@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  user: AuthenticationToken;
  widgets: DashboardWidgetDef[] = [];
  passwordChangeRequired = false;
  today = new Date();

  constructor(public authenticationService: AuthenticationService) { }

  get displayName(): string {
    return this.user?.displayName || this.user?.userName || '';
  }

  get roleSummary(): string {
    if (!this.user) {
      return '';
    }
    if (isAdministrator(this.user)) {
      return 'Administrator';
    }
    const roles = this.user.roles || [];
    return roles.length ? roles.join(', ') : 'User';
  }

  ngOnInit(): void {
    this.authenticationService.isLoggedIn().subscribe(loggedIn => {
      if (!loggedIn) {
        return;
      }
      this.user = this.authenticationService.currentUserValue;
      this.passwordChangeRequired = !isEmailConfirmed(this.user?.emailConfirmed);
      this.widgets = resolveVisibleWidgets(this.user);
    });
  }

  trackByKey(index: number, widget: DashboardWidgetDef): string {
    return widget.key;
  }
}
