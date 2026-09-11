import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { AlertService } from '../../_services';
import { ReportLayoutConfigurationService, ReportLayoutConfigurationDto } from '../../_services/report-layout-configuration.service';
import { extractApiError } from '../../_helpers/api-error';

@Component({
  selector: 'app-report-layout-configuration',
  templateUrl: './report-layout-configuration.component.html'
})
export class ReportLayoutConfigurationComponent implements OnInit, OnDestroy {
  form: FormGroup;
  loading = false;
  saving = false;
  reportType = 'Diagnostic';

  readonly reportTypes = [
    { value: 'Diagnostic', label: 'Diagnostic Report' },
    { value: 'Radiology', label: 'Radiology Report' }
  ];

  readonly horizontalOptions = [
    { value: 'Left', label: 'Left' },
    { value: 'Center', label: 'Center' },
    { value: 'Right', label: 'Right' }
  ];

  /**
   * Technician signature is captured on User Create/Edit (same store as Doctor).
   * Diagnostic reports resolve identity via TestResult.ReviewedBy.
   * Radiology has no technician approver identity yet — layout can still be saved.
   */
  readonly technicianSignatureAvailable = true;

  private enabledSubs: Subscription[] = [];

  constructor(
    private fb: FormBuilder,
    private layoutService: ReportLayoutConfigurationService,
    private alertService: AlertService
  ) {
    this.form = this.fb.group({
      id: [0],
      reportType: ['Diagnostic', Validators.required],
      pageSize: [{ value: 'A4', disabled: true }],
      orientation: [{ value: 'Portrait', disabled: true }],
      headerHeightMm: [50, [Validators.required, Validators.min(0), Validators.max(120)]],
      footerHeightMm: [50, [Validators.required, Validators.min(0), Validators.max(120)]],
      leftMarginMm: [12, [Validators.required, Validators.min(0), Validators.max(50)]],
      rightMarginMm: [12, [Validators.required, Validators.min(0), Validators.max(50)]],
      doctorSignatureEnabled: [true],
      doctorSignatureHorizontal: ['Right', Validators.required],
      doctorSignatureVertical: [{ value: 'Bottom', disabled: true }],
      doctorSignatureWidthMm: [50, [Validators.required, Validators.min(5), Validators.max(80)]],
      doctorSignatureHeightMm: [14, [Validators.required, Validators.min(5), Validators.max(80)]],
      technicianSignatureEnabled: [false],
      technicianSignatureHorizontal: ['Left', Validators.required],
      technicianSignatureVertical: [{ value: 'Bottom', disabled: true }],
      technicianSignatureWidthMm: [50, [Validators.required, Validators.min(5), Validators.max(80)]],
      technicianSignatureHeightMm: [14, [Validators.required, Validators.min(5), Validators.max(80)]]
    });
  }

  ngOnInit() {
    this.enabledSubs.push(
      this.form.get('doctorSignatureEnabled').valueChanges.subscribe(() => this.syncDoctorSignatureControls()),
      this.form.get('technicianSignatureEnabled').valueChanges.subscribe(() => this.syncTechnicianSignatureControls())
    );
    this.syncDoctorSignatureControls();
    this.syncTechnicianSignatureControls();
    this.load();
  }

  ngOnDestroy() {
    this.enabledSubs.forEach(s => s.unsubscribe());
  }

  get previewSignatureAlign(): string {
    return this.alignCss(this.form.get('doctorSignatureHorizontal')?.value);
  }

  get previewTechnicianAlign(): string {
    return this.alignCss(this.form.get('technicianSignatureHorizontal')?.value);
  }

  get isRadiologyReportType(): boolean {
    return (this.form.getRawValue().reportType || '') === 'Radiology';
  }

  onReportTypeChange() {
    this.reportType = this.form.getRawValue().reportType;
    this.load();
  }

  load() {
    this.loading = true;
    const type = this.form.getRawValue().reportType || 'Diagnostic';
    this.layoutService.getByReportType(type).subscribe(
      dto => {
        this.patchForm(dto);
        this.loading = false;
      },
      err => {
        this.loading = false;
        this.alertService.error(extractApiError(err) || 'Unable to load report layout configuration.');
      }
    );
  }

