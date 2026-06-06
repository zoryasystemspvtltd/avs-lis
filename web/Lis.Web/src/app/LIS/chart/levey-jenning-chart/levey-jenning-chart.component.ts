import { Input, OnInit } from '@angular/core';
import { Component } from '@angular/core';
import { QualityService } from '../../../_services';


@Component({
  selector: 'app-levey-jenning-chart',
  templateUrl: './levey-jenning-chart.component.html',
  styleUrls: ['./levey-jenning-chart.component.css']
})

export class LeveyJenningChartComponent implements OnInit {

  @Input() details: any[];
  @Input() days: number[];
  selectedData:any;
  loadingData: boolean = true;
  echartsInstance: any;
  Linechart = [];
  options = {

  }
  constructor(private qualityService: QualityService) { }

  ngOnInit() {
    this.selectedData = this.details[0].lisParamValues;
    this.selectData();
  }

  formatData(raw: any[]) {
    let data = [];
    raw.forEach(p => {
      data[p.day - 1] = p.lisParamValue;
    })

    return data;
  }

  onChartInit(ec) {
    this.echartsInstance = ec;
    this.loadingData = true;
  }


  selectData(){
    let series = [];

    series.push({
      data: this.formatData(this.selectedData),
      type: 'line'
    })

    this.options = {
      xAxis: {
        type: 'category',
        data: this.days
      },
      yAxis: {
        type: 'value'
      },
      series: series
    };
  }


}
