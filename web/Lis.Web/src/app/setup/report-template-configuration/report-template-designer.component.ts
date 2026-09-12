import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { AlertService } from '../../_services/alert.service';
import { ReportTemplateConfigurationService } from '../../_services/report-template-configuration.service';

/**
 * Phase 3 structured three-panel designer (palette / A4 canvas / properties).
 * Not a free-form graphics editor. Clearance bands are locked.
 */
@Component({
  selector: 'app-report-template-designer',
  templateUrl: './report-template-designer.component.html',
  styles: [`
    .designer-shell { display:flex; gap:12px; min-height:70vh; }
    .designer-palette { width:220px; background:#f7f7f7; border:1px solid #ddd; padding:8px; overflow:auto; }
    .designer-canvas-wrap { flex:1; background:#e5e5e5; padding:16px; overflow:auto; }
    .designer-props { width:300px; background:#f7f7f7; border:1px solid #ddd; padding:8px; overflow:auto; }
    .a4-page { width:210mm; min-height:297mm; margin:0 auto; background:#fff; box-shadow:0 1px 6px rgba(0,0,0,.25); position:relative; }
    .clearance-band { background:repeating-linear-gradient(45deg,#f0f0f0,#f0f0f0 8px,#e0e0e0 8px,#e0e0e0 16px); color:#666; text-align:center; font-size:11px; padding:4px; }
    .clearance-header { height:50mm; }
    .clearance-footer { height:50mm; }
    .canvas-body { min-height:160mm; padding:8px 12px; border-left:1px dashed #ccc; border-right:1px dashed #ccc; }
    .palette-item, .canvas-item { border:1px solid #ccc; background:#fff; padding:6px 8px; margin:4px 0; cursor:move; }
    .canvas-item.selected { outline:2px solid #337ab7; }
    .toolbar { margin-bottom:10px; }
    .cdk-drag-preview { box-sizing:border-box; border:1px solid #337ab7; padding:6px; background:#fff; }
    .props-section { border-top:1px solid #ddd; margin-top:10px; padding-top:8px; }
    .props-section h6 { margin:0 0 8px; font-weight:700; }
  `]
})
export class ReportTemplateDesignerComponent implements OnInit {
  templateId: number;
  reportType = 'Diagnostic';
  templateName = '';
  loading = false;
  message = '';
  components: any[] = [];
  fields: any[] = [];
  conditionFields: any[] = [];
  canvasItems: DesignerNode[] = [];
  selected: DesignerNode = null;
  preview: any = null;
  validation: any = null;
  headerMm = 50;
  footerMm = 50;

  readonly fontFamilies = ['Arial', 'Helvetica', 'Times New Roman', 'Courier New', 'sans-serif', 'serif'];
  readonly fontSizes = ['9pt', '10pt', '11pt', '12pt', '14pt', '16pt', '18pt'];
  readonly alignments = ['left', 'center', 'right'];
  readonly conditionOps = [
    { value: 'Exists', label: 'Exists' },
    { value: 'NotEmpty', label: 'Not Empty' },
    { value: 'Equals', label: 'Equals' },
    { value: 'NotEquals', label: 'Not Equals' }
  ];
  readonly collectionSources = [
    { value: 'DepartmentGroups', label: 'Department Groups' },
    { value: 'ProfileGroups', label: 'Profile Groups' },
    { value: 'Both', label: 'Department then Profile Groups' }
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private service: ReportTemplateConfigurationService,
    private alertService: AlertService
  ) { }

  ngOnInit() {
    this.templateId = +this.route.snapshot.paramMap.get('id');
    this.load();
  }

