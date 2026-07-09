import { Component, OnInit } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthenticationService } from '../../_services';
import { AuthenticationToken } from '../../_models';
import { normalizeModuleAccess, isAdministrator } from '../../_guards/permission.util';

@Component({
  selector: 'app-left-nav-menu',
  templateUrl: './left-nav-menu.component.html',
  styleUrls: ['./left-nav-menu.component.css']
})
export class LeftNavMenuComponent implements OnInit {

  public isAuthenticated: boolean;
  user: AuthenticationToken;
  expandWorkingBoard = false;
  expandSetup = false;
  expandMaster = false;
  expandTransaction = false;
  expandReports = false;
  expandAccount = false;

  constructor(
    public authenticationService: AuthenticationService,
    private router: Router
  ) { }

  ngOnInit() {
    this.syncUserState();
    this.authenticationService.isUserChanged().subscribe(() => this.syncUserState());
    this.updateExpandedSections(this.router.url);
    this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe((e: NavigationEnd) => {
      this.updateExpandedSections(e.urlAfterRedirects || e.url);
    });
  }

  private updateExpandedSections(url: string): void {
    const path = (url || '').split('?')[0].toLowerCase();
    this.expandWorkingBoard = this.matchesAny(path, [
      '/samples', '/sample-collection', '/sample-receiving', '/radiology-report-entry',
      '/radiology-doctor-approvals', '/radiology-approved-reports',
      '/lab-result-entry', '/technicianapprovals', '/doctorapprovals', '/approvedsamples',
      '/rejectedsamples', '/quality-controls'
    ]);
    this.expandSetup = this.matchesAny(path, ['/departments', '/units', '/methods', '/equipments', '/equipment-heartbeat']);
    this.expandMaster = this.matchesAny(path, [
      '/test-profiles', '/test-master', '/specimens', '/test-rates', '/referral-doctors', '/corporates',
      '/his-parameters', '/test-parameters', '/test-mappings', '/his-parameter-ranges'
    ]);
    this.expandTransaction = this.matchesAny(path, ['/patient-master', '/sale-invoices']);
    this.expandReports = this.matchesAny(path, [
      '/reports', '/fdd-report', '/radiology-reports', '/test-report'
    ]);
    this.expandAccount = this.matchesAny(path, ['/users', '/roles', '/change-password', '/activitylog']);
    if (path === '/' || path === '/home') {
      this.expandWorkingBoard = true;
    }
  }

  private matchesAny(path: string, prefixes: string[]): boolean {
    return prefixes.some(p => path === p || path.startsWith(p + '/'));
  }

  private syncUserState() {
    const current = this.authenticationService.currentUserValue;
    if (current && current.accessToken) {
      current.access = normalizeModuleAccess(current.access) as any;
      this.authenticationService.isAuthenticated = true;
      this.isAuthenticated = true;
      this.user = current;
    }
  }

  private findAccess(module: string) {
    if (!this.user) {
      return null;
    }
    if (isAdministrator(this.user)) {
      return { name: module, access: 63 };
    }
    const accessList = normalizeModuleAccess(this.user.access);
    if (!accessList.length) {
      return null;
    }
    let acc = accessList.find(a => a.name === module);
    if (acc) {
      return acc;
    }
    const setupApiModules = ['Department', 'Specimens', 'ReferralDoctor', 'Corporate', 'TestGroup',
      'TestCategory', 'Unit', 'Method', 'SampleType', 'Container', 'TestProfile'];
    if (setupApiModules.indexOf(module) >= 0) {
      return accessList.find(a => a.name === 'Masters');
    }
    if (module === 'TestRate') {
      return accessList.find(a => a.name === 'TestRates');
    }
    if (module === 'SaleInvoice') {
      return accessList.find(a => a.name === 'SaleInvoices');
    }
    return null;
  }

  hasAccess(module: string, access: number): boolean {
    if (isAdministrator(this.user)) {
      return true;
    }
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

  hasTransactionAccess(): boolean {
    return this.hasAccess('SaleInvoices', 63) || this.hasAccess('Masters', 63);
  }

  hasReportAccess(): boolean {
    return this.hasAccess('Reports', 63)
      || this.hasAccess('SaleInvoices', 63)
      || this.hasAccess('Samples', 63)
      || this.hasAccess('RadiologyReports', 63);
  }
}
