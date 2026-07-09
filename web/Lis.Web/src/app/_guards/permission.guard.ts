import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthenticationService } from '../_services';
import { hasAnyModuleAccess, isAdministrator, resolveRoutePermission } from './permission.util';

@Injectable({ providedIn: 'root' })
export class PermissionGuard implements CanActivate {
  constructor(
    private router: Router,
    private authenticationService: AuthenticationService
  ) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const user = this.authenticationService.currentUserValue;
    if (!user || !user.accessToken) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    if (isAdministrator(user)) {
      return true;
    }

    const routeModules = route.data['modules'] as string[];
    const routeAccess = route.data['access'] as number;

    if (routeModules && routeModules.length > 0) {
      const access = routeAccess != null ? routeAccess : 63;
      if (hasAnyModuleAccess(user, routeModules, access)) {
        return true;
      }
      this.router.navigate(['/'], { queryParams: { denied: '1' } });
      return false;
    }

    const rule = resolveRoutePermission(state.url);
    if (!rule) {
      return true;
    }

    const access = rule.access != null ? rule.access : 63;
    if (hasAnyModuleAccess(user, rule.modules, access)) {
      return true;
    }

    this.router.navigate(['/'], { queryParams: { denied: '1' } });
    return false;
  }
}