  load() {
    this.loading = true;
    this.service.list().subscribe(
      rows => {
        const t = (rows || []).find(x => (x.id || x.Id) === this.templateId);
        if (!t) {
          this.alertService.error('Template not found.');
          this.router.navigate(['/report-template-configuration']);
          return;
        }
        this.reportType = t.reportType || t.ReportType;
        this.templateName = t.name || t.Name;
        this.service.listVersions(this.templateId).subscribe(versions => {
          const draft = (versions || []).find(v => (v.status || v.Status) === 'Draft')
            || (versions || []).find(v => (v.status || v.Status) === 'Published');
          const json = draft ? (draft.definitionJson || draft.DefinitionJson) : null;
          this.canvasItems = this.parseDefinition(json);
          this.loading = false;
        });
        this.service.listComponents(this.reportType).subscribe(c => this.components = c || []);
        this.service.designFields(this.reportType).subscribe(f => {
          this.fields = f || [];
          this.conditionFields = (this.fields || []).filter(x => !(x.isCollection || x.IsCollection));
        });
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Unable to load template.'));
      }
    );
  }

  dropPaletteToCanvas(event: CdkDragDrop<any[]>) {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      return;
    }
    const src = event.previousContainer.data[event.previousIndex];
    const node = this.createNode(src);
    event.container.data.splice(event.currentIndex, 0, node);
    this.selected = node;
  }

  select(node: DesignerNode) {
    this.selected = node;
    this.ensureStyle(node);
  }

  removeSelected() {
    if (!this.selected) { return; }
    this.canvasItems = this.canvasItems.filter(x => x !== this.selected);
    this.selected = null;
  }

  save() {
    const definitionJson = this.buildDefinition();
    this.loading = true;
    this.service.saveDesign({
      templateId: this.templateId,
      name: this.templateName,
      definitionJson
    }).subscribe(
      () => {
        this.loading = false;
        this.message = 'Saved (not activated).';
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Save failed.'));
      }
    );
  }

  validate() {
    const definitionJson = this.buildDefinition();
    this.service.validateDesign(this.reportType, definitionJson).subscribe(
      v => {
        this.validation = v;
        this.message = (v.isValid || v.IsValid) ? 'Validation passed.' : 'Validation failed.';
      },
      err => this.alertService.error(this.readError(err, 'Validate failed.'))
    );
  }

  previewSample() {
    const definitionJson = this.buildDefinition();
    this.loading = true;
    this.service.previewDefinition(this.reportType, definitionJson).subscribe(
      r => {
        this.preview = r;
        this.loading = false;
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Preview failed.'));
      }
    );
  }

  activate() {
    this.loading = true;
    this.service.saveDesign({
      templateId: this.templateId,
      name: this.templateName,
      definitionJson: this.buildDefinition()
    }).subscribe(
      () => {
        this.service.activate(this.templateId).subscribe(
          () => {
            this.loading = false;
            this.message = 'Activated. Switch Template Mode to Custom for runtime selection.';
          },
          err => {
            this.loading = false;
            this.alertService.error(this.readError(err, 'Activation failed.'));
          }
        );
      },
      err => {
        this.loading = false;
        this.alertService.error(this.readError(err, 'Save before activate failed.'));
      }
    );
  }

  back() {
    this.router.navigate(['/report-template-configuration']);
  }

  showTypography(node: DesignerNode): boolean {
    if (!node) { return false; }
    return ['TEXT', 'PATIENT_FIELD', 'ORDER_FIELD', 'INVOICE_FIELD', 'VISIT_FIELD', 'TEST_FIELD',
      'SAMPLE_FIELD', 'DOCTOR_FIELD', 'TECHNICIAN_FIELD', 'ACCESSION_FIELD', 'RADIOLOGY_FIELD',
      'COMMENT_FIELD', 'DOCTOR_APPROVAL_COMMENT', 'SIGNATURE'].indexOf(node.type) >= 0;
  }

  showWidth(node: DesignerNode): boolean {
    if (!node) { return false; }
    return node.type !== 'SPACER' && node.type !== 'LINE';
  }

  showHeight(node: DesignerNode): boolean {
    if (!node) { return false; }
    return node.type === 'SPACER' || node.type === 'SIGNATURE' || node.type === 'IMAGE';
  }

  showBorder(node: DesignerNode): boolean {
    if (!node) { return false; }
    return ['TEXT', 'PATIENT_FIELD', 'ORDER_FIELD', 'INVOICE_FIELD', 'VISIT_FIELD', 'TEST_FIELD',
      'SAMPLE_FIELD', 'DOCTOR_FIELD', 'TECHNICIAN_FIELD', 'ACCESSION_FIELD', 'RADIOLOGY_FIELD',
      'PARAMETER_TABLE', 'SECTION', 'COMMENT_FIELD', 'DOCTOR_APPROVAL_COMMENT', 'REPEATING_PARAMETER_GROUP']
      .indexOf(node.type) >= 0;
  }

  showConditions(node: DesignerNode): boolean {
    if (!node) { return false; }
    return ['LINE', 'SPACER'].indexOf(node.type) < 0;
  }

  showCollectionSource(node: DesignerNode): boolean {
    return !!node && node.type === 'REPEATING_PARAMETER_GROUP';
  }

  showFieldBinding(node: DesignerNode): boolean {
    if (!node) { return false; }
    return ['PARAMETER_TABLE', 'SECTION', 'LINE', 'SPACER', 'REPEATING_PARAMETER_GROUP', 'SIGNATURE']
      .indexOf(node.type) < 0;
  }

  onVisibilityModeChange(mode: string) {
    if (!this.selected) { return; }
    this.selected.visibilityMode = mode;
    if (mode === 'hidden') {
      this.ensureStyle(this.selected);
      this.selected.style.visibility = 'hidden';
      this.selected.visibleWhen = null;
    } else if (mode === 'conditional') {
      this.ensureStyle(this.selected);
      this.selected.style.visibility = 'visible';
      if (!this.selected.visibleWhen) {
        this.selected.visibleWhen = {
          op: 'NotEmpty',
          binding: this.selected.binding || (this.conditionFields[0] && (this.conditionFields[0].path || this.conditionFields[0].Path)) || '',
          value: ''
        };
      }
    } else {
      this.ensureStyle(this.selected);
      this.selected.style.visibility = 'visible';
      this.selected.visibleWhen = null;
    }
  }

  onConditionOpChange() {
    if (!this.selected || !this.selected.visibleWhen) { return; }
    const op = this.selected.visibleWhen.op;
    if (op !== 'Equals' && op !== 'NotEquals') {
      this.selected.visibleWhen.value = '';
    }
  }

  private createNode(src: any): DesignerNode {
    const type = (src.type || src.Type || 'TEXT').toUpperCase();
    const id = type.toLowerCase() + '-' + Math.random().toString(36).slice(2, 7);
    const node: DesignerNode = {
      id,
      type,
      label: src.displayName || src.DisplayName || type,
      binding: '',
      children: [],
      style: this.defaultStyle(),
      visibilityMode: 'always',
      collectionSource: 'DepartmentGroups'
    };
    if (type === 'REPEATING_PARAMETER_GROUP') {
      node.label = 'Repeating Parameter Group';
      node.collectionSource = 'DepartmentGroups';
    }
    if (type === 'PARAMETER_TABLE') {
      node.columns = [
        { binding: 'Parameter.ParameterName', label: 'Parameter', widthPct: 40, align: 'left' },
        { binding: 'Parameter.ResultValue', label: 'Result', widthPct: 20, showFlag: true, align: 'center' },
        { binding: 'Parameter.Unit', label: 'Unit', widthPct: 15, align: 'center' },
        { binding: 'Parameter.ReferenceRange', label: 'Ref. Range', widthPct: 25, align: 'center' }
      ];
    }
    if (type === 'SIGNATURE') {
      node.source = 'Doctor';
      node.visibilityMode = 'conditional';
      node.visibleWhen = { op: 'NotEmpty', binding: 'Doctor.Name', value: '' };
    }
    if (type.indexOf('PATIENT') >= 0) { node.binding = 'Patient.Name'; node.label = 'Patient Name'; }
    if (type === 'COMMENT_FIELD') {
      node.binding = 'section.Comment';
      node.visibilityMode = 'conditional';
      node.visibleWhen = { op: 'NotEmpty', binding: 'section.Comment', value: '' };
    }
    if (type === 'DOCTOR_APPROVAL_COMMENT') {
      node.binding = 'section.DoctorApprovalComment';
      node.visibilityMode = 'conditional';
      node.visibleWhen = { op: 'NotEmpty', binding: 'section.DoctorApprovalComment', value: '' };
    }
    if (type === 'SPACER') {
      node.style.height = '4mm';
    }
    return node;
  }

  private defaultStyle(): DesignerStyle {
    return {
      width: '',
      height: '',
      fontFamily: 'Arial',
      fontSize: '11pt',
      fontWeight: 'normal',
      fontStyle: 'normal',
      textAlign: 'left',
      marginTop: '',
      marginBottom: '',
      padding: '',
      border: 'none',
      visibility: 'visible',
      whiteSpace: 'normal'
    };
  }

  private ensureStyle(node: DesignerNode) {
    if (!node.style) {
      node.style = this.defaultStyle();
    }
  }

  private parseDefinition(json: string): DesignerNode[] {
    if (!json) { return []; }
    try {
      const root = JSON.parse(json);
      const body = root.body || root.Body;
      const children = body && (body.children || body.Children);
      if (Array.isArray(children)) {
        return children.map((c, i) => this.fromJson(c, i));
      }
      const comps = root.components || root.Components;
      if (Array.isArray(comps)) {
        return comps.map((c, i) => this.fromJson(c, i));
      }
    } catch { /* ignore */ }
    return [];
  }

  private fromJson(c: any, i: number): DesignerNode {
    const style = this.mergeStyle(c.style || c.Style);
    const visibleWhen = c.visibleWhen || c.VisibleWhen || null;
    let visibilityMode = 'always';
    if (style.visibility === 'hidden') {
      visibilityMode = 'hidden';
    } else if (visibleWhen) {
      visibilityMode = 'conditional';
    }
    const repeat = c.repeat || c.Repeat;
    return {
      id: c.id || c.Id || ('n' + i),
      type: (c.type || c.Type || 'TEXT').toUpperCase(),
      label: c.label || c.Label || c.type || c.Type,
      binding: c.binding || c.Binding || '',
      source: c.source || c.Source,
      columns: c.columns || c.Columns,
      visibleWhen: visibleWhen,
      visibilityMode,
      collectionSource: (repeat && (repeat.source || repeat.Source)) || 'DepartmentGroups',
      style,
      children: (c.children || c.Children || []).map((x, j) => this.fromJson(x, j))
    };
  }

  private mergeStyle(raw: any): DesignerStyle {
    const d = this.defaultStyle();
    if (!raw) { return d; }
    return {
      width: raw.width || raw.Width || '',
      height: raw.height || raw.Height || '',
      fontFamily: raw.fontFamily || raw.FontFamily || d.fontFamily,
      fontSize: raw.fontSize || raw.FontSize || d.fontSize,
      fontWeight: raw.fontWeight || raw.FontWeight || d.fontWeight,
      fontStyle: raw.fontStyle || raw.FontStyle || d.fontStyle,
      textAlign: raw.textAlign || raw.TextAlign || d.textAlign,
      marginTop: raw.marginTop || raw.MarginTop || '',
      marginBottom: raw.marginBottom || raw.MarginBottom || '',
      padding: raw.padding || raw.Padding || '',
      border: raw.border || raw.Border || raw.borderBottom || raw.BorderBottom || d.border,
      visibility: raw.visibility || raw.Visibility || d.visibility,
      whiteSpace: raw.whiteSpace || raw.WhiteSpace || d.whiteSpace
    };
  }

  private buildDefinition(): string {
    const body = {
      type: 'SECTION',
      id: 'root',
      children: this.canvasItems.map(n => this.toJson(n))
    };
    return JSON.stringify({
      schemaVersion: 1,
      reportType: this.reportType,
      renderer: 'declarative',
      layoutSource: 'ReportLayoutConfiguration',
      page: {
        size: 'A4',
        orientation: 'Portrait',
        useGlobalLayoutClearance: true
      },
      body
    });
  }

  private toJson(n: DesignerNode): any {
    const o: any = { type: n.type, id: n.id };
    if (n.label) { o.label = n.label; }
    if (n.binding) { o.binding = n.binding; }
    if (n.source) { o.source = n.source; }
    if (n.columns) { o.columns = n.columns; }

    const style = this.buildStyleObject(n);
    if (style && Object.keys(style).length) {
      o.style = style;
    }

    if (n.visibilityMode === 'conditional' && n.visibleWhen && n.visibleWhen.op && n.visibleWhen.binding) {
      o.visibleWhen = {
        op: n.visibleWhen.op,
        binding: n.visibleWhen.binding
      };
      if ((n.visibleWhen.op === 'Equals' || n.visibleWhen.op === 'NotEquals') && n.visibleWhen.value != null && n.visibleWhen.value !== '') {
        o.visibleWhen.value = n.visibleWhen.value;
      }
    }

    if (n.type === 'REPEATING_PARAMETER_GROUP') {
      o.repeat = {
        source: n.collectionSource || 'DepartmentGroups',
        alias: (n.collectionSource === 'ProfileGroups') ? 'group' : 'dept'
      };
    }

    if (n.children && n.children.length) {
      o.children = n.children.map(c => this.toJson(c));
    }
    if (n.type === 'SIGNATURE') {
      o.nameBinding = n.source === 'Technician' ? 'Technician.Name' : 'Doctor.Name';
      o.imageBinding = n.source === 'Technician' ? 'Technician.SignatureImage' : 'Doctor.SignatureImage';
      if (!o.visibleWhen) {
        o.visibleWhen = { op: 'NotEmpty', binding: o.nameBinding };
      }
    }
    if (n.type === 'SPACER' && n.style && n.style.height) {
      const mm = String(n.style.height).replace(/mm$/i, '');
      if (!isNaN(+mm)) {
        o.heightMm = +mm;
      }
    }
    return o;
  }

  private buildStyleObject(n: DesignerNode): any {
    if (!n.style) { return null; }
    const s: any = {};
    const st = n.style;
    if (n.visibilityMode === 'hidden') {
      s.visibility = 'hidden';
    } else if (st.visibility && st.visibility !== 'visible') {
      s.visibility = st.visibility;
    }
    if (this.showWidth(n) && st.width) { s.width = st.width; }
    if (this.showHeight(n) && st.height) { s.height = st.height; }
    if (this.showTypography(n)) {
      if (st.fontFamily) { s.fontFamily = st.fontFamily; }
      if (st.fontSize) { s.fontSize = st.fontSize; }
      if (st.fontWeight && st.fontWeight !== 'normal') { s.fontWeight = st.fontWeight; }
      if (st.fontStyle && st.fontStyle !== 'normal') { s.fontStyle = st.fontStyle; }
      if (st.textAlign) { s.textAlign = st.textAlign; }
      if (st.whiteSpace && st.whiteSpace !== 'normal') { s.whiteSpace = st.whiteSpace; }
    }
    if (st.marginTop) { s.marginTop = st.marginTop; }
    if (st.marginBottom) { s.marginBottom = st.marginBottom; }
    if (st.padding) { s.padding = st.padding; }
    if (this.showBorder(n) && st.border && st.border !== 'none') {
      s.border = st.border;
    }
    return s;
  }

  private readError(err: any, fallback: string): string {
    if (typeof err?.error === 'string' && err.error.trim()) { return err.error; }
    if (err?.error?.message) { return err.error.message; }
    return fallback;
  }
}

export interface DesignerStyle {
  width?: string;
  height?: string;
  fontFamily?: string;
  fontSize?: string;
  fontWeight?: string;
  fontStyle?: string;
  textAlign?: string;
  marginTop?: string;
  marginBottom?: string;
  padding?: string;
  border?: string;
  visibility?: string;
  whiteSpace?: string;
}

export interface DesignerNode {
  id: string;
  type: string;
  label?: string;
  binding?: string;
  source?: string;
  columns?: any[];
  visibleWhen?: { op?: string; binding?: string; value?: string };
  visibilityMode?: string;
  collectionSource?: string;
  style?: DesignerStyle;
  children?: DesignerNode[];
}
