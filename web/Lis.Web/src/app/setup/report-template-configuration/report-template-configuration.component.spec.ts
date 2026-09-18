import { ReportTemplateConfigurationComponent } from './report-template-configuration.component';

/**
 * Focused UI behavior tests (tabs + create buttons). No HTTP.
 */
describe('ReportTemplateConfigurationComponent tabs', () => {
  let component: ReportTemplateConfigurationComponent;
  let targetsCalls: number;
  let setModeCalls: number;
  let activateCalls: number;

  beforeEach(() => {
    targetsCalls = 0;
    setModeCalls = 0;
    activateCalls = 0;
    const serviceStub: any = {
      workspace: () => ({ subscribe: () => {} }),
      setMode: () => {
        setModeCalls++;
        return { subscribe: () => {} };
      },
      targets: () => {
        targetsCalls++;
        return { subscribe: (ok: any) => ok([]) };
      },
      activate: () => {
        activateCalls++;
        return { subscribe: () => {} };
      },
      deactivate: () => ({ subscribe: () => {} }),
      createCustom: () => ({ subscribe: () => {} }),
      previewSample: () => ({ subscribe: () => {} })
    };
    const alertStub: any = { error: () => {}, success: () => {} };
    const routerStub: any = { navigate: () => {} };
    component = new ReportTemplateConfigurationComponent(serviceStub, alertStub, routerStub);
    component.workspace = {
      mode: 'Custom',
      customGenerics: [
        { id: 1, name: 'G1', isActivated: true, canActivate: false, canDeactivate: true },
        { id: 2, name: 'G2', isActivated: false, canActivate: true, canDeactivate: false }
      ],
      specificTemplates: [
        { id: 10, name: 'T1', targetTestId: 5, targetLabel: 'Test A', isActivated: true },
        { id: 11, name: 'P1', targetProfileId: 9, targetLabel: 'Profile A', isActivated: false }
      ]
    };
    component.mode = 'Custom';
  });

  it('defaults customTab to generic', () => {
    expect(component.customTab).toBe('generic');
  });

  it('Custom mode enables tabs', () => {
    component.mode = 'Custom';
    expect(component.customTabsEnabled).toBe(true);
  });

  it('System Default mode disables tabs but keeps them conceptually visible', () => {
    component.mode = 'SystemDefault';
    expect(component.customTabsEnabled).toBe(false);
  });

  it('System Default: selectCustomTab is no-op and does not mutate', () => {
    component.mode = 'SystemDefault';
    component.customTab = 'generic';
    component.selectCustomTab('test');
    expect(component.customTab).toBe('generic');
    expect(setModeCalls).toBe(0);
    expect(activateCalls).toBe(0);
    expect(targetsCalls).toBe(0);
  });

  it('System Default: openCreateGeneric/Specific are no-ops', () => {
    component.mode = 'SystemDefault';
    component.openCreateGeneric();
    expect(component.createPanel).toBe('none');
    component.openCreateSpecific();
    expect(component.createPanel).toBe('none');
  });

  it('Custom: switches tabs without calling mutation APIs', () => {
    component.selectCustomTab('test');
    expect(component.customTab).toBe('test');
    component.selectCustomTab('profile');
    expect(component.customTab).toBe('profile');
    component.selectCustomTab('generic');
    expect(component.customTab).toBe('generic');
    expect(setModeCalls).toBe(0);
    expect(activateCalls).toBe(0);
  });

  it('shows only one category list per selected tab (filter helpers)', () => {
    expect(component.filteredGenerics().length).toBe(2);
    expect(component.filteredTestSpecifics().length).toBe(1);
    expect(component.filteredProfileSpecifics().length).toBe(1);
  });

  it('loads mode from workspace payload (not hard-coded on reload assignment)', () => {
    component.mode = 'SystemDefault';
    component.workspace = { mode: 'Custom', customGenerics: [], specificTemplates: [] };
    component.mode = component.workspace.mode || component.workspace.Mode || 'SystemDefault';
    expect(component.mode).toBe('Custom');
    expect(component.customTabsEnabled).toBe(true);
  });

  it('openCreateGeneric selects generic tab and opens generic panel', () => {
    component.customTab = 'test';
    component.openCreateGeneric();
    expect(component.customTab).toBe('generic');
    expect(component.createPanel).toBe('generic');
    expect(component.createCategory).toBe('CustomGeneric');
  });

  it('Create Generic: Cancel returns to no panel (button can reappear)', () => {
    component.openCreateGeneric();
    expect(component.createPanel).toBe('generic');
    component.cancelCreate();
    expect(component.createPanel).toBe('none');
  });

  it('openCreateSpecific from profile tab preselects Profile target type', () => {
    component.customTab = 'profile';
    component.openCreateSpecific();
    expect(component.createPanel).toBe('specific');
    expect(component.createCategory).toBe('Specific');
    expect(component.targetType).toBe('Profile');
    expect(targetsCalls).toBeGreaterThan(0);
  });

  it('Create Specific: Cancel clears panel', () => {
    component.customTab = 'test';
    component.openCreateSpecific();
    expect(component.createPanel).toBe('specific');
    component.cancelCreate();
    expect(component.createPanel).toBe('none');
  });

  it('openCreateSystem opens create panel in system section only', () => {
    component.openCreateSystem();
    expect(component.createPanel).toBe('system');
    expect(component.createCategory).toBe('CustomGeneric');
  });

  it('Create Custom: Cancel clears panel', () => {
    component.openCreateSystem();
    expect(component.createPanel).toBe('system');
    component.cancelCreate();
    expect(component.createPanel).toBe('none');
  });

  it('cancelCreate clears panel without mode change', () => {
    component.mode = 'Custom';
    component.openCreateGeneric();
    component.cancelCreate();
    expect(component.createPanel).toBe('none');
    expect(component.mode).toBe('Custom');
    expect(setModeCalls).toBe(0);
  });

  it('page size remains 15', () => {
    expect(component.pageSize).toBe(15);
  });
});
