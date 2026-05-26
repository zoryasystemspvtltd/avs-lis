import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AlertService, MasterService } from '../../_services';

@Component({
  selector: 'app-sale-invoice-form',
  templateUrl: './sale-invoice-form.component.html',
  styleUrls: ['./sale-invoice-form.component.css']
})
export class SaleInvoiceFormComponent implements OnInit, OnDestroy {
  form: FormGroup;
  submitted = false;
  loading = false;
  saving = false;
  id: string;
  tests: any[] = [];
  patients: any[] = [];
  corporates: any[] = [];
  doctors: any[] = [];
  isPrintView = false;
  invoiceDto: any;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private masterService: MasterService,
    private alertService: AlertService) { }

  ngOnInit() {
    this.id = this.route.snapshot.params['id'];
    this.isPrintView = this.route.snapshot.url.some(s => s.path === 'print');
    if (this.isPrintView) {
      document.body.classList.add('sale-invoice-print-mode');
    }

    this.form = this.fb.group({
      id: [0],
      invoiceNo: [''],
      invoiceDate: [new Date().toISOString().substring(0, 10), Validators.required],
      patientId: [null, Validators.required],
      invoiceStatus: [0],
      paymentStatus: [0],
      grossAmount: [0],
      discountAmount: [0],
      taxAmount: [0],
      netAmount: [0],
      paidAmount: [0],
      dueAmount: [0],
      refDoctorName: [''],
      corporateId: [null],
      referralDoctorId: [null],
      notes: [''],
      lines: this.fb.array([])
    });

    this.loadBillableTests();
    this.masterService.getAll('Corporate').subscribe(c => {
      this.corporates = (c || []).filter(x => x.isActive !== false && x.IsActive !== false);
    });
    this.masterService.getAll('ReferralDoctor').subscribe(d => {
      this.doctors = (d || []).filter(x => x.isActive !== false && x.IsActive !== false);
    });
    this.masterService.getBillingPatients({
      RecordPerPage: 500, CurrentPage: 1, SearchText: '', SortColumnName: 'Name', SortDirection: false, Status: 0
    }).subscribe(p => {
      this.patients = p?.items || [];
    });

    if (this.id) {
      this.loadInvoice(+this.id);
    } else {
      this.masterService.getNextInvoiceNo().subscribe(no => this.form.patchValue({ invoiceNo: no }));
      this.addLine();
    }
  }

  get isCancelled(): boolean { return this.form?.value?.invoiceStatus === 3; }
  get isPaid(): boolean { return this.form?.value?.invoiceStatus === 2; }

  loadInvoice(invoiceId: number) {
    this.masterService.getInvoice(invoiceId).subscribe(dto => {
      this.invoiceDto = dto;
      if (dto?.invoice) {
        const inv = dto.invoice;
        inv.invoiceDate = inv.invoiceDate ? inv.invoiceDate.substring(0, 10) : '';
        this.form.patchValue(inv);
        this.lines.clear();
        (dto.details || []).forEach(line => this.addLine(line));
        if (this.isCancelled || this.isPaid) {
          this.form.disable();
        }
      }
    });
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }

  addLine(line?: any) {
    this.lines.push(this.fb.group({
      id: [line?.id || 0],
      testId: [line?.testId || '', Validators.required],
      rate: [line?.rate || 0],
      quantity: [line?.quantity || 1],
      amount: [line?.amount || 0],
      discountAmount: [line?.discountAmount || 0],
      taxAmount: [line?.taxAmount || 0],
      netAmount: [line?.netAmount || 0],
      sampleNo: [line?.sampleNo || '']
    }));
  }

  removeLine(i: number) {
    this.lines.removeAt(i);
    this.recalc();
  }

  getInvoiceDate(): string {
    const v = this.form.getRawValue ? this.form.getRawValue() : this.form.value;
    return v.invoiceDate || new Date().toISOString().substring(0, 10);
  }

  getTestName(testId: number): string {
    const t = this.tests.find(x => +x.id === +testId);
    return t ? `${t.hisTestCode} - ${t.hisTestCodeDescription}` : String(testId);
  }

  getCorporateName(corporateId: number | null | undefined): string {
    if (!corporateId) { return '—'; }
    const c = this.corporates.find(x => +x.id === +corporateId);
    return c?.name || '—';
  }

  getDoctorName(referralDoctorId: number | null | undefined, refDoctorName?: string): string {
    if (referralDoctorId) {
      const d = this.doctors.find(x => +x.id === +referralDoctorId);
      if (d?.name) { return d.name; }
    }
    return refDoctorName?.trim() || '—';
  }

  ngOnDestroy() {
    document.body.classList.remove('sale-invoice-print-mode');
  }

  onTestChange(i: number) {
    const line = this.lines.at(i);
    const testId = line.get('testId').value;
    if (!testId) { return; }

    const test = this.tests.find(t => +t.id === +testId);
    if (!test) {
      this.alertService.error('Selected test is inactive or unavailable');
      line.patchValue({ testId: '' });
      return;
    }

    const duplicate = this.lines.controls.some((c, idx) => idx !== i && +c.value.testId === +testId);
    if (duplicate) {
      this.alertService.error('Test already added to invoice');
      line.patchValue({ testId: '' });
      return;
    }

    this.loadLineRate(i, +testId);
  }

  private loadLineRate(i: number, testId: number) {
    const line = this.lines.at(i);
    const v = this.form.getRawValue();
    this.masterService.getEffectiveRateForInvoice(
      testId,
      this.getInvoiceDate(),
      v.corporateId || null,
      v.referralDoctorId || null
    ).subscribe(rate => {
      if (!rate || rate.rate == null) {
        this.alertService.error('No active rate found for this test on the invoice date.');
        line.patchValue({ testId: '', rate: 0, taxAmount: 0, discountAmount: 0, amount: 0, netAmount: 0 });
        this.recalc();
        return;
      }

      const qty = line.value.quantity || 1;
      const amount = (rate.rate || 0) * qty;
      const tax = rate.taxPercent ? Math.round(amount * rate.taxPercent) / 100 : 0;
      const disc = rate.discountPercent ? Math.round(amount * rate.discountPercent) / 100 : 0;
      line.patchValue({
        rate: rate.rate,
        taxAmount: tax,
        discountAmount: disc
      });
      this.recalcLine(i);
    });
  }

  onInvoiceDateChange() {
    this.loadBillableTests();
    this.onRateContextChange();
  }

  private loadBillableTests() {
    const invoiceDate = this.getInvoiceDate();
    this.masterService.getLookupList('HisTest').subscribe(allTests => {
      const activeTests = (allTests || []).filter(x => x.isActive !== false && x.IsActive !== false);
      this.masterService.getItems('TestRate', {
        RecordPerPage: 5000,
        CurrentPage: 1,
        SortColumnName: 'EffectiveStart',
        SortDirection: false
      }).subscribe(rateResponse => {
        const rates = rateResponse?.items || rateResponse?.Items || [];
        const asOf = new Date(invoiceDate);
        asOf.setHours(0, 0, 0, 0);
        const testIdsWithRate = new Set<number>();
        rates.forEach((r: any) => {
          if (r.isActive === false || r.IsActive === false) {
            return;
          }
          const from = new Date(r.effectiveStart || r.EffectiveStart);
          const to = new Date(r.effectiveEnd || r.EffectiveEnd);
          from.setHours(0, 0, 0, 0);
          to.setHours(23, 59, 59, 999);
          if (asOf >= from && asOf <= to) {
            testIdsWithRate.add(+(r.testId || r.TestId));
          }
        });
        this.tests = activeTests.filter(t => testIdsWithRate.has(+t.id));
      }, () => {
        this.tests = [];
      });
    });
  }

  onRateContextChange() {
    this.lines.controls.forEach((_, i) => {
      const testId = this.lines.at(i).get('testId').value;
      if (testId) {
        this.loadLineRate(i, +testId);
      }
    });
  }

  private validatePatient(): boolean {
    const patientId = this.form.get('patientId')?.value;
    if (!patientId || +patientId <= 0) {
      this.alertService.error('Please select a patient');
      return false;
    }
    return true;
  }

  recalcLine(i: number) {
    const line = this.lines.at(i).value;
    const amount = (line.rate || 0) * (line.quantity || 1);
    const net = amount - (line.discountAmount || 0) + (line.taxAmount || 0);
    this.lines.at(i).patchValue({ amount, netAmount: net }, { emitEvent: false });
    this.recalc();
  }

  recalc() {
    let gross = 0, disc = 0, tax = 0, net = 0;
    this.lines.controls.forEach(c => {
      const v = c.value;
      gross += v.amount || 0;
      disc += v.discountAmount || 0;
      tax += v.taxAmount || 0;
      net += v.netAmount || 0;
    });
    const paid = this.form.getRawValue().paidAmount || 0;
    this.form.patchValue({
      grossAmount: gross,
      discountAmount: disc,
      taxAmount: tax,
      netAmount: net,
      dueAmount: net - paid
    }, { emitEvent: false });
  }

  private readApiError(err: any): string {
    if (!err) { return 'Save failed'; }
    if (typeof err === 'string') { return err; }
    if (typeof err.message === 'string') { return err.message; }
    if (typeof err.error === 'string') { return err.error; }
    if (err.error?.message) { return err.error.message; }
    return 'Save failed';
  }

  onSubmit(confirm = false) {
    this.submitted = true;
    if (this.saving) { return; }

    if (!this.validatePatient()) {
      return;
    }

    if (this.form.invalid) {
      if (!this.form.get('patientId')?.valid) {
        this.alertService.error('Please select a patient');
      } else if (!this.form.get('invoiceDate')?.valid) {
        this.alertService.error('Invoice date is required');
      } else {
        this.alertService.error('Please select a test on each line');
      }
      return;
    }

    const val = this.form.getRawValue();
    const lineItems = (val.lines || [])
      .filter(l => l.testId)
      .map(l => ({
        id: l.id || 0,
        testId: +l.testId,
        rate: +l.rate,
        quantity: +l.quantity,
        amount: +l.amount,
        discountAmount: +l.discountAmount,
        taxAmount: +l.taxAmount,
        netAmount: +l.netAmount,
        sampleNo: l.sampleNo
      }));

    if (!lineItems.length) {
      this.alertService.error('Add at least one test line');
      return;
    }

    const dto = {
      invoice: Object.assign({}, val, {
        id: val.id || (this.id ? +this.id : 0),
        invoiceDate: new Date(val.invoiceDate),
        invoiceStatus: confirm ? 1 : (val.invoiceStatus || 0),
        patientId: +val.patientId
      }),
      details: lineItems
    };

    this.loading = true;
    this.saving = true;
    this.masterService.saveInvoice(dto).subscribe(
      data => {
        this.loading = false;
        this.saving = false;
        this.alertService.success(confirm ? 'Invoice confirmed' : 'Invoice saved');
        const newId = data?.result ?? data?.Result ?? val.id;
        if (newId) {
          this.router.navigate(['/sale-invoices', newId]);
        } else {
          this.router.navigate(['/sale-invoices']);
        }
      },
      err => {
        this.loading = false;
        this.saving = false;
        this.alertService.error(this.readApiError(err));
      }
    );
  }

  markPaid() {
    const id = this.form.getRawValue().id;
    if (!id || this.saving) { return; }
    this.loading = true;
    this.masterService.updateInvoiceStatus(id, 2, 2).subscribe(
      () => {
        this.loading = false;
        this.alertService.success('Marked as paid');
        this.loadInvoice(id);
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readApiError(err) || 'Update failed');
      }
    );
  }

  cancelInvoice() {
    const id = this.form.getRawValue().id;
    if (!id || !confirm('Cancel this invoice?')) { return; }
    this.loading = true;
    this.masterService.cancelInvoice(id).subscribe(
      () => {
        this.loading = false;
        this.alertService.success('Invoice cancelled');
        this.router.navigate(['/sale-invoices']);
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readApiError(err) || 'Cancel failed');
      }
    );
  }

  print() {
    const prevTitle = document.title;
    document.title = '\u00A0';
    window.print();
    setTimeout(() => { document.title = prevTitle; }, 500);
  }
  back() { this.router.navigate(['/sale-invoices']); }
}
