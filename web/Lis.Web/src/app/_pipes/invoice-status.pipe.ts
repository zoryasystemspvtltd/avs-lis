import { Pipe, PipeTransform } from '@angular/core';

const INVOICE_STATUS_LABELS: { [key: number]: string } = {
  0: 'Draft',
  1: 'Confirmed',
  2: 'Paid',
  3: 'Cancelled'
};

const INVOICE_STATUS_CLASSES: { [key: number]: string } = {
  0: 'zorya-badge-draft',
  1: 'zorya-badge-confirmed',
  2: 'zorya-badge-paid',
  3: 'zorya-badge-cancelled'
};

const PAYMENT_STATUS_LABELS: { [key: number]: string } = {
  0: 'Unpaid',
  1: 'Partial',
  2: 'Paid'
};

const PAYMENT_STATUS_CLASSES: { [key: number]: string } = {
  0: 'zorya-badge-payment-unpaid',
  1: 'zorya-badge-payment-partial',
  2: 'zorya-badge-payment-paid'
};

@Pipe({ name: 'invoiceStatusLabel' })
export class InvoiceStatusLabelPipe implements PipeTransform {
  transform(value: number): string {
    return INVOICE_STATUS_LABELS[value] != null ? INVOICE_STATUS_LABELS[value] : String(value);
  }
}

@Pipe({ name: 'invoiceStatusClass' })
export class InvoiceStatusClassPipe implements PipeTransform {
  transform(value: number): string {
    return INVOICE_STATUS_CLASSES[value] || 'zorya-badge-default';
  }
}

@Pipe({ name: 'paymentStatusLabel' })
export class PaymentStatusLabelPipe implements PipeTransform {
  transform(value: number): string {
    return PAYMENT_STATUS_LABELS[value] != null ? PAYMENT_STATUS_LABELS[value] : String(value);
  }
}

@Pipe({ name: 'paymentStatusClass' })
export class PaymentStatusClassPipe implements PipeTransform {
  transform(value: number): string {
    return PAYMENT_STATUS_CLASSES[value] || 'zorya-badge-default';
  }
}
