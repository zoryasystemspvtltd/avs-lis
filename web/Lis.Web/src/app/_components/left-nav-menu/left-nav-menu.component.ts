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

  constructor(public authenticationService: AuthenticationService) { }

  ngOnInit() {
    this.syncUserState();
    this.authenticationService.isUserChanged().subscribe(() => this.syncUserState());
  }

  private syncUserState() {
    const current = this.authenticationService.currentUserValue;
    if (current && current.accessToken) {
      this.authenticationService.isAuthenticated = true;
      this.isAuthenticated = true;
      this.user = current;
    }
  }

  private findAccess(module: string) {
    if (!this.user || !this.user.access) {
      return null;
    }
    let acc = this.user.access.find(a => a.name === module);
    if (acc) {
      return acc;
    }
    const setupApiModules = ['Department', 'Specimens', 'ReferralDoctor', 'Corporate', 'TestGroup',
      'TestCategory', 'Unit', 'Method', 'SampleType', 'Container', 'TestProfile'];
    if (setupApiModules.indexOf(module) >= 0) {
      return this.user.access.find(a => a.name === 'Masters');
    }
    if (module === 'TestRate') {
      return this.user.access.find(a => a.name === 'TestRates');
    }
    if (module === 'SaleInvoice') {
      return this.user.access.find(a => a.name === 'SaleInvoices');
    }
    return null;
  }

  hasAccess(module: string, access: number): boolean {
    const acc = this.findAccess(module);
    if (!acc) {
      return false;
    }
    return (parseInt(acc.access as any, 10) & access) === access;
  }

  hasGroupAccess(modules: string): boolean {
    const moduleArray = modules.split(',');
    for (let i = 0; i < moduleArray.length; i++) {
      if (this.hasAccess(moduleArray[i], 63)) {
        return true;
      }
    }
    return false;
  }

  hasSetupAccess(): boolean {
    return this.hasAccess('Masters', 63) || this.hasAccess('Equipments', 63);
  }

  hasMasterAccess(): boolean {
    return this.hasAccess('Masters', 63) || this.hasAccess('HisTest', 63) || this.hasAccess('TestRates', 63);
  }
}
