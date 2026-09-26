import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MasterService } from '../_services/master.service';
import { AlertService } from '../_services/alert.service';
import { CommissionPayoutService } from '../_services/commission-payout.service';

@Component({
  selector: 'app-commission-payout',
  templateUrl: './commission-payout.component.html'
})
export class CommissionPayoutComponent implements OnInit {
  recipientType = 1;
  recipientId: number = null;
  doctors: Array<{ id: number; name: string }> = [];
  collectors: Array<{ id: number; name: string }> = [];
  fromDate = '';
  toDate = '';
  isFirst = false;
  preview: any = null;
  history: any[] = [];
  loading = false;

  constructor(
    private payouts: CommissionPayoutService,
    private masterService: MasterService,
    private alertService: AlertService,
    private router: Router
  ) { }

  ngOnInit() {
    this.loadPeople('ReferralDoctor', rows => this.doctors = rows);
    this.loadPeople('SampleCollector', rows => this.collectors = rows);
    this.payouts.list().subscribe(
      rows => this.history = rows || [],
      err => this.alertService.error(this.readError(err))
    );
  }

  get people(): Array<{ id: number; name: string }> {
    return this.recipientType === 2 ? this.collectors : this.doctors;
  }

  onTypeChange(): void {
    this.recipientId = null;
    this.preview = null;
    this.fromDate = '';
    this.isFirst = false;
  }

  onRecipientChange(): void {
    this.preview = null;
    this.fromDate = '';
    if (!this.recipientId) {
      return;
    }
    this.payouts.schedule(this.recipientType, this.recipientId).subscribe(
      s => {
        this.fromDate = this.dateOnly(s.fromDate || s.FromDate);
        this.isFirst = !!(s.isFirstPayout || s.IsFirstPayout);
      },
      err => this.alertService.error(this.readError(err))
    );
  }

  runPreview(): void {
    if (!this.ready()) { return; }
    this.loading = true;
    this.payouts.preview(this.body()).subscribe(
      result => {
        this.loading = false;
        this.preview = this.normalize(result);
        if (!this.preview.lines.length) {
          this.alertService.error('No eligible invoices.');
        }
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err));
      }
    );
  }

  confirm(): void {
    if (!this.preview || !this.preview.lines.length) {
      this.alertService.error('No eligible invoices for this payout.');
      return;
    }
    this.loading = true;
    this.payouts.confirm(this.body()).subscribe(
      id => {
        this.loading = false;
        this.alertService.success('Payout confirmed');
        this.preview = null;
        this.router.navigate(['/commission-payouts', id, 'memo']);
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err));
      }
    );
  }

  private ready(): boolean {
    if (!this.recipientId) {
      this.alertService.error('Select a recipient.');
      return false;
    }
    if (!this.fromDate || !this.toDate) {
      this.alertService.error('From date and To date are required.');
      return false;
    }
    if (this.fromDate > this.toDate) {
      this.alertService.error('From date cannot be after To date.');
      return false;
    }
    return true;
  }

  private body() {
    return {
      recipientType: this.recipientType,
      recipientId: this.recipientId,
      fromDate: this.fromDate,
      toDate: this.toDate
    };
  }

  private normalize(result: any) {
    const lines = result?.lines || result?.Lines || [];
    return {
      fromDate: this.dateOnly(result?.fromDate || result?.FromDate),
      toDate: this.dateOnly(result?.toDate || result?.ToDate),
      totalAmount: result?.totalAmount ?? result?.TotalAmount ?? 0,
      lines: lines.map((l: any) => ({
        invoiceNo: l.invoiceNo || l.InvoiceNo,
        invoiceDate: l.invoiceDate || l.InvoiceDate,
        commissionBase: l.commissionBase ?? l.CommissionBase,
        commissionPercent: l.commissionPercent ?? l.CommissionPercent,
        amount: l.amount ?? l.Amount
      }))
    };
  }

  private loadPeople(lookup: string, assign: (rows: Array<{ id: number; name: string }>) => void): void {
    this.masterService.getLookupList(lookup).subscribe(list => {
      const items = Array.isArray(list) ? list : [];
      assign(items
        .filter((x: any) => x.isActive !== false && x.IsActive !== false)
        .map((x: any) => ({ id: +(x.id ?? x.Id), name: (x.name ?? x.Name ?? '').trim() }))
        .filter(x => x.id > 0 && x.name));
    });
  }

  private dateOnly(value: any): string {
    if (!value) { return ''; }
    return String(value).substring(0, 10);
  }

  private readError(err: any): string {
    if (!err) { return 'Commission payout failed'; }
    if (typeof err.error === 'string') { return err.error; }
    return err.error?.Message || err.error?.message || err.message || 'Commission payout failed';
  }
}
