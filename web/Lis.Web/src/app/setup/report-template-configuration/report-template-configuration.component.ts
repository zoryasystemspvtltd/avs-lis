import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AlertService } from '../../_services/alert.service';
import { ReportTemplateConfigurationService } from '../../_services/report-template-configuration.service';

/**
 * Admin Report Template Configuration workspace (UI redesign).
 * Reuses existing workspace / create / activate / mode APIs.
 * Does not change template resolution, Gate 6, or RBAC.
 */
@Component({
  selector: 'app-report-template-configuration',
  templateUrl: './report-template-configuration.component.html',
  styles: [`
    .rtc-page { max-width: 1100px; }
    .rtc-page h3.panel-title { margin-top: 0; margin-bottom: 6px; }
    .rtc-page .rtc-lead { margin-bottom: 12px; }
    .rtc-toolbar { margin-bottom: 14px; }
    .rtc-mode-btns .btn { min-width: 130px; margin-right: 6px; }
    .rtc-section { margin-bottom: 16px; padding: 12px 14px; border: 1px solid #ddd; background: #fff; border-radius: 2px; }
    .rtc-section h4 { margin: 0 0 8px; font-size: 15px; font-weight: 700; }
    .rtc-section h5 { margin: 12px 0 8px; font-size: 13px; font-weight: 700; text-transform: uppercase; letter-spacing: .02em; color: #555; }
    .rtc-section-head { display: flex; align-items: center; justify-content: space-between; gap: 8px; flex-wrap: wrap; margin-bottom: 8px; }
    .rtc-section-head h4, .rtc-section-head h5 { margin: 0; }
    .rtc-muted { color: #777; margin: 0 0 8px; }
    .rtc-create-panel { margin: 10px 0 12px; padding: 12px; border: 1px solid #337ab7; background: #f7fbff; border-radius: 2px; }
    .rtc-create-panel h5 { margin: 0 0 10px; text-transform: none; letter-spacing: 0; color: #337ab7; }
    .rtc-filters { margin-bottom: 8px; }
    .rtc-filters .form-control { display: inline-block; width: auto; max-width: 220px; margin-right: 8px; vertical-align: middle; }
    .rtc-filters label { margin-right: 4px; font-weight: 600; }
    .rtc-status-active { color: #3c763d; font-weight: 600; }
    .rtc-status-draft { color: #777; }
    .rtc-empty { color: #888; font-style: italic; margin: 6px 0 10px; }
    .rtc-pager { margin-top: 8px; display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 8px; }
    .rtc-pager .rtc-range { color: #666; font-size: 12px; }
    .rtc-preview { margin-top: 16px; }
    .table.rtc-table > thead > tr > th { white-space: nowrap; }
  `]
})
export class ReportTemplateConfigurationComponent implements OnInit {
  readonly pageTitle = 'Report Template Configuration';
  readonly pageSize = 15;

  loading = false;
  reportType = 'Diagnostic';
  /** Persisted mode from API — never hard-coded on load. */
  mode = 'SystemDefault';
  workspace: any = null;
  preview: any = null;
  message = '';

  /** Which inline create panel is open: none | system | generic | specific */
  createPanel: 'none' | 'system' | 'generic' | 'specific' = 'none';
  createName = '';
  createFrom = 'SystemDefault';
  createCategory = 'CustomGeneric';
  targetType = 'Test';
  targetId: number = null;
  targets: any[] = [];

  genericSearch = '';
  genericStatus: 'All' | 'Active' | 'Inactive' = 'All';
  genericPage = 1;

  testSearch = '';
  testFilter: number | 'All' = 'All';
  testPage = 1;

