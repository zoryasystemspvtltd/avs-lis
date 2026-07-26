/**
 * Contracts for the dashboard framework. A widget is described by data only —
 * the framework decides visibility from RBAC and renders by `kind`.
 */

export type DashboardWidgetKind = 'kpi' | 'alerts' | 'chart' | 'queue' | 'recent';

export interface DashboardWidgetDef {
  /** Stable identifier used for tracking and diagnostics. */
  key: string;
  title: string;
  subtitle?: string;
  kind: DashboardWidgetKind;
  /** Relative API path the widget reads. Empty when the child component owns its call. */
  endpoint: string;
  /** Visible when the user holds `access` on ANY of these modules. */
  modules: string[];
  /** Permission bitmask required (32 = CanView). */
  access: number;
  order: number;
  /** Bootstrap column class controlling widget width. */
  colClass: string;
  /** Auto-refresh period in milliseconds; 0 disables polling. */
  refreshIntervalMs: number;
  enabled: boolean;
  /** DailyStatus discriminator for chart widgets (0 = sample, 1 = technician, 2 = doctor). */
  chartType?: number;
  /** Optional deep link shown in the widget header. */
  linkRoute?: string;
  linkLabel?: string;
  /** Column definitions for queue widgets. */
  columns?: DashboardQueueColumn[];
}

export interface DashboardQueueColumn {
  /** Field on DashboardQueueItem: reference | patientName | description | orderedOn. */
  field: string;
  label: string;
  /** Renders the value as a date when true. */
  isDate?: boolean;
}

export interface DashboardQueueItem {
  reference: string;
  patientName: string;
  description: string;
  orderedOn: string;
}

export interface DashboardQueue {
  totalRecord: number;
  items: DashboardQueueItem[];
}

export interface DashboardMetric {
  key: string;
  label: string;
  value: number;
  format: string;
}

export interface DashboardAlert {
  key: string;
  severity: string;
  title: string;
  detail: string;
  count: number;
}
