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
  loadingData = true;
  echartsInstance: any;
  options: any;

  private readonly statusLabels: Record<string, string> = {
    New: 'New',
    SentToEquipment: 'Sent To Equipment',
    ReportGenerated: 'Report Generated',
    TechnicianApproved: 'Technician Approved',
    TechnicianRejected: 'Technician Rejected',
    DoctorApproved: 'Doctor Approved',
    DoctorRejected: 'Doctor Rejected',
    FinallyRejected: 'Finally Rejected'
  };

  private readonly chartColors = ['#36b1c1', '#1a7f8c', '#c9a84a', '#846a1d', '#2e9d6b', '#6b7c85'];

  constructor(private equipmentService: EquipmentService) { }

  ngOnInit(): void {
    this.options = this.buildBaseOptions();
    this.getDailyStatus();
  }

  private buildBaseOptions(): any {
    return {
      color: this.chartColors,
      title: {
        text: '',
        subtext: '',
        left: 'center',
        top: 4,
        textStyle: {
          fontSize: 15,
          fontWeight: 600,
          color: '#155f69'
        },
        subtextStyle: {
          fontSize: 12,
          color: '#6b7c85'
        }
      },
      tooltip: {
        trigger: 'item',
        formatter: '{b}: {c} ({d}%)'
      },
      legend: {
        type: 'plain',
        orient: 'horizontal',
        bottom: 6,
        left: 'center',
        width: '94%',
        itemGap: 16,
        itemWidth: 12,
        itemHeight: 12,
        textStyle: {
          fontSize: 11,
          color: '#2d3a42',
          lineHeight: 16
        },
        data: []
      },
      series: [
        {
          name: 'Status',
          type: 'pie',
          radius: ['40%', '62%'],
          center: ['50%', '44%'],
          avoidLabelOverlap: true,
          itemStyle: {
            borderRadius: 4,
            borderColor: '#fff',
            borderWidth: 2
          },
          label: {
            show: false
          },
          labelLine: {
            show: false
          },
          emphasis: {
            label: {
              show: true,
              fontSize: 12,
              fontWeight: 600,
              formatter: '{b}\n{d}%'
            },
            labelLine: {
              show: true,
              length: 10,
              length2: 8
            }
          },
          data: []
        }
      ]
    };
  }

  onChartInit(ec: any): void {
    this.echartsInstance = ec;
  }

  getDailyStatus(): void {
    this.loadingData = true;
    this.equipmentService.getEquipmentDailyStatus(this.type)
      .subscribe(response => {
        const data = this.normalizeChartData(response || []);
        this.options = {
          ...this.buildBaseOptions(),
          title: {
            ...this.buildBaseOptions().title,
            text: this.title,
            subtext: this.subtitle
          },
          legend: {
            ...this.buildBaseOptions().legend,
            data: data.map(d => d.name)
          },
          series: [
            {
              ...this.buildBaseOptions().series[0],
              data
            }
          ]
        };
        this.loadingData = false;
        if (this.echartsInstance) {
          this.echartsInstance.setOption(this.options, true);
        }
      }, () => {
        this.loadingData = false;
      });
  }

  private normalizeChartData(response: any[]): { name: string; value: number }[] {
    const totals = new Map<string, number>();
    response.forEach(item => {
      const name = this.formatName(item?.name);
      if (!name) {
        return;
      }
      totals.set(name, (totals.get(name) || 0) + (item?.value || 0));
    });
    return Array.from(totals.entries()).map(([name, value]) => ({ name, value }));
  }

  formatName(text: string): string {
    if (!text) {
      return '';
    }
    const trimmed = text.trim();
    if (this.statusLabels[trimmed]) {
      return this.statusLabels[trimmed];
    }
    const compact = trimmed.replace(/\s+/g, '');
    const match = Object.keys(this.statusLabels).find(
      key => key.toLowerCase() === compact.toLowerCase()
    );
    if (match) {
      return this.statusLabels[match];
    }
    return trimmed
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/_/g, ' ')
      .replace(/\s+/g, ' ')
      .trim()
      .replace(/\b\w/g, c => c.toUpperCase());
  }
}
