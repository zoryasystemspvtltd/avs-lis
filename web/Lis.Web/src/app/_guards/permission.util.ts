import { AuthenticationToken } from '../_models';

export interface RoutePermissionRule {
  pattern: RegExp;
  modules: string[];
  access?: number;
}

/** Mirrors left-nav menu module resolution and bitmask checks (default access = 63). */
const SETUP_API_MODULES = [
  'Department', 'Specimens', 'ReferralDoctor', 'Corporate', 'TestGroup',
  'TestCategory', 'Unit', 'Method', 'SampleType', 'Container', 'TestProfile',
  'HisParameterMaster', 'HisParameterRangeMaster', 'TestMappingMaster',
  'TestParameterMappingMaster', 'PatientMaster'
];

export const ROUTE_PERMISSION_RULES: RoutePermissionRule[] = [
  { pattern: /^\/sample-collection(?:\/|$)/, modules: ['SampleCollection'] },
  { pattern: /^\/sample-receiving(?:\/|$)/, modules: ['SampleReceiving'] },
  { pattern: /^\/radiology-report-entry(?:\/|$)/, modules: ['RadiologyReportEntry'] },
  { pattern: /^\/radiology-doctor-approvals(?:\/|$)/, modules: ['RadiologyDoctorApprovals'] },
  { pattern: /^\/radiology-approved-reports(?:\/|$)/, modules: ['RadiologyDoctorApprovals'] },
  { pattern: /^\/doctorapprovals(?:\/|$)/, modules: ['DoctorsApprovals'] },
  { pattern: /^\/doctor-samples(?:\/|$)/, modules: ['DoctorsApprovals'] },
  { pattern: /^\/technicianapprovals(?:\/|$)/, modules: ['Reports'] },
  { pattern: /^\/technician-samples(?:\/|$)/, modules: ['Reports'] },
  { pattern: /^\/lab-result-entry(?:\/|$)/, modules: ['Reports', 'Samples'] },
  { pattern: /^\/samples(?:\/|$)/, modules: ['Samples'] },
  { pattern: /^\/approvedsamples(?:\/|$)/, modules: ['Reports', 'DoctorsApprovals'] },
  { pattern: /^\/rejectedsamples(?:\/|$)/, modules: ['Reports', 'DoctorsApprovals'] },
  { pattern: /^\/quality-controls(?:\/|$)/, modules: ['Reports', 'DoctorsApprovals'] },
  { pattern: /^\/users(?:\/|$)/, modules: ['Users'] },
  { pattern: /^\/roles(?:\/|$)/, modules: ['Roles'] },
  { pattern: /^\/client-application(?:\/|$)/, modules: ['Users'] },
  { pattern: /^\/sale-invoices(?:\/|$)/, modules: ['SaleInvoices'] },
  { pattern: /^\/patient-master(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/equipments(?:\/|$)/, modules: ['Equipments'] },
  { pattern: /^\/equipment-heartbeat(?:\/|$)/, modules: ['Equipments'] },
  { pattern: /^\/test-master(?:\/|$)/, modules: ['HisTest'] },
  { pattern: /^\/test-rates(?:\/|$)/, modules: ['TestRates'] },
  { pattern: /^\/reports\/radiology(?:\/|$)/, modules: ['RadiologyReports', 'Reports'] },
  { pattern: /^\/reports\/test-report(?:\/|$)/, modules: ['Reports', 'DoctorsApprovals'] },
  { pattern: /^\/reports\/radiology-report(?:\/|$)/, modules: ['RadiologyReports', 'Reports'] },
  { pattern: /^\/reports(?:\/|$)/, modules: ['Reports'] },
  { pattern: /^\/departments(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/specimens(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/referral-doctors(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/corporates(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/test-groups(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/test-categories(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/units(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/methods(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/sample-types(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/containers(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/test-profiles(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/his-parameters(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/test-parameters(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/test-mappings(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/his-parameter-ranges(?:\/|$)/, modules: ['Masters'] },
  { pattern: /^\/parameters(?:\/|$)/, modules: ['Equipments'] }
];

export function findModuleAccess(user: AuthenticationToken, module: string): number | null {
  if (!user || !user.access) {
    return null;
  }

  let acc = user.access.find(a => a.name === module);
  if (acc) {
    return parseInt(acc.access as any, 10);
  }

  if (SETUP_API_MODULES.indexOf(module) >= 0) {
    acc = user.access.find(a => a.name === 'Masters');
  } else if (module === 'TestRate') {
    acc = user.access.find(a => a.name === 'TestRates');
  } else if (module === 'SaleInvoice') {
    acc = user.access.find(a => a.name === 'SaleInvoices');
  }

  if (!acc) {
    return null;
  }

  return parseInt(acc.access as any, 10);
}

export function hasModuleAccess(user: AuthenticationToken, module: string, access = 63): boolean {
  const bits = findModuleAccess(user, module);
  if (bits === null) {
    return false;
  }
  return (bits & access) === access;
}

export function hasAnyModuleAccess(user: AuthenticationToken, modules: string[], access = 63): boolean {
  for (const module of modules) {
    if (hasModuleAccess(user, module, access)) {
      return true;
    }
  }
  return false;
}

export function resolveRoutePermission(path: string): RoutePermissionRule | null {
  const normalized = (path || '').split('?')[0].toLowerCase();
  for (const rule of ROUTE_PERMISSION_RULES) {
    if (rule.pattern.test(normalized)) {
      return rule;
    }
  }
  return null;
}
