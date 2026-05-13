import { Component, Input, OnInit } from '@angular/core';
import { EquipmentService } from '../../../_services';

@Component({
  selector: 'app-status-chart',
  templateUrl: './status-chart.component.html',
  styleUrls: ['./status-chart.component.css']
})

export class StatusChartComponent implements OnInit {

  @Input() type: number;
  @Input() title: string;
  @Input() subtitle: string;
  loadingData:boolean=true;
  echartsInstance:any;
  constructor(
    private equipmentService: EquipmentService) { }


  ngOnInit(): void {
    this.getDailyStatus();
  }

  theme: string;
  options = {
    title: {
      text: '',
      subtext: '',
      x: 'center'
    },
    tooltip: {
      trigger: 'item',
      formatter: '{b} : {c} ({d}%)'
    },
    legend: {
      x: 'center',
      y: 'bottom',
      data: []
    },
    calculable: true,
    series: [
      {
        name: 'area',
        type: 'pie',
        radius: [30, 110],
        roseType: 'area',
        data: [
          
        ]
      }
    ]
  };

  onChartInit(ec) {
    this.echartsInstance = ec;
    this.loadingData = true;
  }

  getDailyStatus(){
    this.loadingData = true;
    this.equipmentService.getEquipmentDailyStatus(this.type)
      .subscribe(response => {
        this.options.series[0].data = [];
        this.options.legend.data = [];
        this.options.title.text = this.title;
        this.options.title.subtext = this.subtitle;
        response.forEach(d=>{
          this.options.series[0].data.push({ value: d.value, name: this.formatName(d.name) });
          this.options.legend.data.push(this.formatName(d.name));
        });
        this.loadingData = false;
        if (this.echartsInstance) {
          this.echartsInstance.setOption(this.options);
        }
      });
  }

  formatName(text:string){
    return text.replace(/([A-Z])/g, ' $1')
    // uppercase the first character
    .replace(/^./, function(str){ return str.toUpperCase(); })
  }
}