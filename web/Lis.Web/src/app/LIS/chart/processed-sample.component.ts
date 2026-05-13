import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-processed-sample-chart',
  template: '<app-status-chart [type]="type" [title]="title" [subtitle]="subtitle"></app-status-chart>'
})
export class ProcessedSampleChartComponent {
  type: number = 1;
  title: string = 'Technician\'s - Daily report status';
  subtitle: string = 'Approval vs Rejection'
}