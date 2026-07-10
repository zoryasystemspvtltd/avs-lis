import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-radiology-report-header',
  templateUrl: './radiology-report-header.component.html',
  styleUrls: ['./radiology-report-header.component.css']
})
export class RadiologyReportHeaderComponent {
  @Input() reportDetail: any;

  get patientName(): string {
    return this.read('patientName', 'PatientName') || '—';
  }

  get invoiceNo(): string {
    return this.read('invoiceNo', 'InvoiceNo', 'hisRequestNo', 'HisRequestNo') || '—';
  }

  get ageGender(): string {
    const age = this.read('age', 'Age');
    const gender = this.read('gender', 'Gender');
    const ageText = age != null && age !== '' ? String(age) : '—';
    const genderText = gender || '—';
    return `${ageText} / ${genderText}`;
  }

  get resultDateLabel(): string {
    const raw = this.read('resultDate', 'ResultDate');
    if (raw == null || raw === '') {
      return '—';
    }

    const parsed = raw instanceof Date ? raw : new Date(raw);
    if (isNaN(parsed.getTime()) || parsed.getFullYear() < 1900) {
      return '—';
    }

    const day = String(parsed.getDate()).padStart(2, '0');
    const month = String(parsed.getMonth() + 1).padStart(2, '0');
    const year = parsed.getFullYear();
    const hours = String(parsed.getHours()).padStart(2, '0');
    const minutes = String(parsed.getMinutes()).padStart(2, '0');
    return `${day}/${month}/${year} ${hours}:${minutes}`;
  }

  private read(...keys: string[]): any {
    const detail = this.reportDetail;
    if (!detail) {
      return null;
    }

    for (const key of keys) {
      const value = detail[key];
      if (value != null && value !== '') {
        return value;
      }
    }

    return null;
  }
}
