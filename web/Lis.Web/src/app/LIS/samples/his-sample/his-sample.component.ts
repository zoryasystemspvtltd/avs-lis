import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { ModuleService } from '../../../_services/modules.service';

@Component({
  selector: 'app-his-sample',
  templateUrl: './his-sample.component.html',
  styleUrls: ['./his-sample.component.css']
})
export class HisSampleComponent implements OnInit, OnDestroy {
  @Output() onGetOrder = new EventEmitter<boolean>();
  @Input() hideCreateButton = false;

  recentSamples: any[] = [];
  loading = true;
  loadError = '';

  private readonly listOption = {
    RecordPerPage: 8,
    CurrentPage: 1,
    SortColumnName: 'sampleCollectionDate',
    SortDirection: false,
    SearchText: '',
    ReceivedOnly: true
  };

  constructor(
    private moduleService: ModuleService
  ) { }

  ngOnInit(): void {
    this.loadRecentSamples();
  }

  ngOnDestroy(): void { }

  loadRecentSamples(): void {
    this.loading = true;
    this.loadError = '';
    this.moduleService.getItems('Patients', this.listOption).subscribe(
      response => {
        this.recentSamples = response?.items || [];
        this.loading = false;
      },
      () => {
        this.loadError = 'Unable to load recent samples.';
        this.recentSamples = [];
        this.loading = false;
      }
    );
  }

  refresh(): void {
    this.loadRecentSamples();
    this.onGetOrder.emit(true);
  }

  patientName(sample: any): string {
    const patient = sample?.patient;
    if (!patient) {
      return '—';
    }
    return patient.name || patient.Name || '—';
  }

  testName(sample: any): string {
    return sample?.hisTestName || sample?.testName || '—';
  }
}
