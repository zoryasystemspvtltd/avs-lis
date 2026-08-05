import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AlertService, NotificationConfigurationService } from '../../_services';
import { extractApiError } from '../../_helpers/api-error';

@Component({
  selector: 'app-notification-configuration',
  templateUrl: './notification-configuration.component.html'
})
export class NotificationConfigurationComponent implements OnInit {
  form: FormGroup;
  loading = false;
  saving = false;
  auditLoading = false;
  templates: any[] = [];
  auditRows: any[] = [];

  readonly channelModes = [
    { value: 1, label: 'SMS' },
    { value: 2, label: 'WhatsApp' },
    { value: 3, label: 'Both' }
  ];

  readonly defaultChannels = [
    { value: 1, label: 'SMS' },
    { value: 2, label: 'WhatsApp' }
  ];

  constructor(
    private fb: FormBuilder,
    private notificationService: NotificationConfigurationService,
    private alertService: AlertService) {
    this.form = this.fb.group({
      isEnabled: [false],
      smsEnabled: [true],
      whatsAppEnabled: [true],
      channelMode: [3],
      retryCount: [3],
      retryIntervalSeconds: [30],
      defaultChannel: [1]
    });
  }

  ngOnInit(): void {
    this.loadConfiguration();
    this.loadTemplates();
    this.loadAudit();
  }

  loadConfiguration(): void {
    this.loading = true;
    this.notificationService.getConfiguration().subscribe({
      next: (config) => {
        this.form.patchValue({
          isEnabled: !!config.isEnabled,
          smsEnabled: config.smsEnabled !== false,
          whatsAppEnabled: config.whatsAppEnabled !== false,
          channelMode: config.channelMode || 3,
          retryCount: config.retryCount || 3,
          retryIntervalSeconds: config.retryIntervalSeconds || 30,
          defaultChannel: config.defaultChannel || 1
        });
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.alertService.error(extractApiError(err, 'Unable to load notification configuration.'));
      }
    });
  }

  loadTemplates(): void {
    this.notificationService.getTemplates().subscribe({
      next: (rows) => { this.templates = rows || []; },
      error: () => { this.templates = []; }
    });
  }

  loadAudit(): void {
    this.auditLoading = true;
    this.notificationService.getAudit(100).subscribe({
      next: (rows) => {
        this.auditRows = rows || [];
        this.auditLoading = false;
      },
      error: () => {
        this.auditRows = [];
        this.auditLoading = false;
      }
    });
  }

  save(): void {
    this.saving = true;
    this.notificationService.saveConfiguration(this.form.value).subscribe({
      next: () => {
        this.saving = false;
        this.alertService.success('Notification configuration saved.');
        this.loadConfiguration();
      },
      error: (err) => {
        this.saving = false;
        this.alertService.error(extractApiError(err, 'Unable to save notification configuration.'));
      }
    });
  }
}