  profileSearch = '';
  profileFilter: number | 'All' = 'All';
  profilePage = 1;

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
        this.resetPages();
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Unable to load workspace.'));
      }
    );
  }

  onReportTypeChange() {
    this.cancelCreate();
    this.reload();
  }

  /**
   * Mode changes require confirmation and persist via existing SetMode API.
   * Never auto-reset on deploy/refresh — backend EnsureModeRow only seeds when missing.
   */
  requestModeChange(next: string) {
    if (!next || next === this.mode) {
      return;
    }
    if (next === 'Custom') {
      const ok = confirm(
        'Switch to Custom Mode?\n\n' +
        'Custom mode will use activated custom templates according to the configured resolution hierarchy.\n\n' +
        'Templates without a matching custom template will fall back to System Default.'
      );
      if (!ok) {
        return;
      }
      this.persistMode('Custom');
      return;
    }
    if (next === 'SystemDefault') {
      const ok = confirm(
        'Switch to System Default?\n\n' +
        'The standard System Default report format will be used.\n\n' +
        'Your custom templates will be preserved and can be used again if Custom mode is selected later.'
      );
      if (!ok) {
        return;
      }
      this.persistMode('SystemDefault');
    }
  }

  private persistMode(mode: string) {
    this.loading = true;
    this.service.setMode(this.reportType, mode).subscribe(
      m => {
        this.mode = m.mode || m.Mode || mode;
        this.message = mode === 'Custom'
          ? 'Custom mode saved. Activated custom templates apply according to resolution hierarchy.'
          : 'System Default mode saved. Custom templates are preserved but not used for production resolution.';
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

  openCreateSystem() {
    this.beginCreate('system', 'CustomGeneric');
  }

  openCreateGeneric() {
    this.beginCreate('generic', 'CustomGeneric');
  }

  openCreateSpecific() {
    this.beginCreate('specific', 'Specific');
  }

  private beginCreate(panel: 'system' | 'generic' | 'specific', category: string) {
    this.createPanel = panel;
    this.createName = '';
    this.createFrom = 'SystemDefault';
    this.createCategory = category;
    this.targetType = 'Test';
    this.targetId = null;
    if (panel === 'specific' || !this.targets.length) {
      this.service.targets('').subscribe(
        rows => this.targets = rows || [],
        () => this.targets = []
      );
    }
  }

  cancelCreate() {
    this.createPanel = 'none';
    this.createName = '';
    this.createFrom = 'SystemDefault';
    this.createCategory = 'CustomGeneric';
    this.targetType = 'Test';
    this.targetId = null;
  }

  createTemplate() {
    if (!this.createName || !this.createName.trim()) {
      this.alertService.error('Template name is required.');
      return;
    }
    const body: any = {
      reportType: this.reportType,
      name: this.createName.trim(),
      createFrom: this.createFrom,
      templateCategory: this.createCategory
    };
    if (this.createCategory === 'Specific') {
      if (this.targetId == null) {
        this.alertService.error('Select a target Test or Profile.');
        return;
      }
      if (this.targetType === 'Profile') {
        body.targetProfileId = this.targetId;
      } else {
        body.targetTestId = this.targetId;
      }
    }
    this.loading = true;
    this.service.createCustom(body).subscribe(
      item => {
        this.cancelCreate();
        this.loading = false;
        this.message = 'Template created (Draft / Inactive). Opening designer…';
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

  confirmActivate(item: any) {
    if (!item) {
      return;
    }
    const name = this.val(item, 'name', 'Name') || 'Template';
    const category = this.val(item, 'templateCategory', 'TemplateCategory');
    let msg = 'Activate Template?\n\nTemplate:\n' + name + '\n\n';
    if (category === 'Specific') {
      if (this.val(item, 'targetTestId', 'TargetTestId')) {
        msg += 'Target:\nTest — ' + (this.val(item, 'targetLabel', 'TargetLabel') || this.val(item, 'targetTestId', 'TargetTestId')) + '\n\n';
      } else if (this.val(item, 'targetProfileId', 'TargetProfileId')) {
        msg += 'Target:\nProfile — ' + (this.val(item, 'targetLabel', 'TargetLabel') || this.val(item, 'targetProfileId', 'TargetProfileId')) + '\n\n';
      }
    } else {
      msg += 'This template will become the active Generic template for Custom mode.\n\n';
    }
    msg += 'Activation does not change Template Mode.';
    if (!confirm(msg)) {
      return;
    }
    this.loading = true;
    this.service.activate(item.id || item.Id).subscribe(
      () => {
        this.message = 'Template activated. Template Mode was not changed.';
        this.reload();
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Activation failed.'));
      }
    );
  }

  confirmDeactivate(item: any) {
    if (!item) {
      return;
    }
    const name = this.val(item, 'name', 'Name') || 'Template';
    if (!confirm('Deactivate Template?\n\nTemplate:\n' + name + '\n\nThe template will remain saved as Draft / Inactive.')) {
      return;
    }
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

  // ---- list helpers (client-side; no new APIs) ----

  get generics(): any[] {
    if (!this.workspace) {
      return [];
    }
    return this.workspace.customGenerics || this.workspace.CustomGenerics || [];
  }

  get specifics(): any[] {
    if (!this.workspace) {
      return [];
    }
    return this.workspace.specificTemplates || this.workspace.SpecificTemplates || [];
  }

  get testSpecifics(): any[] {
    return this.specifics.filter(t => !!this.val(t, 'targetTestId', 'TargetTestId'));
  }

  get profileSpecifics(): any[] {
    return this.specifics.filter(t => !!this.val(t, 'targetProfileId', 'TargetProfileId'));
  }

  filteredGenerics(): any[] {
    const q = (this.genericSearch || '').trim().toLowerCase();
    return this.generics.filter(t => {
      const name = String(this.val(t, 'name', 'Name') || '').toLowerCase();
      const active = !!this.val(t, 'isActivated', 'IsActivated');
      if (q && name.indexOf(q) < 0) {
        return false;
      }
      if (this.genericStatus === 'Active' && !active) {
        return false;
      }
      if (this.genericStatus === 'Inactive' && active) {
        return false;
      }
      return true;
    });
  }

  filteredTestSpecifics(): any[] {
    const q = (this.testSearch || '').trim().toLowerCase();
    return this.testSpecifics.filter(t => {
      const name = String(this.val(t, 'name', 'Name') || '').toLowerCase();
      const label = String(this.val(t, 'targetLabel', 'TargetLabel') || '').toLowerCase();
      const tid = this.val(t, 'targetTestId', 'TargetTestId');
      if (this.testFilter !== 'All' && tid !== this.testFilter) {
        return false;
      }
      if (q && name.indexOf(q) < 0 && label.indexOf(q) < 0) {
        return false;
      }
      return true;
    });
  }

  filteredProfileSpecifics(): any[] {
    const q = (this.profileSearch || '').trim().toLowerCase();
    return this.profileSpecifics.filter(t => {
      const name = String(this.val(t, 'name', 'Name') || '').toLowerCase();
      const label = String(this.val(t, 'targetLabel', 'TargetLabel') || '').toLowerCase();
      const pid = this.val(t, 'targetProfileId', 'TargetProfileId');
      if (this.profileFilter !== 'All' && pid !== this.profileFilter) {
        return false;
      }
      if (q && name.indexOf(q) < 0 && label.indexOf(q) < 0) {
        return false;
      }
      return true;
    });
  }

  paged(list: any[], page: number): any[] {
    const p = Math.max(1, page || 1);
    const start = (p - 1) * this.pageSize;
    return list.slice(start, start + this.pageSize);
  }

  rangeLabel(total: number, page: number): string {
    if (!total) {
      return 'Showing 0';
    }
    const p = Math.max(1, page || 1);
    const from = (p - 1) * this.pageSize + 1;
    const to = Math.min(total, p * this.pageSize);
    return 'Showing ' + from + '–' + to + ' of ' + total;
  }

  testFilterOptions(): { id: number; label: string }[] {
    const map: { [k: number]: string } = {};
    this.testSpecifics.forEach(t => {
      const id = this.val(t, 'targetTestId', 'TargetTestId');
      if (id != null && map[id] == null) {
        map[id] = this.val(t, 'targetLabel', 'TargetLabel') || ('Test #' + id);
      }
    });
    return Object.keys(map).map(k => ({ id: +k, label: map[+k] })).sort((a, b) => a.label.localeCompare(b.label));
  }

  profileFilterOptions(): { id: number; label: string }[] {
    const map: { [k: number]: string } = {};
    this.profileSpecifics.forEach(t => {
      const id = this.val(t, 'targetProfileId', 'TargetProfileId');
      if (id != null && map[id] == null) {
        map[id] = this.val(t, 'targetLabel', 'TargetLabel') || ('Profile #' + id);
      }
    });
    return Object.keys(map).map(k => ({ id: +k, label: map[+k] })).sort((a, b) => a.label.localeCompare(b.label));
  }

  targetOptions(): any[] {
    return (this.targets || []).filter(x => (x.targetType || x.TargetType) === this.targetType);
  }

  statusText(item: any): string {
    return this.val(item, 'isActivated', 'IsActivated') ? 'Active' : 'Draft / Inactive';
  }

  onGenericFilterChange() {
    this.genericPage = 1;
  }

  onTestFilterChange() {
    this.testPage = 1;
  }

  onProfileFilterChange() {
    this.profilePage = 1;
  }

  private resetPages() {
    this.genericPage = 1;
    this.testPage = 1;
    this.profilePage = 1;
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
