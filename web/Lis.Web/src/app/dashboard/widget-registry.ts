import { AuthenticationToken } from '../_models';
import { hasAnyModuleAccess } from '../_guards/permission.util';
import { DashboardWidgetDef } from './dashboard.model';

/** CanView — the lowest bit that makes a read-only widget meaningful. */
const CAN_VIEW = 32;

const MINUTE = 60000;

/**
 * The single source of truth for dashboard content. Adding a widget here is the
 * only change required to surface it; visibility is derived from the module
 * permissions a role already holds, so new roles work without code changes.
 */
export const DASHBOARD_WIDGETS: DashboardWidgetDef[] = [
  {
    key: 'alerts',
    title: 'Operational Alerts',
    kind: 'alerts',
    endpoint: 'api/Dashboard/Alerts',
    modules: ['Samples', 'Reports'],
    access: CAN_VIEW,
    order: 10,
    colClass: 'col-md-12',
    refreshIntervalMs: 2 * MINUTE,
    enabled: true
  },
  {
    key: 'kpi.registration',
    title: 'Patient Registration',
    subtitle: 'Today',
    kind: 'kpi',
    endpoint: 'api/Dashboard/Registration',
    modules: ['PatientDetails', 'Masters'],
    access: CAN_VIEW,
    order: 20,
    colClass: 'col-md-4',
    refreshIntervalMs: 2 * MINUTE,
    enabled: true,
    linkRoute: '/patient-master',
    linkLabel: 'Patients'
  },
  {
    key: 'kpi.billing',
    title: 'Billing',
    subtitle: 'Today',
    kind: 'kpi',
    endpoint: 'api/Dashboard/Billing',
    modules: ['SaleInvoices'],
    access: CAN_VIEW,
    order: 30,
    colClass: 'col-md-8',
    refreshIntervalMs: 2 * MINUTE,
    enabled: true,
    linkRoute: '/sale-invoices',
    linkLabel: 'Invoices'
  },
  {
    key: 'kpi.collection',
    title: 'Sample Collection',
    kind: 'kpi',
    endpoint: 'api/Dashboard/Collection',
    modules: ['SampleCollection', 'Samples'],
    access: CAN_VIEW,
    order: 40,
    colClass: 'col-md-4',
    refreshIntervalMs: 2 * MINUTE,
    enabled: true,
    linkRoute: '/sample-collection',
    linkLabel: 'Collect'
  },
  {
    key: 'kpi.receiving',
    title: 'Sample Receiving',
    kind: 'kpi',
    endpoint: 'api/Dashboard/Receiving',
    modules: ['SampleReceiving', 'Samples'],
    access: CAN_VIEW,
    order: 50,
    colClass: 'col-md-4',
    refreshIntervalMs: 2 * MINUTE,
    enabled: true,
    linkRoute: '/sample-receiving',
    linkLabel: 'Receive'
  },
  {
    key: 'kpi.radiology',
    title: 'Radiology',
    kind: 'kpi',
    endpoint: 'api/Dashboard/Radiology',
    modules: ['RadiologyReports', 'RadiologyDoctorApprovals'],
    access: CAN_VIEW,
    order: 60,
    colClass: 'col-md-4',
    refreshIntervalMs: 2 * MINUTE,
    enabled: true,
    linkRoute: '/radiology-report-entry',
    linkLabel: 'Radiology'
  },
  {
    key: 'kpi.laboratory',
    title: 'Laboratory Workload',
    kind: 'kpi',
    endpoint: 'api/Dashboard/Laboratory',
    modules: ['Reports', 'Samples'],
    access: CAN_VIEW,
    order: 70,
    colClass: 'col-md-6',
    refreshIntervalMs: 2 * MINUTE,
    enabled: true,
    linkRoute: '/technicianapprovals',
    linkLabel: 'Approvals'
  },
  {
    key: 'kpi.doctorApproval',
    title: 'Doctor Approval',
    kind: 'kpi',
    endpoint: 'api/Dashboard/DoctorApproval',
    modules: ['DoctorsApprovals'],
    access: CAN_VIEW,
    order: 80,
    colClass: 'col-md-6',
    refreshIntervalMs: 2 * MINUTE,
    enabled: true,
    linkRoute: '/doctorapprovals',
    linkLabel: 'Approvals'
  },
  {
    key: 'queue.pendingCollection',
    title: 'Pending Sample Collection',
    kind: 'queue',
    endpoint: 'api/Dashboard/PendingCollectionQueue',
    modules: ['SampleCollection', 'Samples', 'Reports'],
    access: CAN_VIEW,
    order: 90,
    colClass: 'col-md-6',
    refreshIntervalMs: 3 * MINUTE,
    enabled: true,
    linkRoute: '/reports/pending-collection',
    linkLabel: 'View all',
    columns: [
      { field: 'reference', label: 'Sample' },
      { field: 'patientName', label: 'Patient' },
      { field: 'description', label: 'Test' },
      { field: 'orderedOn', label: 'Ordered', isDate: true }
    ]
  },
  {
    key: 'queue.pendingRadiology',
    title: 'Pending Radiology',
    kind: 'queue',
    endpoint: 'api/Dashboard/PendingRadiologyQueue',
    modules: ['RadiologyReports', 'RadiologyDoctorApprovals'],
    access: CAN_VIEW,
    order: 100,
    colClass: 'col-md-6',
    refreshIntervalMs: 3 * MINUTE,
    enabled: true,
    linkRoute: '/reports/radiology/pending',
    linkLabel: 'View all',
    columns: [
      { field: 'reference', label: 'Accession' },
      { field: 'patientName', label: 'Patient' },
      { field: 'description', label: 'Modality' },
      { field: 'orderedOn', label: 'Ordered', isDate: true }
    ]
  },
  {
    key: 'chart.sample',
    title: 'Daily sample status',
    kind: 'chart',
    chartType: 0,
    endpoint: 'api/DailyStatus/0',
    modules: ['Samples', 'Reports'],
    access: CAN_VIEW,
    order: 110,
    colClass: 'col-md-4',
    refreshIntervalMs: 0,
    enabled: true
  },
  {
    key: 'chart.technician',
    title: 'Technician daily report status',
    kind: 'chart',
    chartType: 1,
    endpoint: 'api/DailyStatus/1',
    modules: ['Reports', 'Samples'],
    access: CAN_VIEW,
    order: 120,
    colClass: 'col-md-4',
    refreshIntervalMs: 0,
    enabled: true
  },
  {
    key: 'chart.doctor',
    title: 'Doctor daily approval status',
    kind: 'chart',
    chartType: 2,
    endpoint: 'api/DailyStatus/2',
    modules: ['DoctorsApprovals'],
    access: CAN_VIEW,
    order: 130,
    colClass: 'col-md-4',
    refreshIntervalMs: 0,
    enabled: true
  },
  {
    key: 'recent.samples',
    title: 'Recent Samples',
    kind: 'recent',
    endpoint: 'api/Patients',
    modules: ['Samples'],
    access: CAN_VIEW,
    order: 140,
    colClass: 'col-md-12',
    refreshIntervalMs: 0,
    enabled: true
  }
];

/** Widgets the given user is allowed to see, in display order. */
export function resolveVisibleWidgets(user: AuthenticationToken): DashboardWidgetDef[] {
  if (!user) {
    return [];
  }
  return DASHBOARD_WIDGETS
    .filter(widget => widget.enabled)
    .filter(widget => hasAnyModuleAccess(user, widget.modules, widget.access))
    .sort((a, b) => a.order - b.order);
}
