import { Component, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AlertService, MasterService } from '../../_services';

@Component({
  selector: 'app-sale-invoice-form',
  templateUrl: './sale-invoice-form.component.html',
  styleUrls: ['./sale-invoice-form.component.css']
})
export class SaleInvoiceFormComponent implements OnInit {
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

    this.form = this.fb.group({
      id: [0],
      invoiceNo: [''],
      invoiceDate: [new Date().toISOString().substring(0, 10), Validators.required],
      patientId: ['', Validators.required],
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

    this.masterService.getAll('HisTest').subscribe(t => this.tests = t || []);
    this.masterService.getAll('Corporate').subscribe(c => this.corporates = c || []);
    this.masterService.getAll('ReferralDoctor').subscribe(d => this.doctors = d || []);
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

  getRateType(): number {
    const v = this.form.getRawValue ? this.form.getRawValue() : this.form.value;
    if (v.corporateId) { return 1; }
    if (v.referralDoctorId) { return 2; }
    return 0;
  }

  getTestName(testId: number): string {
    const t = this.tests.find(x => +x.id === +testId);
    return t ? `${t.hisTestCode} - ${t.hisTestCodeDescription}` : String(testId);
  }

  onTestChange(i: number) {
    const line = this.lines.at(i);
    const testId = line.get('testId').value;
    if (!testId) { return; }

    const duplicate = this.lines.controls.some((c, idx) => idx !== i && +c.value.testId === +testId);
    if (duplicate) {
      this.alertService.error('Test already added to invoice');
      line.patchValue({ testId: '' });
      return;
    }

    this.masterService.getEffectiveRate(
      +testId,
      this.getRateType(),
      this.form.value.corporateId,
      this.form.value.referralDoctorId
    ).subscribe(rate => {
      if (rate) {
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
      }
    });
  }

  onRateContextChange() {
    this.lines.controls.forEach((_, i) => {
      if (this.lines.at(i).get('testId').value) {
        this.onTestChange(i);
      }
    });
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

  onSubmit(confirm = false) {
    this.submitted = true;
    if (this.form.invalid || this.saving) { return; }

    const val = this.form.getRawValue();
    const dto = {
      invoice: Object.assign({}, val, {
        invoiceDate: new Date(val.invoiceDate),
        invoiceStatus: confirm ? 1 : (val.invoiceStatus || 0),
        patientId: +val.patientId
      }),
      details: val.lines.map(l => ({
        id: l.id || 0,
        testId: +l.testId,
        rate: +l.rate,
        quantity: +l.quantity,
        amount: +l.amount,
        discountAmount: +l.discountAmount,
        taxAmount: +l.taxAmount,
        netAmount: +l.netAmount,
        sampleNo: l.sampleNo
      }))
    };

    this.loading = true;
    this.saving = true;
    this.masterService.saveInvoice(dto).subscribe(
      data => {
        this.loading = false;
        this.saving = false;
        this.alertService.success(confirm ? 'Invoice confirmed' : 'Invoice saved');
        const newId = data?.result || val.id;
        if (newId) {
          this.router.navigate(['/sale-invoices', newId]);
        } else {
          this.router.navigate(['/sale-invoices']);
        }
      },
      err => {
        this.loading = false;
        this.saving = false;
        this.alertService.error(err?.error?.message || 'Save failed');
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
      () => {
        this.loading = false;
        this.alertService.error('Update failed');
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
      () => {
        this.loading = false;
        this.alertService.error('Cancel failed');
      }
    );
  }

  print() { window.print(); }
  back() { this.router.navigate(['/sale-invoices']); }
}
