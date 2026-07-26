import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { Subscription, timer } from 'rxjs';

import { DashboardService } from '../dashboard.service';
import { DashboardAlert, DashboardWidgetDef } from '../dashboard.model';

/** Surfaces operational exceptions that need attention now (overdue collection, approvals, rejections). */
@Component({
  selector: 'app-dashboard-alerts-widget',
  templateUrl: './alerts-widget.component.html',
  styleUrls: ['./widget.css']
})
export class DashboardAlertsWidgetComponent implements OnInit, OnDestroy {
  @Input() widget: DashboardWidgetDef;

  alerts: DashboardAlert[] = [];
  loading = true;
  loadError = '';

  private subscription: Subscription;

  constructor(private dashboardService: DashboardService) { }

  ngOnInit(): void {
    const interval = this.widget?.refreshIntervalMs || 0;
    this.subscription = interval > 0
      ? timer(0, interval).subscribe(() => this.load())
      : timer(0).subscribe(() => this.load());
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  refresh(): void {
    this.load();
  }

  trackByKey(index: number, alert: DashboardAlert): string {
    return alert.key;
  }

  severityClass(alert: DashboardAlert): string {
    return `zl-alert zl-alert-${alert?.severity || 'info'}`;
  }

  private load(): void {
    this.loading = true;
    this.loadError = '';
    this.dashboardService.getAlerts(this.widget.endpoint).subscribe(
      alerts => {
        this.alerts = alerts;
        this.loading = false;
      },
      () => {
        this.loadError = 'Unable to load alerts.';
        this.alerts = [];
        this.loading = false;
      }
    );
  }
}
