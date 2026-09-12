import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AlertService } from '../../_services/alert.service';
import { ReportTemplateConfigurationService } from '../../_services/report-template-configuration.service';

/**
 * Phase 3 Admin workspace: Template Mode + System Default + Custom templates.
 * Production print remains on built-in Angular reports until a later phase.
 */
@Component({
  selector: 'app-report-template-configuration',
  templateUrl: './report-template-configuration.component.html'
})
export class ReportTemplateConfigurationComponent implements OnInit {
  readonly pageTitle = 'Report Template Configuration';
  loading = false;
  reportType = 'Diagnostic';
  mode = 'SystemDefault';
  workspace: any = null;
  preview: any = null;
  message = '';

  // create dialog state
  showCreate = false;
  createName = '';
  createFrom = 'SystemDefault';
  createCategory = 'CustomGeneric';
  targetType = 'Test';
  targetId: number = null;
  targets: any[] = [];

  constructor(
    private service: ReportTemplateConfigurationService,
    private alertService: AlertService,
    private router: Router
  ) { }

  ngOnInit() {
    this.reload();
  }

  reload() {
    this.loading = true;
    this.service.workspace(this.reportType).subscribe(
      w => {
        this.workspace = w;
        this.mode = w.mode || w.Mode || 'SystemDefault';
        this.loading = false;
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Unable to load workspace.'));
      }
    );
  }

  onReportTypeChange() {
    this.reload();
  }

  setMode(mode: string) {
    this.loading = true;
    this.service.setMode(this.reportType, mode).subscribe(
      m => {
        this.mode = m.mode || m.Mode;
        this.message = mode === 'Custom'
          ? 'Custom mode enabled. Activated templates may apply when production declarative print is enabled (ops flag; currently OFF by default).'
          : 'System Default mode enabled. Custom templates remain saved but inactive for runtime.';
        this.reload();
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Unable to change mode.'));
      }
    );
  }

  previewSystemDefault() {
    this.loading = true;
    this.service.previewSample(this.reportType).subscribe(
      r => {
        this.preview = r;
        this.loading = false;
        this.message = 'System Default sample preview (synthetic data).';
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Preview failed.'));
      }
    );
  }

  openCreate() {
    this.showCreate = true;
    this.createName = '';
    this.createFrom = 'SystemDefault';
    this.createCategory = 'CustomGeneric';
    this.targetId = null;
    this.service.targets('').subscribe(rows => this.targets = rows || []);
  }

  createTemplate() {
    const body: any = {
      reportType: this.reportType,
      name: this.createName,
      createFrom: this.createFrom,
      templateCategory: this.createCategory
    };
    if (this.createCategory === 'Specific') {
      if (this.targetType === 'Profile') {
        body.targetProfileId = this.targetId;
      } else {
        body.targetTestId = this.targetId;
      }
    }
    this.loading = true;
    this.service.createCustom(body).subscribe(
      item => {
        this.showCreate = false;
        this.loading = false;
        this.message = 'Template created (not activated).';
        const id = item.id || item.Id;
        this.router.navigate(['/report-template-configuration/design', id]);
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Create failed.'));
      }
    );
  }

  design(item: any) {
    if (!item || (item.isLocked || item.IsLocked)) {
      return;
    }
    this.router.navigate(['/report-template-configuration/design', item.id || item.Id]);
  }

  activate(item: any) {
    this.loading = true;
    this.service.activate(item.id || item.Id).subscribe(
      () => {
        this.message = 'Template activated. Ensure Template Mode is Custom for runtime selection.';
        this.reload();
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Activation failed.'));
      }
    );
  }

  deactivate(item: any) {
    this.loading = true;
    this.service.deactivate(item.id || item.Id).subscribe(
      () => {
        this.message = 'Template deactivated (not deleted).';
        this.reload();
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Deactivation failed.'));
      }
    );
  }

  private readError(err: any, fallback: string): string {
    if (typeof err?.error === 'string' && err.error.trim()) {
      return err.error;
    }
    if (err?.error?.message) {
      return err.error.message;
    }
    return fallback;
  }

  val(obj: any, camel: string, pascal: string) {
    return obj ? (obj[camel] != null ? obj[camel] : obj[pascal]) : null;
  }
}
