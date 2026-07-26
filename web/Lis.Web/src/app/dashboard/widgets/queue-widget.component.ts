import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { Subscription, timer } from 'rxjs';

import { DashboardService } from '../dashboard.service';
import { DashboardQueueColumn, DashboardQueueItem, DashboardWidgetDef } from '../dashboard.model';

/** Rows shown inline before the user is sent to the full report. */
const QUEUE_PAGE_SIZE = 5;

/** Shows the head of a pending-work queue backed by an existing operational report API. */
@Component({
  selector: 'app-dashboard-queue-widget',
  templateUrl: './queue-widget.component.html',
  styleUrls: ['./widget.css']
})
export class DashboardQueueWidgetComponent implements OnInit, OnDestroy {
  @Input() widget: DashboardWidgetDef;

  rows: DashboardQueueItem[] = [];
  totalRecord = 0;
  loading = true;
  loadError = '';

  private subscription: Subscription;

  constructor(private dashboardService: DashboardService) { }

  get columns(): DashboardQueueColumn[] {
    return this.widget?.columns || [];
  }

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

  cellValue(row: DashboardQueueItem, column: DashboardQueueColumn): string {
    const value = row ? row[column.field] : null;
    if (value === null || value === undefined || value === '') {
      return '—';
    }
    return value;
  }

  private load(): void {
    this.loading = true;
    this.loadError = '';
    this.dashboardService.getQueue(this.widget.endpoint, QUEUE_PAGE_SIZE).subscribe(
      response => {
        this.rows = response.items;
        this.totalRecord = response.totalRecord;
        this.loading = false;
      },
      () => {
        this.loadError = 'Unable to load this queue.';
        this.rows = [];
        this.totalRecord = 0;
        this.loading = false;
      }
    );
  }
}
