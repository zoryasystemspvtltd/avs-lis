/**
 * Frontend mirror of API MenuCatalog — section → module → menu tree for Role Edit + guards/nav.
 * Keys must stay in sync with Lis.Api.Providers.MenuCatalog.
 */
export interface RoleMenuDef {
  menuKey: string;
  moduleName: string;
  section: string;
  label: string;
  route: string;
  order: number;
}

export interface RolePermissionSection {
  id: string;
  title: string;
  /** UserModule names toggled by "Module Enabled" for this section */
  moduleNames: string[];
  menus: RoleMenuDef[];
}

const MENUS: RoleMenuDef[] = [
  // Working Board
  { menuKey: 'WORKING_BOARD_SAMPLES', moduleName: 'Samples', section: 'Working Board', label: 'Recent Samples', route: '/samples', order: 10 },
  { menuKey: 'WORKING_BOARD_COLLECTION', moduleName: 'SampleCollection', section: 'Working Board', label: 'Sample Collection', route: '/sample-collection', order: 20 },
  { menuKey: 'WORKING_BOARD_RECEIVING', moduleName: 'SampleReceiving', section: 'Working Board', label: 'Sample Receiving', route: '/sample-receiving', order: 30 },
  { menuKey: 'WORKING_BOARD_RADIOLOGY_ENTRY', moduleName: 'RadiologyReportEntry', section: 'Working Board', label: 'Radiology Entry', route: '/radiology-report-entry', order: 40 },
  { menuKey: 'WORKING_BOARD_RADIOLOGY_APPROVAL', moduleName: 'RadiologyDoctorApprovals', section: 'Working Board', label: 'Radiology Doctor Approval', route: '/radiology-doctor-approvals', order: 50 },
  { menuKey: 'WORKING_BOARD_RADIOLOGY_APPROVED', moduleName: 'RadiologyDoctorApprovals', section: 'Working Board', label: 'Approved Radiology Reports', route: '/radiology-approved-reports', order: 60 },
  { menuKey: 'WORKING_BOARD_TECHNICIAN_APPROVAL', moduleName: 'Reports', section: 'Working Board', label: 'Technician Approval', route: '/technicianapprovals', order: 70 },
  { menuKey: 'WORKING_BOARD_LAB_RESULT_EDIT', moduleName: 'Reports', section: 'Working Board', label: 'Lab Result Entry', route: '/edit-test-results', order: 80 },
  { menuKey: 'WORKING_BOARD_DOCTOR_APPROVAL', moduleName: 'DoctorsApprovals', section: 'Working Board', label: 'Doctor Approval', route: '/doctorapprovals', order: 90 },
  { menuKey: 'WORKING_BOARD_APPROVED_SAMPLES', moduleName: 'Reports', section: 'Working Board', label: 'Approved Samples', route: '/approvedsamples', order: 100 },
  { menuKey: 'WORKING_BOARD_REJECTED_SAMPLES', moduleName: 'Reports', section: 'Working Board', label: 'Rejected Samples', route: '/rejectedsamples', order: 110 },
  { menuKey: 'WORKING_BOARD_QUALITY_CONTROLS', moduleName: 'Reports', section: 'Working Board', label: 'Quality Controls', route: '/quality-controls', order: 120 },

  // Masters
  { menuKey: 'MASTER_DEPARTMENT', moduleName: 'Masters', section: 'Masters', label: 'Department', route: '/departments', order: 10 },
  { menuKey: 'MASTER_UNIT', moduleName: 'Masters', section: 'Masters', label: 'Unit', route: '/units', order: 20 },
  { menuKey: 'MASTER_METHOD', moduleName: 'Masters', section: 'Masters', label: 'Method', route: '/methods', order: 30 },
  { menuKey: 'SETUP_EQUIPMENT', moduleName: 'Equipments', section: 'Masters', label: 'Equipment', route: '/equipments', order: 40 },
  { menuKey: 'SETUP_EQUIPMENT_HEARTBEAT', moduleName: 'Equipments', section: 'Masters', label: 'Equipment Heartbeat', route: '/equipment-heartbeat', order: 50 },
  { menuKey: 'SETUP_NOTIFICATION_CONFIGURATION', moduleName: 'NotificationConfiguration', section: 'Masters', label: 'Notification Configuration', route: '/notification-configuration', order: 55 },
  { menuKey: 'MASTER_TESTMASTER', moduleName: 'HisTest', section: 'Masters', label: 'Test Master', route: '/test-master', order: 60 },
  { menuKey: 'MASTER_TESTPROFILE', moduleName: 'Masters', section: 'Masters', label: 'Test Profile', route: '/test-profiles', order: 70 },
  { menuKey: 'MASTER_SPECIMEN', moduleName: 'Masters', section: 'Masters', label: 'Specimen', route: '/specimens', order: 80 },
  { menuKey: 'MASTER_TESTRATE', moduleName: 'TestRates', section: 'Masters', label: 'Test Rate', route: '/test-rates', order: 90 },
  { menuKey: 'MASTER_REFERRAL_DOCTOR', moduleName: 'Masters', section: 'Masters', label: 'Referral Doctor', route: '/referral-doctors', order: 100 },
  { menuKey: 'MASTER_CORPORATE', moduleName: 'Masters', section: 'Masters', label: 'Corporate', route: '/corporates', order: 110 },
  { menuKey: 'MASTER_PARAMETER', moduleName: 'Masters', section: 'Masters', label: 'Parameter Master', route: '/his-parameters', order: 120 },
  { menuKey: 'MASTER_TEST_PARAM_MAPPING', moduleName: 'Masters', section: 'Masters', label: 'Test Parameter Mapping', route: '/test-parameters', order: 130 },
  { menuKey: 'MASTER_ANALYZER_PARAM_MAPPING', moduleName: 'Masters', section: 'Masters', label: 'Analyzer Parameter Mapping', route: '/test-mappings', order: 140 },
  { menuKey: 'MASTER_PARAMETER_RANGE', moduleName: 'Masters', section: 'Masters', label: 'Parameter Range', route: '/his-parameter-ranges', order: 150 },

  // Transaction
  { menuKey: 'TRANSACTION_PATIENT', moduleName: 'PatientDetails', section: 'Transaction', label: 'Patient Details', route: '/patient-master', order: 10 },
  { menuKey: 'TRANSACTION_SALEINVOICE', moduleName: 'SaleInvoices', section: 'Transaction', label: 'Sale Invoice', route: '/sale-invoices', order: 20 },

  // Reports
  { menuKey: 'REPORT_INVOICE_REGISTER', moduleName: 'Reports', section: 'Reports', label: 'Invoice Reports', route: '/reports/sale-invoice-register', order: 10 },
  { menuKey: 'REPORT_TEST_BOOKING', moduleName: 'Reports', section: 'Reports', label: 'Test Booking Register', route: '/reports/test-booking-register', order: 20 },
  { menuKey: 'REPORT_DIAGNOSTIC', moduleName: 'Reports', section: 'Reports', label: 'Diagnostic Report', route: '/reports/test-report', order: 30 },
  { menuKey: 'REPORT_RADIOLOGY', moduleName: 'RadiologyReports', section: 'Reports', label: 'Radiology Reports', route: '/reports/radiology-report', order: 40 },
  { menuKey: 'REPORT_COLLECTION_SUMMARY', moduleName: 'Reports', section: 'Reports', label: 'Collection Reports', route: '/reports/collection-summary', order: 50 },
  { menuKey: 'REPORT_COLLECTOR_WISE', moduleName: 'Reports', section: 'Reports', label: 'Collector Wise Report', route: '/reports/collector-wise', order: 60 },
  { menuKey: 'REPORT_PENDING_COLLECTION', moduleName: 'Reports', section: 'Reports', label: 'Pending Collection', route: '/reports/pending-collection', order: 70 },
  { menuKey: 'REPORT_RECOLLECTION', moduleName: 'Reports', section: 'Reports', label: 'Recollection Report', route: '/reports/recollection', order: 80 },
  { menuKey: 'REPORT_RECEIVED_SAMPLES', moduleName: 'Reports', section: 'Reports', label: 'Received Samples', route: '/reports/received-samples', order: 90 },
  { menuKey: 'REPORT_REJECTED_SAMPLES', moduleName: 'Reports', section: 'Reports', label: 'Rejected Samples', route: '/reports/rejected-samples', order: 100 },
  { menuKey: 'REPORT_TAT', moduleName: 'Reports', section: 'Reports', label: 'TAT Reports', route: '/reports/sample-turnaround', order: 110 },
  { menuKey: 'REPORT_RADIOLOGY_PENDING', moduleName: 'RadiologyReports', section: 'Reports', label: 'Pending Radiology Cases', route: '/reports/radiology/pending', order: 120 },
  { menuKey: 'REPORT_RADIOLOGY_AUTHORIZED', moduleName: 'RadiologyReports', section: 'Reports', label: 'Authorized Radiology', route: '/reports/radiology/authorized', order: 130 },
  { menuKey: 'REPORT_RADIOLOGY_MODALITY', moduleName: 'RadiologyReports', section: 'Reports', label: 'Modality Statistics', route: '/reports/radiology/modality-stats', order: 140 },
  { menuKey: 'REPORT_RADIOLOGY_PRODUCTIVITY', moduleName: 'RadiologyReports', section: 'Reports', label: 'Radiologist Productivity', route: '/reports/radiology/productivity', order: 150 },

  // Account
  { menuKey: 'ACCOUNT_USERS', moduleName: 'Users', section: 'Account', label: 'Users', route: '/users', order: 10 },
  { menuKey: 'ACCOUNT_ROLES', moduleName: 'Roles', section: 'Account', label: 'Roles', route: '/roles', order: 20 },
];

