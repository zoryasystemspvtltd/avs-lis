import { Component, ChangeDetectorRef, OnDestroy, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription } from 'rxjs';
import { map } from 'rxjs/operators';
import { AlertService, MasterService } from '../../_services';

interface BillableItemRow {
  key: string;
  label: string;
  shortLabel: string;
  displayLabel: string;
  itemType: string;
  testId?: number;
  testProfileId?: number;
  departmentCode?: string;
  departmentGroup?: string;
  profileGroup?: string;
}

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
  private testBillablePool: BillableItemRow[] = [];
  private profileBillablePool: BillableItemRow[] = [];
  lineBillableItems: BillableItemRow[][] = [];
  private lastBillableSearchByType: { test?: string; profile?: string } = {};
  /** ng-select groupBy must be a stable function reference (v4). */
  readonly testGroupByFn = (item: BillableItemRow) => item.departmentGroup || 'Other';
  readonly profileGroupByFn = (item: BillableItemRow) => item.profileGroup || 'Profiles';
  /** Server returns filtered rows; disable ng-select client filter (avoids empty list after load). */
  readonly modalServerSearchFn = (_term: string, _item: BillableItemRow) => true;
  private billableItemCache = new Map<string, BillableItemRow>();
  departments: Array<{ code: string; name: string; processingCategory?: string }> = [];
  private activeBillableLineIndex = 0;
  patients: any[] = [];
  patientsLoading = false;
  billableItemsLoading = false;
  corporates: any[] = [];
  doctors: any[] = [];
  isPrintView = false;
  invoiceDto: any;
  private patientSearchTimer: ReturnType<typeof setTimeout>;
  private billableSearchTimer: ReturnType<typeof setTimeout>;
  private billableSearchSeq = 0;
  private billableSearchSub: Subscription;
  readonly paymentTypes = ['Cash', 'Card', 'UPI', 'Net Banking', 'Cheque', 'Credit'];
  readonly paymentStatuses = [
    { value: 0, label: 'Unpaid' },
    { value: 1, label: 'Partially Paid' },
    { value: 2, label: 'Paid' }
  ];
  readonly discountTypes = ['Percentage', 'Fixed Amount'];
  readonly itemTypes = [
    { value: 'test', label: 'Test' },
    { value: 'profile', label: 'Profile' }
  ];
  /** Debounced server search; 0 = show full list on open, type to filter. */
  readonly searchMinLength = 0;
  readonly searchDebounceMs = 300;
  readonly billablePageSize = 100;

  /** Add-line modal (new invoice entry flow). */
  showAddLineModal = false;
  modalItemType: 'test' | 'profile' = 'test';
  modalSelectedKey = '';
  modalTestItems: BillableItemRow[] = [];
  modalProfileItems: BillableItemRow[] = [];
  modalSelectedItem: BillableItemRow | null = null;
  modalTestLoading = false;
  modalProfileLoading = false;
  private modalTestSearchTimer: ReturnType<typeof setTimeout>;
  private modalProfileSearchTimer: ReturnType<typeof setTimeout>;
  private lastModalSearchByType: { test?: string; profile?: string } = {};
  private modalTestSearchSeq = 0;
  private modalProfileSearchSeq = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private masterService: MasterService,
    private alertService: AlertService,
    private cdr: ChangeDetectorRef) { }

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
      paymentType: ['Cash'],
      discountType: ['Fixed Amount'],
      headerDiscountValue: [0],
      lines: this.fb.array([])
    });

    this.masterService.getAll('Corporate').subscribe(c => {
      this.corporates = (c || []).filter(x => x.isActive !== false && x.IsActive !== false);
    });
    this.masterService.getAll('ReferralDoctor').subscribe(d => {
      this.doctors = (d || []).filter(x => x.isActive !== false && x.IsActive !== false);
    });
    this.masterService.getAll('Department').subscribe(d => {
      this.departments = (d || []).map(x => ({
        code: x.code ?? x.Code ?? '',
        name: x.name ?? x.Name ?? '',
        processingCategory: x.processingCategory ?? x.ProcessingCategory ?? 'Laboratory'
      })).filter(x => x.code);
      this.refreshLineBillableItems();
    });
    this.loadPatients('');

    const navState = this.router.getCurrentNavigation()?.extras?.state as { patientId?: number };
    const statePatientId = navState?.patientId ?? (history.state?.patientId as number);

    if (this.id) {
      this.loadInvoice(+this.id);
    } else {
      this.masterService.getNextInvoiceNo().subscribe(no => this.form.patchValue({ invoiceNo: no }));
      if (statePatientId && +statePatientId > 0) {
        this.preselectPatient(+statePatientId);
      }
    }
  }

  get modalSearchLoading(): boolean {
    return this.modalItemType === 'profile' ? this.modalProfileLoading : this.modalTestLoading;
  }

  get modalActiveItems(): BillableItemRow[] {
    return this.modalItemType === 'profile' ? this.modalProfileItems : this.modalTestItems;
  }

  get totalDiscountPercentLabel(): string {
    const gross = +this.form?.get('grossAmount')?.value || 0;
    const disc = +this.form?.get('discountAmount')?.value || 0;
    if (!gross || !disc) {
      return '';
    }
    const pct = Math.round((disc / gross) * 10000) / 100;
    return `(${pct}%)`;
  }

  get printDiscountPercentLabel(): string {
    const gross = +this.invoiceDto?.invoice?.grossAmount || 0;
    const disc = +this.invoiceDto?.invoice?.discountAmount || 0;
    if (!gross || !disc) {
      return '';
    }
    const pct = Math.round((disc / gross) * 10000) / 100;
    return ` (${pct}%)`;
  }

  get isCancelled(): boolean { return this.form?.value?.invoiceStatus === 3; }
  /** Invoice locked (paid/cancelled workflow) — not payment status on draft. */
  get isInvoiceLocked(): boolean { return this.isCancelled || this.form?.value?.invoiceStatus === 2; }

  loadInvoice(invoiceId: number) {
    this.masterService.getInvoice(invoiceId).subscribe(dto => {
      this.invoiceDto = dto;
      if (dto?.invoice) {
        const inv = dto.invoice;
        inv.invoiceDate = inv.invoiceDate ? inv.invoiceDate.substring(0, 10) : '';
        const gross = +inv.grossAmount || 0;
        const disc = +inv.discountAmount || 0;
        let headerDiscountValue = disc;
        if ((inv.discountType || 'Fixed Amount') === 'Percentage' && gross > 0) {
          headerDiscountValue = Math.round((disc / gross) * 10000) / 100;
        }
        this.form.patchValue({
          ...inv,
          paymentType: inv.paymentType || 'Cash',
          discountType: inv.discountType || 'Fixed Amount',
          headerDiscountValue
        });
        this.lines.clear();
        (dto.details || []).forEach(line => {
          const itemType = line.testProfileId ? 'profile' : 'test';
          const departmentCode = line.departmentCode || '';
          line.lineItemKey = this.buildLineItemKey(itemType, line.testId, line.testProfileId);
          if (line.lineItemKey) {
            const label = line.testName || line.testProfileName || line.lineItemKey;
            this.billableItemCache.set(line.lineItemKey, this.enrichBillableItem({
              key: line.lineItemKey,
              label,
              itemType,
              testId: line.testId,
              testProfileId: line.testProfileId,
              departmentCode: departmentCode || undefined
            }));
          }
          this.addLine({ ...line, itemType, departmentCode });
        });
        this.ensureSelectedLineItemsInList();
        if (this.isCancelled || this.isInvoiceLocked) {
          this.form.disable();
        }
        this.ensureSelectedPatientInList();
      }
    });
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }

  addLine(line?: any) {
    const itemType = line?.itemType || (line?.testProfileId ? 'profile' : 'test');
    const lineItemKey = line?.lineItemKey || this.buildLineItemKey(itemType, line?.testId, line?.testProfileId);
    this.lines.push(this.fb.group({
      id: [line?.id || 0],
      itemType: [itemType, Validators.required],
      departmentCode: [line?.departmentCode || ''],
      lineItemKey: [lineItemKey, Validators.required],
      testProfileId: [line?.testProfileId || null],
      testId: [line?.testId || '', Validators.required],
      rate: [line?.rate || 0],
      quantity: [line?.quantity || 1],
      amount: [line?.amount || 0],
      discountType: [line?.discountType || 'Fixed Amount'],
      discountAmount: [line?.discountAmount || 0],
      taxAmount: [line?.taxAmount || 0],
      netAmount: [line?.netAmount || 0],
      sampleNo: [line?.sampleNo || ''],
      testLabel: [line?.testName || '']
    }));
    this.refreshLineBillableItems(this.lines.length - 1);
  }

  removeLine(i: number) {
    this.lines.removeAt(i);
    this.refreshLineBillableItems();
    this.recalc();
  }

  getInvoiceDate(): string {
    const v = this.form.getRawValue ? this.form.getRawValue() : this.form.value;
    return v.invoiceDate || new Date().toISOString().substring(0, 10);
  }

  getTestName(testId: number, testProfileId?: number, testProfileName?: string): string {
    if (testProfileId) {
      const key = `profile:${testProfileId}`;
      const cached = this.billableItemCache.get(key);
      if (cached) {
        return cached.label;
      }
      return testProfileName ? `Profile: ${testProfileName}` : `Profile #${testProfileId}`;
    }
    const bloodKey = `test:${testId}`;
    const legacyBlood = `blood:${testId}`;
    const legacyRad = `radiology:${testId}`;
    const cached = this.billableItemCache.get(bloodKey)
      || this.billableItemCache.get(legacyBlood)
      || this.billableItemCache.get(legacyRad);
    if (cached) {
      return cached.label;
    }
    return String(testId);
  }

  getLineDescription(line: any): string {
    return this.getTestName(line?.testId, line?.testProfileId, line?.testProfileName);
  }

  private buildLineItemKey(itemType: string, testId?: number, testProfileId?: number): string {
    if (itemType === 'profile' && testProfileId) {
      return `profile:${testProfileId}`;
    }
    if (testId) {
      return `test:${testId}`;
    }
    return '';
  }

  isTestLine(i: number): boolean {
    return this.lines.at(i)?.get('itemType')?.value === 'test';
  }

  onItemTypeChange(i: number): void {
    const line = this.lines.at(i);
    line.patchValue({
      departmentCode: '',
      lineItemKey: '',
      testId: '',
      testProfileId: null,
      testLabel: '',
      rate: 0,
      amount: 0,
      netAmount: 0
    });
    this.activeBillableLineIndex = i;
    this.searchBillableItems('', i);
    this.recalc();
  }

  getBillableItemsForLine(lineIndex: number): BillableItemRow[] {
    return this.lineBillableItems[lineIndex] || [];
  }

  private refreshLineBillableItems(lineIndex?: number): void {
    const indexes = lineIndex != null
      ? [lineIndex]
      : this.lines.controls.map((_, idx) => idx);
    for (const idx of indexes) {
      const line = this.lines.at(idx);
      if (!line) {
        continue;
      }
      const itemType = line.get('itemType')?.value === 'profile' ? 'profile' : 'test';
      const pool = itemType === 'profile' ? this.profileBillablePool : this.testBillablePool;
      this.lineBillableItems[idx] = this.mergePoolWithLineSelections(pool, itemType);
    }
  }

  getDepartmentDisplay(lineIndex: number): string {
    const line = this.lines.at(lineIndex);
    if (!line || line.get('itemType')?.value !== 'test') {
      return '';
    }
    const code = line.get('departmentCode')?.value;
    if (!code) {
      return '—';
    }
    return this.departmentNameByCode(code);
  }

  onLineItemChange(i: number): void {
    const line = this.lines.at(i);
    const key = line.get('lineItemKey')?.value;
    const item = this.billableItemCache.get(key)
      || this.getBillableItemsForLine(i).find(x => x.key === key);
    if (!item) {
      line.patchValue({ testId: '', testProfileId: null });
      return;
    }

    this.billableItemCache.set(item.key, item);

    const duplicate = this.lines.controls.some((c, idx) => idx !== i && c.value.lineItemKey === key);
    if (duplicate) {
      this.alertService.error('Item already added to invoice');
      line.patchValue({ lineItemKey: '' });
      return;
    }

    line.patchValue({
      itemType: item.itemType,
      testId: item.testId || '',
      testProfileId: item.testProfileId || null,
      departmentCode: item.itemType === 'test' ? (item.departmentCode || '') : '',
      testLabel: item.shortLabel || item.label
    });

    if (item.itemType === 'profile') {
      this.onProfileChange(i);
      return;
    }

    if (item.testId) {
      this.onLineTestChange(i);
    }
  }

  onProfileChange(i: number) {
    const line = this.lines.at(i);
    const profileId = +line.get('testProfileId').value;
    if (!profileId) { return; }

    const duplicate = this.lines.controls.some((c, idx) =>
      idx !== i && c.value.itemType === 'profile' && +c.value.testProfileId === profileId);
    if (duplicate) {
      this.alertService.error('Profile already added to invoice');
      line.patchValue({ testProfileId: null });
      return;
    }

    this.masterService.getProfileHierarchy(profileId).subscribe(profile => {
      if (!profile || profile.isActive === false) {
        this.alertService.error('Selected profile is inactive or unavailable');
        line.patchValue({ testProfileId: null });
        return;
      }

      const details = profile.profileDetails || profile.ProfileDetails || [];
      const firstTestId = details[0]?.testId;
      if (!firstTestId) {
        this.alertService.error('Profile has no tests configured');
        line.patchValue({ testProfileId: null });
        return;
      }

      const amount = +profile.packageRate || 0;
      line.patchValue({
        testId: firstTestId,
        rate: profile.packageRate,
        quantity: 1,
        amount,
        discountAmount: 0,
        taxAmount: 0,
        netAmount: amount
      });
      this.recalc();
    });
  }

  private departmentNameByCode(code: string): string {
    if (!code) {
      return 'Unassigned';
    }
    const dept = this.departments.find(d => d.code === code);
    return dept?.name || code;
  }

  private extractShortLabel(label: string, itemType: string): string {
    if (!label) {
      return '';
    }
    if (itemType === 'profile') {
      return label.replace(/^\[Profile\]\s*/i, '').replace(/\s*\([^)]*\)\s*$/, '').trim();
    }
    const dash = label.indexOf(' - ');
    if (dash > 0) {
      return label.substring(dash + 3).trim();
    }
    return label.trim();
  }

  private asText(value: any): string {
    if (value == null) {
      return '';
    }
    if (typeof value === 'string') {
      return value.trim();
    }
    if (typeof value === 'number' || typeof value === 'boolean') {
      return String(value);
    }
    return '';
  }

  private enrichBillableItem(base: Partial<BillableItemRow> & { key: string; label: string; itemType: string }): BillableItemRow {
    const itemType = base.itemType === 'profile' ? 'profile' : 'test';
    const label = this.asText(base.label);
    const shortLabel = this.asText(base.shortLabel) || this.extractShortLabel(label, itemType) || label || this.asText(base.key);
    const displayLabel = shortLabel || label || this.asText(base.key);
    const departmentCode = this.asText(base.departmentCode) || undefined;
    const departmentGroup = itemType === 'test'
      ? (this.departmentNameByCode(departmentCode || '') || 'Other')
      : undefined;
    return {
      key: this.asText(base.key),
      label: label || displayLabel,
      shortLabel,
      displayLabel,
      itemType,
      testId: base.testId,
      testProfileId: base.testProfileId,
      departmentCode,
      departmentGroup,
      profileGroup: itemType === 'profile' ? 'Profiles' : undefined
    };
  }

  private normalizeBillableItem(item: any): BillableItemRow {
    const itemType = item.itemType ?? item.ItemType ?? item.lineType ?? item.LineType ?? 'test';
    const normalizedType = itemType === 'profile' ? 'profile' : 'test';
    return this.enrichBillableItem({
      key: item.key ?? item.Key ?? '',
      label: item.label ?? item.Label ?? '',
      itemType: normalizedType,
      testId: item.testId ?? item.TestId,
      testProfileId: item.testProfileId ?? item.TestProfileId,
      departmentCode: item.departmentCode ?? item.DepartmentCode
    });
  }

  private sortBillableItems(items: BillableItemRow[]): BillableItemRow[] {
    return items.slice().sort((a, b) => {
      const ga = a.departmentGroup || a.profileGroup || '';
      const gb = b.departmentGroup || b.profileGroup || '';
      if (ga !== gb) {
        return ga.localeCompare(gb, undefined, { sensitivity: 'base' });
      }
      return (a.displayLabel || a.shortLabel || a.label).localeCompare(b.displayLabel || b.shortLabel || b.label, undefined, { sensitivity: 'base' });
    });
  }

  private mergePoolWithLineSelections(pool: BillableItemRow[], itemType: string): BillableItemRow[] {
    const map = new Map<string, BillableItemRow>();
    (pool || []).forEach(item => {
      const key = this.asText(item?.key);
      if (!key) {
        return;
      }
      map.set(key, item);
      this.billableItemCache.set(key, item);
    });
    this.lines.controls.forEach(line => {
      const lineType = line.get('itemType')?.value === 'profile' ? 'profile' : 'test';
      if (lineType !== itemType) {
        return;
      }
      const key = line.get('lineItemKey')?.value;
      if (!key || map.has(key)) {
        return;
      }
      const cached = this.billableItemCache.get(key);
      if (cached) {
        map.set(key, cached);
        return;
      }
      const testId = +line.get('testId')?.value;
      const profileId = +line.get('testProfileId')?.value;
      const label = line.get('testLabel')?.value || key;
      const departmentCode = line.get('departmentCode')?.value || undefined;
      const fallback = this.enrichBillableItem({
        key, label, itemType, testId: testId || undefined, testProfileId: profileId || undefined, departmentCode
      });
      map.set(key, fallback);
      this.billableItemCache.set(key, fallback);
    });
    return this.sortBillableItems(Array.from(map.values()));
  }

  private applyBillableSearchResults(items: BillableItemRow[], itemType: string): void {
    const normalized = (items || []).map(x => this.normalizeBillableItem(x));
    if (itemType === 'profile') {
      this.profileBillablePool = this.mergePoolWithLineSelections(normalized, 'profile');
    } else {
      this.testBillablePool = this.mergePoolWithLineSelections(normalized, 'test');
    }
    this.refreshLineBillableItems();
  }

  onBillableDropdownOpen(lineIndex: number): void {
    this.activeBillableLineIndex = lineIndex;
    const itemType = this.lines.at(lineIndex)?.get('itemType')?.value === 'profile' ? 'profile' : 'test';
    if (!this.billableItemsLoading) {
      const lastSearch = this.lastBillableSearchByType[itemType] ?? '';
      this.searchBillableItems(lastSearch, lineIndex);
    }
  }

  onBillableSearch(event: any, lineIndex: number): void {
    this.activeBillableLineIndex = lineIndex;
    const search = (typeof event === 'string' ? event : event?.term || '').trim();
    if (this.billableSearchTimer) {
      clearTimeout(this.billableSearchTimer);
    }
    this.billableSearchTimer = setTimeout(() => this.searchBillableItems(search, lineIndex), this.searchDebounceMs);
  }

  openAddLineModal(): void {
    if (this.isCancelled || this.isInvoiceLocked) {
      return;
    }
    this.modalItemType = 'test';
    this.modalSelectedKey = '';
    this.modalSelectedItem = null;
    this.modalTestLoading = false;
    this.modalProfileLoading = false;
    this.lastModalSearchByType = {};
    this.modalTestItems = this.filterAvailableBillableItems([...this.testBillablePool]);
    this.modalProfileItems = this.filterAvailableBillableItems([...this.profileBillablePool]);
    this.showAddLineModal = true;
    document.body.classList.add('sale-invoice-modal-open', 'modal-open');
    setTimeout(() => {
      if (!this.showAddLineModal) {
        return;
      }
      this.loadModalBillableItems('', 'test');
      this.loadModalBillableItems('', 'profile');
    }, 0);
  }

  closeAddLineModal(): void {
    this.showAddLineModal = false;
    this.modalSelectedKey = '';
    this.modalSelectedItem = null;
    this.modalTestLoading = false;
    this.modalProfileLoading = false;
    this.modalTestSearchSeq++;
    this.modalProfileSearchSeq++;
    document.body.classList.remove('sale-invoice-modal-open', 'modal-open');
    if (this.modalTestSearchTimer) {
      clearTimeout(this.modalTestSearchTimer);
    }
    if (this.modalProfileSearchTimer) {
      clearTimeout(this.modalProfileSearchTimer);
    }
  }

  onModalItemTypeChange(type: 'test' | 'profile'): void {
    if (this.modalItemType === type) {
      return;
    }
    this.modalItemType = type;
    this.modalSelectedKey = '';
    this.modalSelectedItem = null;
    const lastSearch = this.lastModalSearchByType[type] ?? '';
    this.loadModalBillableItems(lastSearch, type);
  }

  onModalBillableSearch(event: any): void {
    const search = (typeof event === 'string' ? event : event?.term || '').trim();
    const type = this.modalItemType;
    this.lastModalSearchByType[type] = search;
    if (type === 'profile') {
      if (this.modalProfileSearchTimer) {
        clearTimeout(this.modalProfileSearchTimer);
      }
      this.modalProfileSearchTimer = setTimeout(() => {
        if (this.showAddLineModal) {
          this.loadModalBillableItems(search, 'profile');
        }
      }, this.searchDebounceMs);
      return;
    }
    if (this.modalTestSearchTimer) {
      clearTimeout(this.modalTestSearchTimer);
    }
    this.modalTestSearchTimer = setTimeout(() => {
      if (this.showAddLineModal) {
        this.loadModalBillableItems(search, 'test');
      }
    }, this.searchDebounceMs);
  }

  onModalSelectOpen(): void {
    const type = this.modalItemType;
    const lastSearch = this.lastModalSearchByType[type] ?? '';
    this.loadModalBillableItems(lastSearch, type);
  }

  onModalItemSelected(key: string | null): void {
    if (!key) {
      this.modalSelectedKey = '';
      this.modalSelectedItem = null;
      return;
    }
    this.modalSelectedKey = key;
    const pool = this.modalItemType === 'profile' ? this.modalProfileItems : this.modalTestItems;
    const item = pool.find(x => x.key === key) || this.billableItemCache.get(key) || null;
    this.modalSelectedItem = item;
    if (item) {
      this.billableItemCache.set(key, item);
    }
  }

  get modalDepartmentPreview(): string {
    if (this.modalItemType !== 'test') {
      return '—';
    }
    const item = this.getModalSelectedItem();
    if (!item?.departmentCode) {
      return '—';
    }
    return this.departmentNameByCode(item.departmentCode);
  }

  get modalSelectedItemPreview(): string {
    const item = this.getModalSelectedItem();
    return item?.displayLabel || '—';
  }

  private getModalSelectedItem(): BillableItemRow | null {
    if (this.modalSelectedItem?.key) {
      return this.modalSelectedItem;
    }
    if (!this.modalSelectedKey) {
      return null;
    }
    return this.modalTestItems.find(x => x.key === this.modalSelectedKey)
      || this.modalProfileItems.find(x => x.key === this.modalSelectedKey)
      || this.billableItemCache.get(this.modalSelectedKey)
      || null;
  }

  private extractBillableItemsFromResponse(response: any): any[] {
    if (!response) {
      return [];
    }
    const raw = response.items ?? response.Items;
    if (!raw) {
      return [];
    }
    return Array.isArray(raw) ? raw : [raw];
  }

  private normalizeBillableItems(raw: any[]): BillableItemRow[] {
    return (raw || [])
      .map(x => this.normalizeBillableItem(x))
      .filter(x => !!x.key);
  }

  private setModalBillablePool(itemType: 'test' | 'profile', items: BillableItemRow[]): void {
    let sorted = this.sortBillableItems(this.filterAvailableBillableItems(items));
    if (this.modalSelectedItem?.key && !sorted.some(x => x.key === this.modalSelectedItem.key)) {
      sorted = [this.modalSelectedItem, ...sorted];
    }
    if (itemType === 'profile') {
      this.modalProfileItems = [...sorted];
    } else {
      this.modalTestItems = [...sorted];
    }
    sorted.forEach(item => this.billableItemCache.set(item.key, item));
  }

  private loadModalBillableItems(searchText: string, itemType: 'test' | 'profile'): void {
    const trimmed = (searchText || '').trim();
    this.lastModalSearchByType[itemType] = trimmed;
    const seq = itemType === 'profile' ? ++this.modalProfileSearchSeq : ++this.modalTestSearchSeq;
    if (itemType === 'profile') {
      this.modalProfileLoading = true;
    } else {
      this.modalTestLoading = true;
    }

    this.fetchModalBillableItems(trimmed, itemType).subscribe({
      next: items => {
        const currentSeq = itemType === 'profile' ? this.modalProfileSearchSeq : this.modalTestSearchSeq;
        if (seq !== currentSeq || !this.showAddLineModal) {
          return;
        }
        this.setModalBillablePool(itemType, items);
        if (itemType === 'profile') {
          this.modalProfileLoading = false;
        } else {
          this.modalTestLoading = false;
        }
        this.cdr.markForCheck();
      },
      error: () => {
        const currentSeq = itemType === 'profile' ? this.modalProfileSearchSeq : this.modalTestSearchSeq;
        if (seq !== currentSeq || !this.showAddLineModal) {
          return;
        }
        if (itemType === 'profile') {
          this.modalProfileLoading = false;
        } else {
          this.modalTestLoading = false;
        }
        this.alertService.error(`Unable to load ${itemType === 'profile' ? 'profiles' : 'tests'}. Check API connection and try again.`);
        this.cdr.markForCheck();
      }
    });
  }

  private fetchModalBillableItems(searchText: string, itemType: 'test' | 'profile'): Observable<BillableItemRow[]> {
    return this.masterService.getBillableItems(
      (searchText || '').trim(),
      this.getInvoiceDate(),
      1,
      this.billablePageSize,
      itemType,
      undefined
    ).pipe(
      map(response => this.normalizeBillableItems(this.extractBillableItemsFromResponse(response)))
    );
  }

  submitAddLineModal(): void {
    const item = this.getModalSelectedItem();
    if (!item?.key) {
      this.alertService.error('Please select a test or profile');
      return;
    }
    if (this.lines.controls.some(c => c.value.lineItemKey === item.key)) {
      this.alertService.error('Item already added to invoice');
      return;
    }
    this.billableItemCache.set(item.key, item);
    this.addLine({
      itemType: item.itemType,
      lineItemKey: item.key,
      testId: item.testId,
      testProfileId: item.testProfileId,
      departmentCode: item.departmentCode || '',
      testName: item.displayLabel
    });
    const lineIndex = this.lines.length - 1;
    if (item.itemType === 'profile') {
      this.onProfileChange(lineIndex);
    } else if (item.testId) {
      this.onLineTestChange(lineIndex);
    }
    this.closeAddLineModal();
  }

  private filterAvailableBillableItems(items: BillableItemRow[]): BillableItemRow[] {
    const taken = new Set(
      this.lines.controls.map(c => c.value.lineItemKey).filter((k: string) => !!k)
    );
    return (items || []).filter(i => i.key && !taken.has(i.key));
  }

  private searchBillableItems(searchText: string, lineIndex: number): void {
    const line = this.lines.at(lineIndex);
    const itemType = line?.get('itemType')?.value === 'profile' ? 'profile' : 'test';
    const trimmed = (searchText || '').trim();

    this.lastBillableSearchByType[itemType] = trimmed;

    const seq = ++this.billableSearchSeq;
    if (this.billableSearchSub) {
      this.billableSearchSub.unsubscribe();
    }
    this.billableItemsLoading = true;
    this.billableSearchSub = this.masterService.getBillableItems(
      trimmed,
      this.getInvoiceDate(),
      1,
      this.billablePageSize,
      itemType,
      undefined
    ).subscribe(
      response => {
        if (seq !== this.billableSearchSeq) {
          return;
        }
        const items = this.normalizeBillableItems(this.extractBillableItemsFromResponse(response));
        this.applyBillableSearchResults(items, itemType);
        this.billableItemsLoading = false;
      },
      () => {
        if (seq !== this.billableSearchSeq) {
          return;
        }
        this.billableItemsLoading = false;
      }
    );
  }

  private ensureSelectedLineItemsInList(): void {
    this.testBillablePool = this.mergePoolWithLineSelections(this.testBillablePool, 'test');
    this.profileBillablePool = this.mergePoolWithLineSelections(this.profileBillablePool, 'profile');
    this.refreshLineBillableItems();
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
    document.body.classList.remove('sale-invoice-modal-open');
    document.body.classList.remove('modal-open');
    if (this.patientSearchTimer) {
      clearTimeout(this.patientSearchTimer);
    }
    if (this.billableSearchTimer) {
      clearTimeout(this.billableSearchTimer);
    }
    if (this.billableSearchSub) {
      this.billableSearchSub.unsubscribe();
    }
    if (this.modalTestSearchTimer) {
      clearTimeout(this.modalTestSearchTimer);
    }
    if (this.modalProfileSearchTimer) {
      clearTimeout(this.modalProfileSearchTimer);
    }
  }

  loadPatients(searchText: string): void {
    this.patientsLoading = true;
    this.masterService.getBillingPatients({
      RecordPerPage: 100,
      CurrentPage: 1,
      SearchText: searchText || '',
      SortColumnName: 'Name',
      SortDirection: false,
      Status: 0
    }).subscribe(
      p => {
        this.patients = this.normalizePatients(p?.items || p?.Items || []);
        this.ensureSelectedPatientInList();
        this.patientsLoading = false;
      },
      () => {
        this.patientsLoading = false;
      }
    );
  }

  onPatientSearch(event: any): void {
    const search = (typeof event === 'string' ? event : event?.term || '').trim();
    if (this.patientSearchTimer) {
      clearTimeout(this.patientSearchTimer);
    }
    this.patientSearchTimer = setTimeout(() => this.loadPatients(search), 300);
  }

  private normalizePatients(items: any[]): any[] {
    return items
      .map(x => ({
        id: x.id ?? x.Id,
        name: (x.name ?? x.Name ?? '').trim(),
        phone: x.phone ?? x.Phone ?? '',
        hisPatientId: x.hisPatientId ?? x.HisPatientId ?? '',
        mrNo: x.mrNo ?? x.MRNo ?? '',
        visitId: x.visitId ?? x.VisitId ?? '',
        patientPrefix: x.patientPrefix ?? x.PatientPrefix ?? ''
      }))
      .filter(x => x.id > 0 && x.name);
  }

  private preselectPatient(patientId: number): void {
    this.masterService.getItem('PatientMaster', patientId).subscribe(patient => {
      if (!patient) {
        return;
      }
      const normalized = this.normalizePatients([patient])[0];
      if (!normalized) {
        return;
      }
      if (!this.patients.some(p => p.id === normalized.id)) {
        this.patients = [normalized, ...this.patients];
      }
      this.form.patchValue({ patientId: normalized.id });
    });
  }

  private ensureSelectedPatientInList(): void {
    const patientId = this.form?.get('patientId')?.value;
    if (!patientId || this.patients.some(p => p.id === patientId)) {
      return;
    }
    const inv = this.invoiceDto?.invoice;
    if (inv) {
      this.patients = [{
        id: patientId,
        name: inv.patientName || `Patient #${patientId}`,
        phone: inv.patientPhone || '',
        hisPatientId: inv.patientId || '',
        mrNo: '',
        visitId: '',
        patientPrefix: ''
      }, ...this.patients];
    }
  }

  patientOptionLabel(patient: any): string {
    if (!patient) {
      return '';
    }
    const extra = patient.mrNo || patient.phone || patient.hisPatientId || 'N/A';
    const prefix = patient.patientPrefix ? `${patient.patientPrefix} ` : '';
    return `${prefix}${patient.name} (${extra})`;
  }

  onTestChange(i: number) {
    const line = this.lines.at(i);
    if (line.get('itemType').value === 'profile') {
      return;
    }
    const testId = line.get('testId').value;
    if (!testId) { return; }

    const duplicate = this.lines.controls.some((c, idx) =>
      idx !== i && +c.value.testId === +testId && c.value.itemType === 'test');
    if (duplicate) {
      this.alertService.error('Test already added to invoice');
      line.patchValue({ testId: '', lineItemKey: '' });
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
    this.onRateContextChange();
    if (this.lines.length > 0) {
      const itemType = this.lines.at(this.activeBillableLineIndex || 0)?.get('itemType')?.value === 'profile'
        ? 'profile' : 'test';
      const lastSearch = this.lastBillableSearchByType[itemType] ?? '';
      this.searchBillableItems(lastSearch, this.activeBillableLineIndex || 0);
    }
  }

  onLineTestChange(i: number): void {
    const line = this.lines.at(i);
    const testId = +line.get('testId')?.value;
    const key = line.get('lineItemKey')?.value;
    const item = this.billableItemCache.get(key)
      || this.getBillableItemsForLine(i).find(x => x.key === key);
    if (item) {
      line.patchValue({ testLabel: item.label }, { emitEvent: false });
    }
    this.onTestChange(i);
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
    const discount = this.computeDiscount(amount, line.discountType, line.discountAmount);
    const net = amount - discount + (line.taxAmount || 0);
    this.lines.at(i).patchValue({ amount, netAmount: net }, { emitEvent: false });
    this.recalc();
  }

  private computeDiscount(amount: number, discountType: string, discountValue: number): number {
    if (!discountValue) {
      return 0;
    }
    if (discountType === 'Percentage') {
      return Math.round(amount * discountValue) / 100;
    }
    return discountValue;
  }

  recalc(syncPaymentStatusFromPaid = false) {
    let gross = 0, lineDisc = 0, tax = 0, net = 0;
    this.lines.controls.forEach(c => {
      const v = c.value;
      const amount = (v.rate || 0) * (v.quantity || 1);
      gross += amount;
      lineDisc += this.computeDiscount(amount, v.discountType, v.discountAmount);
      tax += v.taxAmount || 0;
      net += v.netAmount || 0;
    });

    const headerType = this.form.get('discountType')?.value || 'Fixed Amount';
    const headerDiscInput = +this.form.get('headerDiscountValue')?.value || 0;
    let totalDisc = lineDisc;
    if (headerType === 'Percentage') {
      totalDisc = this.computeDiscount(gross, 'Percentage', headerDiscInput);
    } else if (headerDiscInput > 0) {
      totalDisc = headerDiscInput;
    }

    const paid = +this.form.getRawValue().paidAmount || 0;
    const finalNet = gross - totalDisc + tax;
    const patch: Record<string, number> = {
      grossAmount: gross,
      discountAmount: totalDisc,
      taxAmount: tax,
      netAmount: finalNet,
      dueAmount: Math.max(0, finalNet - paid)
    };
    if (syncPaymentStatusFromPaid) {
      let paymentStatus = 0;
      if (paid > 0 && paid < finalNet) {
        paymentStatus = 1;
      } else if (paid >= finalNet && finalNet > 0) {
        paymentStatus = 2;
      }
      patch.paymentStatus = paymentStatus;
    }
    this.form.patchValue(patch, { emitEvent: false });
  }

  onPaidAmountChange(): void {
    this.recalc(true);
  }

  onPaymentStatusChange(): void {
    const status = +this.form.get('paymentStatus')?.value;
    const net = +this.form.get('netAmount')?.value || 0;
    let paid = +this.form.get('paidAmount')?.value || 0;
    if (status === 0) {
      paid = 0;
    } else if (status === 2) {
      paid = net;
    } else if (status === 1) {
      if (paid <= 0 && net > 0) {
        paid = Math.round(net * 50) / 100;
      }
      if (paid >= net && net > 0) {
        paid = Math.round(net * 50) / 100;
      }
    }
    this.form.patchValue({ paidAmount: paid, paymentStatus: status }, { emitEvent: false });
    this.recalc(false);
  }

  onTotalDiscountChange(): void {
    this.recalc();
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
      .filter(l => l.testId || l.testProfileId)
      .map(l => {
        const amount = (+l.rate || 0) * (+l.quantity || 1);
        const discountAmount = this.computeDiscount(amount, l.discountType, +l.discountAmount || 0);
        return {
          id: l.id || 0,
          testId: +l.testId,
          testProfileId: l.itemType === 'profile' && l.testProfileId ? +l.testProfileId : null,
          rate: +l.rate,
          quantity: +l.quantity,
          amount,
          discountAmount,
          taxAmount: +l.taxAmount,
          netAmount: amount - discountAmount + (+l.taxAmount || 0),
          sampleNo: l.sampleNo
        };
      });

    if (!lineItems.length) {
      this.alertService.error('Add at least one test line');
      return;
    }

    const paid = +val.paidAmount || 0;
    if (paid < 0) {
      this.alertService.error('Paid amount cannot be negative');
      return;
    }
    if (paid > +val.netAmount) {
      this.alertService.error('Paid amount cannot exceed net amount');
      return;
    }

    const loadedIsActive = this.invoiceDto?.invoice?.isActive ?? this.invoiceDto?.invoice?.IsActive;
    const dto = {
      invoice: Object.assign({}, val, {
        id: val.id || (this.id ? +this.id : 0),
        invoiceDate: new Date(val.invoiceDate),
        invoiceStatus: confirm ? 1 : (val.invoiceStatus || 0),
        patientId: +val.patientId,
        isActive: this.id ? (loadedIsActive !== false) : true
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
    const net = +this.form.getRawValue().netAmount || 0;
    this.loading = true;
    this.masterService.updateInvoiceStatus(id, 2, 2, net).subscribe(
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
