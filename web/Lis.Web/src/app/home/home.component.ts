import { Component } from '@angular/core';

/**
 * Landing page host. All dashboard behaviour lives in the dashboard framework
 * (`app-dashboard`), which resolves its widgets from the widget registry and RBAC.
 */
@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
}
