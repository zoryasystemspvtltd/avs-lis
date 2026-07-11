import { Component, OnInit } from '@angular/core';
import { AuthenticationService, AlertService, UserService } from '../../../_services';
import { ActivatedRoute, Router } from '@angular/router';
import { FormGroup, Validators, FormBuilder } from '@angular/forms';
import {
  buildRolePermissionSections,
  normalizeMenuKey,
  RolePermissionSection
} from '../role-permission-catalog';

@Component({
  selector: 'app-roles-edit',
  templateUrl: './roles-edit.component.html',
  styleUrls: ['./roles-edit.component.css']
})
export class RolesEditComponent implements OnInit {

  submitted = false;
  id: string;
  item: any;
  private sub: any;
  public isLoaded: Boolean;
  editRoleForm: FormGroup;
  loading = false;
  public message: string;
  selectedApplicationName: string;
  selectedApplicationId: any;

  sections: RolePermissionSection[] = buildRolePermissionSections();
  expandedSections: { [id: string]: boolean } = {};
  /** menuKey → enabled */
  menuEnabled: { [menuKey: string]: boolean } = {};

  constructor(
    private userService: UserService,
    private authenticationService: AuthenticationService,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private alertService: AlertService,
    private router: Router) { }

  ngOnInit() {
    this.sub = this.route.params.subscribe(params => {
      this.isLoaded = false;
      this.id = params['id'];
      this.sections.forEach(s => this.expandedSections[s.id] = true);
      this.getUserApps();
    });
  }

  initForms() {
    // Keep rolePermission on the form (same array reference) so Save posts checkbox state.
    this.editRoleForm = this.formBuilder.group({
      id: [this.item.id, Validators.required],
      name: [this.item.name, Validators.required],
      rolePermission: [this.item.rolePermission]
    });
  }

  get f() { return this.editRoleForm.controls; }

  getItemDetails(id: string) {
    this.userService.getRoleById(id)
      .subscribe(response => {
        this.item = (response && response.items && response.items[0])
          ? response.items[0]
          : { id: id, name: '', rolePermission: [] };
        if (!this.item.id && this.item.Id) {
          this.item.id = this.item.Id;
        }
        if (!this.item.name && this.item.Name) {
          this.item.name = this.item.Name;
        }
        if (!this.item.rolePermission) {
          this.item.rolePermission = this.item.RolePermission || [];
        }
        // Normalize permission flags to booleans for checkboxes.
        this.item.rolePermission = (this.item.rolePermission || []).map((p: any) => ({
          id: p.id != null ? p.id : p.Id,
          name: p.name || p.Name,
          url: p.url || p.Url,
          canView: !!(p.canView || p.CanView),
          canAdd: !!(p.canAdd || p.CanAdd),
          canEdit: !!(p.canEdit || p.CanEdit),
          canDelete: !!(p.canDelete || p.CanDelete),
          canAuthorize: !!(p.canAuthorize || p.CanAuthorize),
          canReject: !!(p.canReject || p.CanReject),
          applicationId: p.applicationId != null ? p.applicationId : p.ApplicationId
        }));
        this.hydrateMenuState(
          this.item.roleMenuPermission || this.item.RoleMenuPermission || []
        );
        this.initForms();
        this.isLoaded = true;
      }, (error) => {
        this.loading = false;
        this.message = this.readError(error, 'Unable to load role permissions.');
        this.alertService.error(this.message);
        this.item = { id: id, name: '', rolePermission: [] };
        this.hydrateMenuState([]);
        this.initForms();
        this.isLoaded = true;
      });
  }

