import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-raw-sample-chart',
  template: '<app-status-chart [type]="type" [title]="title" [subtitle]="subtitle"></app-status-chart>'
})

export class RawSampleChartComponent {
  type: number = 0;
  title: string = 'Daily sample status';
  subtitle: string = 'New vs Processing'
}