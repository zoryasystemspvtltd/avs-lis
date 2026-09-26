import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommissionPayoutService } from '../_services/commission-payout.service';
import { AlertService } from '../_services/alert.service';

@Component({
  selector: 'app-commission-payout-memo',
  templateUrl: './commission-payout-memo.component.html',
  styles: [`
    .memo { max-width: 800px; margin: 16px auto; }
    .signature { margin-top: 56px; width: 240px; border-top: 1px solid #222; padding-top: 6px; }
    @media print {
      .no-print { display: none !important; }
    }
  `]
})
export class CommissionPayoutMemoComponent implements OnInit {
  memo: any = null;

  constructor(
    private route: ActivatedRoute,
    private payouts: CommissionPayoutService,
    private alertService: AlertService
  ) { }

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id');
    this.payouts.memo(id).subscribe(
      result => this.memo = result,
      err => this.alertService.error(err?.error?.Message || err?.error?.message || 'Payout was not found.')
    );
  }

  get payout(): any {
    return this.memo?.payout || this.memo?.Payout || {};
  }

  get details(): any[] {
    return this.memo?.details || this.memo?.Details || [];
  }

  get labName(): string {
    return this.memo?.labName || this.memo?.LabName || '';
  }

  print(): void {
    const prev = document.title;
    document.title = '\u00A0';
    window.print();
    setTimeout(() => { document.title = prev; }, 500);
  }
}