const LEGACY_MENU_KEYS: { [k: string]: string } = {
  'workingboard.recentSamples': 'WORKING_BOARD_SAMPLES',
  'workingboard.sampleCollection': 'WORKING_BOARD_COLLECTION',
  'workingboard.sampleReceiving': 'WORKING_BOARD_RECEIVING',
  'workingboard.radiologyReportEntry': 'WORKING_BOARD_RADIOLOGY_ENTRY',
  'workingboard.radiologyDoctorApproval': 'WORKING_BOARD_RADIOLOGY_APPROVAL',
  'workingboard.radiologyApproved': 'WORKING_BOARD_RADIOLOGY_APPROVED',
  'workingboard.technicianApproval': 'WORKING_BOARD_TECHNICIAN_APPROVAL',
  'workingboard.testResultEdit': 'WORKING_BOARD_LAB_RESULT_EDIT',
  'workingboard.doctorApproval': 'WORKING_BOARD_DOCTOR_APPROVAL',
  'workingboard.approvedSamples': 'WORKING_BOARD_APPROVED_SAMPLES',
  'workingboard.rejectedSamples': 'WORKING_BOARD_REJECTED_SAMPLES',
  'workingboard.qualityControls': 'WORKING_BOARD_QUALITY_CONTROLS',
  'setup.department': 'MASTER_DEPARTMENT',
  'setup.unit': 'MASTER_UNIT',
  'setup.method': 'MASTER_METHOD',
  'setup.equipment': 'SETUP_EQUIPMENT',
  'setup.equipmentHeartbeat': 'SETUP_EQUIPMENT_HEARTBEAT',
  'setup.notificationConfiguration': 'SETUP_NOTIFICATION_CONFIGURATION',
  'masters.testMaster': 'MASTER_TESTMASTER',
  'masters.testProfile': 'MASTER_TESTPROFILE',
  'masters.specimen': 'MASTER_SPECIMEN',
  'masters.testRate': 'MASTER_TESTRATE',
  'masters.referralDoctor': 'MASTER_REFERRAL_DOCTOR',
  'masters.corporate': 'MASTER_CORPORATE',
  'masters.parameter': 'MASTER_PARAMETER',
  'masters.testParamMapping': 'MASTER_TEST_PARAM_MAPPING',
  'masters.analyzerParamMapping': 'MASTER_ANALYZER_PARAM_MAPPING',
  'masters.parameterRange': 'MASTER_PARAMETER_RANGE',
  'transaction.patientDetails': 'TRANSACTION_PATIENT',
  'transaction.saleInvoice': 'TRANSACTION_SALEINVOICE',
  'reports.saleInvoiceRegister': 'REPORT_INVOICE_REGISTER',
  'reports.testBookingRegister': 'REPORT_TEST_BOOKING',
  'reports.diagnosticReport': 'REPORT_DIAGNOSTIC',
  'reports.radiologyReportPrint': 'REPORT_RADIOLOGY',
  'reports.collectionSummary': 'REPORT_COLLECTION_SUMMARY',
  'reports.collectorWise': 'REPORT_COLLECTOR_WISE',
  'reports.pendingCollection': 'REPORT_PENDING_COLLECTION',
  'reports.recollection': 'REPORT_RECOLLECTION',
  'reports.receivedSamples': 'REPORT_RECEIVED_SAMPLES',
  'reports.rejectedSamples': 'REPORT_REJECTED_SAMPLES',
  'reports.turnaround': 'REPORT_TAT',
  'reports.radiologyPending': 'REPORT_RADIOLOGY_PENDING',
  'reports.radiologyAuthorized': 'REPORT_RADIOLOGY_AUTHORIZED',
  'reports.radiologyModality': 'REPORT_RADIOLOGY_MODALITY',
  'reports.radiologyProductivity': 'REPORT_RADIOLOGY_PRODUCTIVITY',
  'account.users': 'ACCOUNT_USERS',
  'account.roles': 'ACCOUNT_ROLES'
};

export function normalizeMenuKey(menuKey: string): string {
  if (!menuKey) {
    return menuKey;
  }
  const mapped = LEGACY_MENU_KEYS[menuKey];
  return mapped || menuKey;
}

export function buildRolePermissionSections(): RolePermissionSection[] {
  const order = ['Working Board', 'Masters', 'Transaction', 'Reports', 'Account'];
  return order.map(title => {
    const menus = MENUS.filter(m => m.section === title).sort((a, b) => a.order - b.order);
    const moduleNames: string[] = [];
    menus.forEach(m => {
      if (moduleNames.indexOf(m.moduleName) < 0) {
        moduleNames.push(m.moduleName);
      }
    });
    return {
      id: title.replace(/\s+/g, '-').toLowerCase(),
      title,
      moduleNames,
      menus
    };
  });
}

export function findMenuDef(menuKey: string): RoleMenuDef | null {
  const key = normalizeMenuKey(menuKey);
  return MENUS.find(m => m.menuKey === key) || null;
}

export const ALL_ROLE_MENUS = MENUS;
