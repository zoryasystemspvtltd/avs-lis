import {
  ALL_ROLE_MENUS,
  buildRolePermissionSections,
  normalizeMenuKey,
  findMenuDef
} from './role-permission-catalog';
import { ROUTE_PERMISSION_RULES } from '../../_guards/permission.util';

describe('Report Template Configuration RBAC catalog', () => {
  it('registers menu under Masters/Setup catalog with correct route and module', () => {
    const item = ALL_ROLE_MENUS.find(m => m.menuKey === 'SETUP_REPORT_TEMPLATE_CONFIGURATION');
    expect(item).toBeDefined();
    expect(item.moduleName).toBe('ReportTemplateConfiguration');
    expect(item.section).toBe('Masters');
    expect(item.label).toBe('Report Template Configuration');
    expect(item.route).toBe('/report-template-configuration');
    expect(item.order).toBe(57);
  });

  it('does not create duplicate menu keys or routes', () => {
    const byKey = ALL_ROLE_MENUS.filter(m => m.menuKey === 'SETUP_REPORT_TEMPLATE_CONFIGURATION');
    const byRoute = ALL_ROLE_MENUS.filter(m => m.route === '/report-template-configuration');
    expect(byKey.length).toBe(1);
    expect(byRoute.length).toBe(1);
  });

  it('normalizes legacy setup.reportTemplateConfiguration key', () => {
    expect(normalizeMenuKey('setup.reportTemplateConfiguration')).toBe('SETUP_REPORT_TEMPLATE_CONFIGURATION');
    expect(findMenuDef('setup.reportTemplateConfiguration').menuKey).toBe('SETUP_REPORT_TEMPLATE_CONFIGURATION');
  });

  it('includes ReportTemplateConfiguration in Role Edit Masters section modules', () => {
    const sections = buildRolePermissionSections();
    const masters = sections.find(s => s.title === 'Masters');
    expect(masters).toBeDefined();
    expect(masters.moduleNames.indexOf('ReportTemplateConfiguration') >= 0).toBe(true);
    expect(masters.menus.some(m => m.menuKey === 'SETUP_REPORT_TEMPLATE_CONFIGURATION')).toBe(true);
  });

  it('guards list and designer routes with module + menuKey', () => {
    const list = ROUTE_PERMISSION_RULES.find(r => r.pattern.test('/report-template-configuration'));
    const design = ROUTE_PERMISSION_RULES.find(r => r.pattern.test('/report-template-configuration/design/12'));
    expect(list).toBeDefined();
    expect(list.modules).toEqual(['ReportTemplateConfiguration']);
    expect(list.menuKey).toBe('SETUP_REPORT_TEMPLATE_CONFIGURATION');
    expect(design).toBeDefined();
    expect(design.modules).toEqual(['ReportTemplateConfiguration']);
    expect(design.menuKey).toBe('SETUP_REPORT_TEMPLATE_CONFIGURATION');
  });

  it('does not grant template designer access via Reports module alone', () => {
    const rule = ROUTE_PERMISSION_RULES.find(r => r.pattern.test('/report-template-configuration'));
    expect(rule.modules.indexOf('Reports')).toBe(-1);
    expect(rule.modules.indexOf('RadiologyReports')).toBe(-1);
  });
});
