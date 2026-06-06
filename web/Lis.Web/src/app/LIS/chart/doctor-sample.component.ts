import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-doctor-sample-chart',
  template: '<app-status-chart [type]="type" [title]="title" [subtitle]="subtitle"></app-status-chart>'
})
export class DoctorSampleChartComponent {
  type: number = 2;
  title: string = 'Doctor\'s - Daily report status';
  subtitle: string = 'Approval vs Rejection'
}