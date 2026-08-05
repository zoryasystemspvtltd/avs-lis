import { AuthenticationToken } from '../_models';
import { UserAccess, MenuAccess } from '../_models/useraccess';
import { normalizeMenuKey } from '../administration/roles/role-permission-catalog';

export interface RoutePermissionRule {
  pattern: RegExp;
  modules: string[];
  access?: number;
  /** When set, menu overlay is enforced if the role has any menus for the matched module. */
  menuKey?: string;
  /** Alternate menu keys (OR). Used when a screen is reachable via multiple modules. */
  alternateMenuKeys?: string[];
}

/** Mirrors left-nav menu module resolution and bitmask checks (default access = 63). */
const SETUP_API_MODULES = [
  'Department', 'Specimens', 'ReferralDoctor', 'Corporate', 'TestGroup',
  'TestCategory', 'Unit', 'Method', 'SampleType', 'Container', 'TestProfile',
  'HisParameterMaster', 'HisParameterRangeMaster', 'TestMappingMaster',
  'TestParameterMappingMaster'
];

export const ROUTE_PERMISSION_RULES: RoutePermissionRule[] = [
  { pattern: /^\/sample-collection(?:\/|$)/, modules: ['SampleCollection'], menuKey: 'WORKING_BOARD_COLLECTION' },
  { pattern: /^\/sample-receiving(?:\/|$)/, modules: ['SampleReceiving'], menuKey: 'WORKING_BOARD_RECEIVING' },
  { pattern: /^\/radiology-report-entry(?:\/|$)/, modules: ['RadiologyReportEntry'], menuKey: 'WORKING_BOARD_RADIOLOGY_ENTRY' },
  { pattern: /^\/radiology-doctor-approvals(?:\/|$)/, modules: ['RadiologyDoctorApprovals'], menuKey: 'WORKING_BOARD_RADIOLOGY_APPROVAL' },
  { pattern: /^\/radiology-approved-reports(?:\/|$)/, modules: ['RadiologyDoctorApprovals'], menuKey: 'WORKING_BOARD_RADIOLOGY_APPROVED' },
  { pattern: /^\/doctorapprovals(?:\/|$)/, modules: ['DoctorsApprovals'], menuKey: 'WORKING_BOARD_DOCTOR_APPROVAL' },
  { pattern: /^\/doctor-samples(?:\/|$)/, modules: ['DoctorsApprovals'], menuKey: 'WORKING_BOARD_DOCTOR_APPROVAL' },
  { pattern: /^\/technicianapprovals(?:\/|$)/, modules: ['Reports'], menuKey: 'WORKING_BOARD_TECHNICIAN_APPROVAL' },
  { pattern: /^\/technician-samples(?:\/|$)/, modules: ['Reports'], menuKey: 'WORKING_BOARD_TECHNICIAN_APPROVAL' },
  { pattern: /^\/lab-result-entry(?:\/|$)/, modules: ['Reports', 'Samples'], menuKey: 'WORKING_BOARD_LAB_RESULT_EDIT' },
  { pattern: /^\/edit-test-results(?:\/|$)/, modules: ['Reports', 'Samples'], menuKey: 'WORKING_BOARD_LAB_RESULT_EDIT' },
  { pattern: /^\/samples(?:\/|$)/, modules: ['Samples'], menuKey: 'WORKING_BOARD_SAMPLES' },
  { pattern: /^\/approvedsamples(?:\/|$)/, modules: ['Reports', 'DoctorsApprovals'], menuKey: 'WORKING_BOARD_APPROVED_SAMPLES' },
  { pattern: /^\/rejectedsamples(?:\/|$)/, modules: ['Reports', 'DoctorsApprovals'], menuKey: 'WORKING_BOARD_REJECTED_SAMPLES' },
  { pattern: /^\/quality-controls(?:\/|$)/, modules: ['Reports', 'DoctorsApprovals'], menuKey: 'WORKING_BOARD_QUALITY_CONTROLS' },
  { pattern: /^\/users(?:\/|$)/, modules: ['Users'], menuKey: 'ACCOUNT_USERS' },
  { pattern: /^\/roles(?:\/|$)/, modules: ['Roles'], menuKey: 'ACCOUNT_ROLES' },
  { pattern: /^\/client-application(?:\/|$)/, modules: ['Users'] },
  { pattern: /^\/sale-invoices(?:\/|$)/, modules: ['SaleInvoices'], menuKey: 'TRANSACTION_SALEINVOICE' },
  { pattern: /^\/patient-master(?:\/|$)/, modules: ['PatientDetails', 'Masters'], menuKey: 'TRANSACTION_PATIENT' },
  { pattern: /^\/equipments(?:\/|$)/, modules: ['Equipments'], menuKey: 'SETUP_EQUIPMENT' },
  { pattern: /^\/equipment-heartbeat(?:\/|$)/, modules: ['Equipments'], menuKey: 'SETUP_EQUIPMENT_HEARTBEAT' },
  { pattern: /^\/notification-configuration(?:\/|$)/, modules: ['NotificationConfiguration'], menuKey: 'SETUP_NOTIFICATION_CONFIGURATION' },
  { pattern: /^\/test-master(?:\/|$)/, modules: ['HisTest'], menuKey: 'MASTER_TESTMASTER' },
  { pattern: /^\/test-rates(?:\/|$)/, modules: ['TestRates'], menuKey: 'MASTER_TESTRATE' },
  { pattern: /^\/reports\/radiology\/pending(?:\/|$)/, modules: ['RadiologyReports', 'Reports'], menuKey: 'REPORT_RADIOLOGY_PENDING' },
  { pattern: /^\/reports\/radiology\/authorized(?:\/|$)/, modules: ['RadiologyReports', 'Reports'], menuKey: 'REPORT_RADIOLOGY_AUTHORIZED' },
  { pattern: /^\/reports\/radiology\/modality-stats(?:\/|$)/, modules: ['RadiologyReports', 'Reports'], menuKey: 'REPORT_RADIOLOGY_MODALITY' },
  { pattern: /^\/reports\/radiology\/productivity(?:\/|$)/, modules: ['RadiologyReports', 'Reports'], menuKey: 'REPORT_RADIOLOGY_PRODUCTIVITY' },
  { pattern: /^\/reports\/radiology(?:\/|$)/, modules: ['RadiologyReports', 'Reports'] },
  { pattern: /^\/reports\/test-report(?:\/|$)/, modules: ['Reports', 'DoctorsApprovals'], menuKey: 'REPORT_DIAGNOSTIC' },
  { pattern: /^\/reports\/radiology-report(?:\/|$)/, modules: ['RadiologyReports', 'Reports'], menuKey: 'REPORT_RADIOLOGY' },
  { pattern: /^\/reports\/sale-invoice-register(?:\/|$)/, modules: ['Reports'], menuKey: 'REPORT_INVOICE_REGISTER' },
  { pattern: /^\/reports\/test-booking-register(?:\/|$)/, modules: ['Reports'], menuKey: 'REPORT_TEST_BOOKING' },
  { pattern: /^\/reports\/collection-summary(?:\/|$)/, modules: ['Reports'], menuKey: 'REPORT_COLLECTION_SUMMARY' },
  { pattern: /^\/reports\/collector-wise(?:\/|$)/, modules: ['Reports'], menuKey: 'REPORT_COLLECTOR_WISE' },
  { pattern: /^\/reports\/pending-collection(?:\/|$)/, modules: ['Reports'], menuKey: 'REPORT_PENDING_COLLECTION' },
  { pattern: /^\/reports\/recollection(?:\/|$)/, modules: ['Reports'], menuKey: 'REPORT_RECOLLECTION' },
  { pattern: /^\/reports\/received-samples(?:\/|$)/, modules: ['Reports'], menuKey: 'REPORT_RECEIVED_SAMPLES' },
  { pattern: /^\/reports\/rejected-samples(?:\/|$)/, modules: ['Reports'], menuKey: 'REPORT_REJECTED_SAMPLES' },
  { pattern: /^\/reports\/sample-turnaround(?:\/|$)/, modules: ['Reports'], menuKey: 'REPORT_TAT' },
  { pattern: /^\/reports(?:\/|$)/, modules: ['Reports'] },
  { pattern: /^\/departments(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_DEPARTMENT' },
  { pattern: /^\/specimens(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_SPECIMEN' },
  { pattern: /^\/referral-doctors(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_REFERRAL_DOCTOR' },
  { pattern: /^\/corporates(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_CORPORATE' },
  // These masters have no MenuCatalog entry (not grantable in Role edit): when the role
  // has a Masters menu overlay, the unknown key denies access; module-only roles keep access.
  { pattern: /^\/test-groups(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_TEST_GROUP' },
  { pattern: /^\/test-categories(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_TEST_CATEGORY' },
  { pattern: /^\/units(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_UNIT' },
  { pattern: /^\/methods(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_METHOD' },
  { pattern: /^\/sample-types(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_SAMPLE_TYPE' },
  { pattern: /^\/containers(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_CONTAINER' },
  { pattern: /^\/test-profiles(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_TESTPROFILE' },
  { pattern: /^\/his-parameters(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_PARAMETER' },
  { pattern: /^\/test-parameters(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_TEST_PARAM_MAPPING' },
  { pattern: /^\/test-mappings(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_ANALYZER_PARAM_MAPPING' },
  { pattern: /^\/his-parameter-ranges(?:\/|$)/, modules: ['Masters'], menuKey: 'MASTER_PARAMETER_RANGE' },
  { pattern: /^\/parameters(?:\/|$)/, modules: ['Equipments'] }
];

/** UserAccess API returns a JSON string; normalize to array for menu/guard checks. */
export function normalizeModuleAccess(access: any): UserAccess[] {
  if (!access) {
    return [];
  }
  if (typeof access === 'string') {
    try {
      const parsed = JSON.parse(access);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }
  if (Array.isArray(access)) {
    return access;
  }
  return [];
}

export function normalizeMenuAccess(access: any): MenuAccess[] {
  if (!access) {
    return [];
  }
  if (typeof access === 'string') {
    try {
      const parsed = JSON.parse(access);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }
  if (Array.isArray(access)) {
    return access;
  }
  return [];
}

/** Mirrors server QAuthorize Administrator bypass (full module bitmask from UserAccess API). */
export function isAdministrator(user: AuthenticationToken): boolean {
  const modules = normalizeModuleAccess(user?.access);
  if (modules.length < 10) {
    return false;
  }
  return modules.every(m => (parseInt(m.access as any, 10) & 63) === 63);
}

export function isEmailConfirmed(value: any): boolean {
  return value === true || value === 'true';
}

export function findModuleAccess(user: AuthenticationToken, module: string): number | null {
  if (!user) {
    return null;
  }
  if (isAdministrator(user)) {
    return 63;
  }

  const accessList = normalizeModuleAccess(user.access);
  if (!accessList.length) {
    return null;
  }

  let acc = accessList.find(a => a.name === module);
  if (acc) {
    return parseInt(acc.access as any, 10);
  }

  if (SETUP_API_MODULES.indexOf(module) >= 0) {
    acc = accessList.find(a => a.name === 'Masters');
  } else if (module === 'PatientMaster' || module === 'PatientDetails') {
    acc = accessList.find(a => a.name === 'PatientDetails') || accessList.find(a => a.name === 'Masters');
  } else if (module === 'TestRate') {
    acc = accessList.find(a => a.name === 'TestRates');
  } else if (module === 'SaleInvoice') {
    acc = accessList.find(a => a.name === 'SaleInvoices');
  }

  if (!acc) {
    return null;
  }

  return parseInt(acc.access as any, 10);
}

export function hasModuleAccess(user: AuthenticationToken, module: string, access = 63): boolean {
  if (isAdministrator(user)) {
    return true;
  }
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

/**
 * Module permission must exist (full nav bitmask). Menu overlay applies only when
 * the role has at least one menu row for that module; otherwise falls back to module behaviour.
 * Menu visibility requires CanView (32) on the menu row when overlay is active.
 */
export function hasMenuAccess(
  user: AuthenticationToken,
  module: string,
  menuKey: string,
  access = 32
): boolean {
  if (isAdministrator(user)) {
    return true;
  }
  // Nav visibility needs at least CanView on the module.
  if (!hasModuleAccess(user, module, 32)) {
    return false;
  }
  if (!menuKey) {
    return true;
  }

  const menus = normalizeMenuAccess(user.menuAccess);
  const moduleMenus = menus.filter(m =>
    (m.moduleName || '').toLowerCase() === (module || '').toLowerCase()
  );

  // No menu overlay for this module → existing module behaviour.
  if (!moduleMenus.length) {
    return true;
  }

  const want = normalizeMenuKey(menuKey);
  const row = moduleMenus.find(m =>
    normalizeMenuKey(m.menuKey || '') === want
  );
  if (!row) {
    return false;
  }
  const bits = parseInt(row.access as any, 10);
  // Accept either requested bits or at least CanView for visibility.
  const need = access || 32;
  return (bits & need) === need || (bits & 32) === 32;
}

export function hasAnyMenuAccess(
  user: AuthenticationToken,
  modules: string[],
  menuKey: string,
  access = 63,
  alternateMenuKeys?: string[]
): boolean {
  const keys = [menuKey].concat(alternateMenuKeys || []).filter(Boolean);
  for (const module of modules) {
    if (!menuKey) {
      if (hasModuleAccess(user, module, access)) {
        return true;
      }
      continue;
    }
    for (const key of keys) {
      if (hasMenuAccess(user, module, key, access)) {
        return true;
      }
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