  save() {
    this.syncDoctorSignatureControls();
    this.syncTechnicianSignatureControls();
    if (this.form.invalid) {
      this.alertService.error('Please correct validation errors before saving.');
      return;
    }

    const raw = this.form.getRawValue();
    const contentH = 297 - (+raw.headerHeightMm) - (+raw.footerHeightMm);
    if (contentH < 80) {
      this.alertService.error('Header + Footer clearance leave less than 80 mm content height on A4.');
      return;
    }

    const dto: ReportLayoutConfigurationDto = {
      id: raw.id || 0,
      reportType: raw.reportType,
      pageSize: 'A4',
      orientation: 'Portrait',
      headerHeightMm: +raw.headerHeightMm,
      footerHeightMm: +raw.footerHeightMm,
      leftMarginMm: +raw.leftMarginMm,
      rightMarginMm: +raw.rightMarginMm,
      doctorSignatureEnabled: !!raw.doctorSignatureEnabled,
      doctorSignatureHorizontal: raw.doctorSignatureHorizontal,
      doctorSignatureVertical: 'Bottom',
      doctorSignatureWidthMm: +raw.doctorSignatureWidthMm,
      doctorSignatureHeightMm: +raw.doctorSignatureHeightMm,
      technicianSignatureEnabled: !!raw.technicianSignatureEnabled,
      technicianSignatureHorizontal: raw.technicianSignatureHorizontal,
      technicianSignatureVertical: 'Bottom',
      technicianSignatureWidthMm: +raw.technicianSignatureWidthMm,
      technicianSignatureHeightMm: +raw.technicianSignatureHeightMm,
      isActive: true
    };

    this.saving = true;
    this.layoutService.save(dto).subscribe(
      () => {
        this.saving = false;
        this.alertService.success('Report layout configuration saved.');
        this.load();
      },
      err => {
        this.saving = false;
        this.alertService.error(extractApiError(err) || 'Save failed.');
      }
    );
  }

  resetToDefault() {
    if (!confirm('Reset this report type to the default production layout?')) {
      return;
    }
    this.saving = true;
    const type = this.form.getRawValue().reportType || 'Diagnostic';
    this.layoutService.reset(type).subscribe(
      () => {
        this.saving = false;
        this.alertService.success('Layout reset to default.');
        this.load();
      },
      err => {
        this.saving = false;
        this.alertService.error(extractApiError(err) || 'Reset failed.');
      }
    );
  }

  private alignCss(pos: string): string {
    if (pos === 'Left') {
      return 'left';
    }
    if (pos === 'Center') {
      return 'center';
    }
    return 'right';
  }

  private syncDoctorSignatureControls() {
    this.syncSignatureControls(
      !!this.form.get('doctorSignatureEnabled').value,
      'doctorSignatureHorizontal',
      'doctorSignatureWidthMm',
      'doctorSignatureHeightMm'
    );
  }

  private syncTechnicianSignatureControls() {
    this.syncSignatureControls(
      !!this.form.get('technicianSignatureEnabled').value,
      'technicianSignatureHorizontal',
      'technicianSignatureWidthMm',
      'technicianSignatureHeightMm'
    );
  }

  private syncSignatureControls(enabled: boolean, horizontalKey: string, widthKey: string, heightKey: string) {
    const horizontal = this.form.get(horizontalKey);
    const width = this.form.get(widthKey);
    const height = this.form.get(heightKey);
    if (enabled) {
      horizontal.enable({ emitEvent: false });
      width.enable({ emitEvent: false });
      height.enable({ emitEvent: false });
    } else {
      horizontal.disable({ emitEvent: false });
      width.disable({ emitEvent: false });
      height.disable({ emitEvent: false });
    }
  }

  private patchForm(dto: ReportLayoutConfigurationDto | any) {
    if (!dto) {
      return;
    }
    const enabled = dto.doctorSignatureEnabled ?? dto.DoctorSignatureEnabled;
    const techEnabled = dto.technicianSignatureEnabled ?? dto.TechnicianSignatureEnabled;

    this.form.patchValue({
      id: dto.id ?? dto.Id ?? 0,
      reportType: dto.reportType || dto.ReportType,
      pageSize: dto.pageSize || dto.PageSize || 'A4',
      orientation: dto.orientation || dto.Orientation || 'Portrait',
      headerHeightMm: dto.headerHeightMm ?? dto.HeaderHeightMm,
      footerHeightMm: dto.footerHeightMm ?? dto.FooterHeightMm,
      leftMarginMm: dto.leftMarginMm ?? dto.LeftMarginMm,
      rightMarginMm: dto.rightMarginMm ?? dto.RightMarginMm,
      doctorSignatureEnabled: enabled !== false && enabled !== 0,
      doctorSignatureHorizontal: dto.doctorSignatureHorizontal || dto.DoctorSignatureHorizontal || 'Right',
      doctorSignatureVertical: 'Bottom',
      doctorSignatureWidthMm: dto.doctorSignatureWidthMm ?? dto.DoctorSignatureWidthMm,
      doctorSignatureHeightMm: dto.doctorSignatureHeightMm ?? dto.DoctorSignatureHeightMm,
      technicianSignatureEnabled: !!techEnabled && techEnabled !== 0,
      technicianSignatureHorizontal: dto.technicianSignatureHorizontal || dto.TechnicianSignatureHorizontal || 'Left',
      technicianSignatureVertical: 'Bottom',
      technicianSignatureWidthMm: dto.technicianSignatureWidthMm ?? dto.TechnicianSignatureWidthMm ?? 50,
      technicianSignatureHeightMm: dto.technicianSignatureHeightMm ?? dto.TechnicianSignatureHeightMm ?? 14
    });
    this.syncDoctorSignatureControls();
    this.syncTechnicianSignatureControls();
  }
}