  /**
   * Overlay rules:
   * - No active overlay rows (all canView false / empty granted) → module-only behaviour.
   * - API returns full catalog with flags; only menus with any Can* bit count as granted.
   */
  private hydrateMenuState(apiMenus: any[]) {
    this.menuEnabled = {};
    const granted = new Set<string>();
    const list = apiMenus || [];
    list.forEach(m => {
      const key = normalizeMenuKey(m.menuKey || m.MenuKey);
      const on = !!(m.canView || m.CanView || m.canAdd || m.CanAdd || m.canEdit || m.CanEdit
        || m.canDelete || m.CanDelete || m.canAuthorize || m.CanAuthorize || m.canReject || m.CanReject);
      if (on && key) {
        granted.add(key);
      }
    });

    // Overlay is active only when at least one menu was explicitly granted in DB.
    const hasOverlay = granted.size > 0;

    this.sections.forEach(section => {
      section.menus.forEach(menu => {
        const moduleOn = this.isModuleEnabled(menu.moduleName);
        if (!moduleOn) {
          this.menuEnabled[menu.menuKey] = false;
        } else if (!hasOverlay) {
          this.menuEnabled[menu.menuKey] = true;
        } else {
          this.menuEnabled[menu.menuKey] = granted.has(menu.menuKey);
        }
      });
    });
  }

  hasAccess(): boolean {
    return true;
  }

  isInValid(field: string) {
    if (this.submitted) {
      if (this.f[field].errors && this.f[field].errors.required) {
        return true;
      }
    }
    return false;
  }

  findModulePerm(moduleName: string): any {
    if (!this.item || !this.item.rolePermission) {
      return null;
    }
    return this.item.rolePermission.find((p: any) => p.name === moduleName) || null;
  }

  isModuleEnabled(moduleName: string): boolean {
    const perm = this.findModulePerm(moduleName);
    if (!perm) {
      return false;
    }
    return !!(perm.canView || perm.canAdd || perm.canEdit || perm.canDelete || perm.canAuthorize || perm.canReject);
  }

  isSectionModulesEnabled(section: RolePermissionSection): boolean {
    if (!section.moduleNames.length) {
      return false;
    }
    return section.moduleNames.every(m => this.isModuleEnabled(m));
  }

  setModuleEnabled(moduleName: string, enabled: boolean) {
    const perm = this.findModulePerm(moduleName);
    if (!perm) {
      return;
    }
    this.selectAll(perm, enabled);

    this.sections.forEach(section => {
      section.menus.filter(m => m.moduleName === moduleName).forEach(menu => {
        this.menuEnabled[menu.menuKey] = enabled;
      });
    });
  }

  onSectionModuleToggle(section: RolePermissionSection, event: any) {
    const enabled = !!(event && event.target && event.target.checked);
    section.moduleNames.forEach(m => this.setModuleEnabled(m, enabled));
  }

  isMenuEnabled(menuKey: string): boolean {
    return !!this.menuEnabled[menuKey];
  }

  onMenuToggle(menuKey: string, moduleName: string, event: any) {
    const enabled = !!(event && event.target && event.target.checked);
    this.menuEnabled[menuKey] = enabled;
    if (enabled && !this.isModuleEnabled(moduleName)) {
      this.setModuleEnabled(moduleName, true);
      this.menuEnabled[menuKey] = true;
    }
  }

  selectAllMenusInSection(section: RolePermissionSection, enabled: boolean) {
    if (enabled) {
      section.moduleNames.forEach(m => this.setModuleEnabled(m, true));
    }
    section.menus.forEach(menu => {
      if (!enabled) {
        this.menuEnabled[menu.menuKey] = false;
      } else if (this.isModuleEnabled(menu.moduleName)) {
        this.menuEnabled[menu.menuKey] = true;
      }
    });
  }

  /** Row-wise: set all CRUDAR flags on one module row. */
  selectAll(element: any, status: boolean) {
    if (!element) {
      return;
    }
    element.canAdd = status;
    element.canEdit = status;
    element.canAuthorize = status;
    element.canReject = status;
    element.canDelete = status;
    element.canView = status;
  }

  isAllSelected(element: any): boolean {
    return !!(element && element.canAdd && element.canEdit && element.canAuthorize
      && element.canReject && element.canDelete && element.canView);
  }

  isNoneSelected(element: any): boolean {
    return !!(element && (element.canAdd || element.canEdit || element.canAuthorize
      || element.canReject || element.canDelete || element.canView));
  }

  onCheckRole(event: any, element: any, type: number) {
    const checked = !!(event && event.target && event.target.checked);
    switch (type) {
      case 1: element.canAdd = checked; break;
      case 2: element.canEdit = checked; break;
      case 4: element.canAuthorize = checked; break;
      case 8: element.canReject = checked; break;
      case 16: element.canDelete = checked; break;
      case 32: element.canView = checked; break;
    }
  }

