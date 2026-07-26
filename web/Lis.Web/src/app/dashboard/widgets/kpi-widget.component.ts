import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { Subscription, timer } from 'rxjs';

import { DashboardService } from '../dashboard.service';
import { DashboardMetric, DashboardWidgetDef } from '../dashboard.model';

/**
 * Renders one KPI group. Every failure is contained here so a broken metric
 * endpoint never affects the rest of the dashboard.
 */
@Component({
  selector: 'app-dashboard-kpi-widget',
  templateUrl: './kpi-widget.component.html',
  styleUrls: ['./widget.css']
})
export class DashboardKpiWidgetComponent implements OnInit, OnDestroy {
  @Input() widget: DashboardWidgetDef;

  metrics: DashboardMetric[] = [];
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

  trackByKey(index: number, metric: DashboardMetric): string {
    return metric.key;
  }

  isCurrency(metric: DashboardMetric): boolean {
    return metric?.format === 'currency';
  }

  private load(): void {
    this.loading = true;
    this.loadError = '';
    this.dashboardService.getMetrics(this.widget.endpoint).subscribe(
      metrics => {
        this.metrics = metrics;
        this.loading = false;
      },
      () => {
        this.loadError = 'Unable to load this section.';
        this.metrics = [];
        this.loading = false;
      }
    );
  }
}
