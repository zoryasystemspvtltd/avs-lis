/**
 * Lightweight certification checks for menu-permission overlay semantics (Node).
 * Mirrors permission.util hasMenuAccess / fallback rules.
 */
function hasModuleAccess(modules, module, access = 63) {
  const m = modules.find(x => x.name === module);
  if (!m) return false;
  return (m.access & access) === access;
}

function hasMenuAccess(modules, menus, module, menuKey, access = 63) {
  if (!hasModuleAccess(modules, module, access)) return false;
  if (!menuKey) return true;
  const moduleMenus = menus.filter(m => (m.moduleName || '').toLowerCase() === module.toLowerCase());
  if (!moduleMenus.length) return true; // fallback
  const row = moduleMenus.find(m => (m.menuKey || '').toLowerCase() === menuKey.toLowerCase());
  if (!row) return false;
  return (row.access & access) === access;
}

const fullMasters = [{ name: 'Masters', access: 63 }];
let pass = 0, fail = 0;
function assert(name, cond) {
  if (cond) { pass++; console.log('PASS', name); }
  else { fail++; console.log('FAIL', name); }
}

// Backward compat: full Masters, no menu rows
assert('BC: department visible with no overlay', hasMenuAccess(fullMasters, [], 'Masters', 'setup.department'));
assert('BC: specimen visible with no overlay', hasMenuAccess(fullMasters, [], 'Masters', 'masters.specimen'));

// Overlay: only department
const overlay = [{ menuKey: 'setup.department', moduleName: 'Masters', access: 63 }];
assert('Overlay: department allowed', hasMenuAccess(fullMasters, overlay, 'Masters', 'setup.department'));
assert('Overlay: specimen denied', !hasMenuAccess(fullMasters, overlay, 'Masters', 'masters.specimen'));

// Module denied
assert('No module: denied', !hasMenuAccess([], overlay, 'Masters', 'setup.department'));

// Other module overlay does not affect Masters fallback
const reportsOverlay = [{ menuKey: 'reports.diagnosticReport', moduleName: 'Reports', access: 63 }];
assert('Other-module overlay: Masters still fallback', hasMenuAccess(fullMasters, reportsOverlay, 'Masters', 'setup.unit'));

// Report Layout Configuration module (Administrator-only by seed)
const layoutModules = [{ name: 'ReportLayoutConfiguration', access: 63 }];
const layoutMenu = [{
  menuKey: 'SETUP_REPORT_LAYOUT_CONFIGURATION',
  moduleName: 'ReportLayoutConfiguration',
  access: 63
}];
assert('ReportLayout: admin module+menu allowed',
  hasMenuAccess(layoutModules, layoutMenu, 'ReportLayoutConfiguration', 'SETUP_REPORT_LAYOUT_CONFIGURATION'));
assert('ReportLayout: unauthorized role denied (no module)',
  !hasMenuAccess([], layoutMenu, 'ReportLayoutConfiguration', 'SETUP_REPORT_LAYOUT_CONFIGURATION'));
assert('ReportLayout: module without matching menu overlay denied',
  !hasMenuAccess(layoutModules, layoutMenu, 'ReportLayoutConfiguration', 'SETUP_NOTIFICATION_CONFIGURATION'));
assert('ReportLayout: module-only fallback when no overlay rows',
  hasMenuAccess(layoutModules, [], 'ReportLayoutConfiguration', 'SETUP_REPORT_LAYOUT_CONFIGURATION'));

console.log(`\nResult: ${pass} passed, ${fail} failed`);
process.exit(fail ? 1 : 0);