  /** Table-level select/deselect for Other Modules. */
  selectAllOtherModules(status: boolean) {
    (this.item.rolePermission || []).forEach((perm: any) => this.selectAll(perm, status));
    // Keep menu tree in sync with module flags.
    this.sections.forEach(section => {
      section.moduleNames.forEach(m => {
        const perm = this.findModulePerm(m);
        if (perm) {
          section.menus.filter(menu => menu.moduleName === m).forEach(menu => {
            this.menuEnabled[menu.menuKey] = status && this.isModuleEnabled(m);
          });
        }
      });
    });
  }

  toggleSection(sectionId: string) {
    this.expandedSections[sectionId] = !this.expandedSections[sectionId];
  }

  isExpanded(sectionId: string): boolean {
    return !!this.expandedSections[sectionId];
  }

  canEditMenus(moduleName: string): boolean {
    return this.isModuleEnabled(moduleName);
  }

  private buildMenuPayload(): any[] {
    let expectedUnderEnabled = 0;
    let checkedUnderEnabled = 0;
    const checked: any[] = [];
    const appId = this.resolveAppId();

    this.sections.forEach(section => {
      section.menus.forEach(menu => {
        if (!this.isModuleEnabled(menu.moduleName)) {
          return;
        }
        expectedUnderEnabled++;
        if (this.menuEnabled[menu.menuKey]) {
          checkedUnderEnabled++;
          checked.push({
            menuKey: menu.menuKey,
            moduleName: menu.moduleName,
            label: menu.label,
            route: menu.route,
            canView: true,
            canAdd: true,
            canEdit: true,
            canDelete: true,
            canAuthorize: true,
            canReject: true,
            applicationId: appId
          });
        }
      });
    });

    if (expectedUnderEnabled > 0 && checkedUnderEnabled === expectedUnderEnabled) {
      return [];
    }
    return checked;
  }

  private resolveAppId(): number {
    const n = parseInt(this.selectedApplicationId as any, 10);
    return isNaN(n) ? this.selectedApplicationId : n;
  }

  private readError(error: any, fallback: string): string {
    if (!error) {
      return fallback;
    }
    if (typeof error === 'string') {
      return error;
    }
    if (error.message) {
      return error.message;
    }
    if (error.error) {
      return typeof error.error === 'string' ? error.error : fallback;
    }
    return fallback;
  }

  onSubmit() {
    this.submitted = true;
    if (this.editRoleForm.invalid) {
      this.alertService.error('Please enter a valid role name.');
      return;
    }

    const appId = this.resolveAppId();
    const permissions = (this.item.rolePermission || []).map((per: any) => ({
      id: per.id,
      name: per.name,
      url: per.url,
      canView: !!per.canView,
      canAdd: !!per.canAdd,
      canEdit: !!per.canEdit,
      canDelete: !!per.canDelete,
      canAuthorize: !!per.canAuthorize,
      canReject: !!per.canReject,
      applicationId: appId
    }));

    // Keep form control in sync for consistency with legacy save path.
    this.editRoleForm.patchValue({ rolePermission: permissions });

    const item = {
      id: this.editRoleForm.value.id || this.id,
      name: this.editRoleForm.value.name,
      rolePermission: permissions,
      roleMenuPermission: this.buildMenuPayload()
    };

    this.loading = true;
    this.userService.editRole(item)
      .subscribe(() => {
        this.loading = false;
        this.alertService.success('Role permissions saved.');
        this.router.navigate(['/roles']);
      },
        (error) => {
          this.loading = false;
          this.message = this.readError(error, 'Data not saved.');
          this.alertService.error(this.message);
        });
  }

  getUserApps() {
    this.authenticationService.getUserApps().subscribe(val => {
      const app = (val || []).find(a => a.accessKey == this.authenticationService.selectedApplication);
      if (app == null) {
        this.router.navigate(['/']);
        return;
      }
      this.selectedApplicationName = app.name;
      this.selectedApplicationId = app.id;
      this.getItemDetails(this.id);
    }, () => {
      this.alertService.error('Unable to load applications.');
      this.isLoaded = true;
    });
  }
}
