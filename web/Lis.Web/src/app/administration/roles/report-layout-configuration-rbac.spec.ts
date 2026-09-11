import {
  ALL_ROLE_MENUS,
  buildRolePermissionSections,
  normalizeMenuKey,
  findMenuDef
} from './role-permission-catalog';
import { ROUTE_PERMISSION_RULES } from '../../_guards/permission.util';

describe('Report Layout Configuration RBAC catalog', () => {
  it('registers menu under Masters/Setup catalog with correct route and module', () => {
    const item = ALL_ROLE_MENUS.find(m => m.menuKey === 'SETUP_REPORT_LAYOUT_CONFIGURATION');
    expect(item).toBeDefined();
    expect(item.moduleName).toBe('ReportLayoutConfiguration');
    expect(item.section).toBe('Masters');
    expect(item.label).toBe('Report Layout Configuration');
    expect(item.route).toBe('/report-layout-configuration');
    expect(item.order).toBe(56);
  });

  it('does not create duplicate menu keys or routes', () => {
    const byKey = ALL_ROLE_MENUS.filter(m => m.menuKey === 'SETUP_REPORT_LAYOUT_CONFIGURATION');
    const byRoute = ALL_ROLE_MENUS.filter(m => m.route === '/report-layout-configuration');
    expect(byKey.length).toBe(1);
    expect(byRoute.length).toBe(1);
  });

  it('normalizes legacy setup.reportLayoutConfiguration key', () => {
    expect(normalizeMenuKey('setup.reportLayoutConfiguration')).toBe('SETUP_REPORT_LAYOUT_CONFIGURATION');
    expect(findMenuDef('setup.reportLayoutConfiguration').menuKey).toBe('SETUP_REPORT_LAYOUT_CONFIGURATION');
  });

  it('includes ReportLayoutConfiguration in Role Edit Masters section modules', () => {
    const sections = buildRolePermissionSections();
    const masters = sections.find(s => s.title === 'Masters');
    expect(masters).toBeDefined();
    expect(masters.moduleNames.indexOf('ReportLayoutConfiguration') >= 0).toBe(true);
    expect(masters.menus.some(m => m.menuKey === 'SETUP_REPORT_LAYOUT_CONFIGURATION')).toBe(true);
  });

  it('guards direct route /report-layout-configuration with module + menuKey', () => {
    const rule = ROUTE_PERMISSION_RULES.find(r => r.pattern.test('/report-layout-configuration'));
    expect(rule).toBeDefined();
    expect(rule.modules).toEqual(['ReportLayoutConfiguration']);
    expect(rule.menuKey).toBe('SETUP_REPORT_LAYOUT_CONFIGURATION');
  });
});
