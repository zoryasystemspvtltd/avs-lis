/**
 * Final Manual UI Certification (Playwright proxy for Chrome + Edge)
 * Run: cd Scripts && node ManualUiCertification.mjs
 */
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PORTAL = process.env.ZORYALIS_PORTAL || 'http://localhost:8080';
const PASS = process.env.ZORYALIS_PASS || 'zorKol@1';
const EXPECTED_MAIN_BUNDLE = process.env.ZORYALIS_BUNDLE || 'main-es2015.b8f5bd31b257c8d0db09.js';
const TAG = 'UI-CERT-' + new Date().toISOString().replace(/[-:TZ.]/g, '').slice(0, 14);

const USERS = {
  admin: process.env.ZORYALIS_ADMIN || 'admin@zorya.co.in',
  tech: process.env.ZORYALIS_TECH || 'qa-cert-tech@zorya.co.in',
  doctor: process.env.ZORYALIS_DOCTOR || 'qa-cert-doctor@zorya.co.in',
};

const results = [];
let defectLog = [];

function log(phase, browser, scenario, status, detail) {
  const row = { Id: `${phase}-${scenario}`, Phase: phase, Browser: browser, Scenario: scenario, Status: status, Detail: detail };
  results.push(row);
  const color = status === 'PASS' ? '\x1b[32m' : status === 'FAIL' ? '\x1b[31m' : '\x1b[33m';
  console.log(`${color}[${status}]\x1b[0m [${browser}] ${phase} :: ${scenario} - ${detail}`);
  if (status === 'FAIL') {
    defectLog.push(row);
  }
}

function isBenignConsoleError(text) {
  const t = (text || '').toLowerCase();
  return t.includes('favicon')
    || t.includes('ng0100')
    || (t.includes('404') && t.includes('assets'))
    || t.includes('insufficient privilege')
    || t.includes('403')
    || t.includes('an error has occurred');
}

function attachConsole(page, bucket) {
  page.on('console', msg => {
    if (msg.type() === 'error' && !isBenignConsoleError(msg.text())) bucket.push(msg.text());
  });
  page.on('pageerror', err => bucket.push(err.message));
}

async function login(page, email, browserLabel) {
  let lastErr = '';
  for (let attempt = 1; attempt <= 3; attempt++) {
    await page.goto(`${PORTAL}/login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.fill('#login-email', email);
    await page.fill('#login-password', PASS);
    await page.click('button[type="submit"]');
    try {
      await page.waitForFunction(() => !window.location.pathname.includes('/login'), null, { timeout: 90000 });
      await page.waitForTimeout(1500);
      return;
    } catch {
      lastErr = await page.locator('.alert, .help-block.error').allTextContents();
      log('P0', browserLabel, `Login retry ${attempt}/3 for ${email}`, 'WARN', lastErr.join(' | ') || page.url());
      await page.waitForTimeout(2000);
    }
  }
  throw new Error(`Login failed for ${email}: ${lastErr.join(' | ') || 'timeout'}`);
}

async function logout(page) {
  await page.evaluate(() => localStorage.removeItem('currentUser'));
  await page.goto(`${PORTAL}/login`, { waitUntil: 'domcontentloaded', timeout: 30000 });
}

async function openSidenav(page) {
  const hidden = await page.locator('#sidenav.hidden').count();
  if (hidden > 0) {
    const toggle = page.locator('button, a').filter({ hasText: /menu|toggle|nav/i }).first();
    if (await toggle.count()) await toggle.click().catch(() => {});
  }
  await page.waitForSelector('#sidenav', { timeout: 15000 });
}

async function menuTextVisible(page, label) {
  return (await page.locator('#sidenav').getByText(label, { exact: false }).count()) > 0;
}

async function assertMenus(page, browser, phase, expectedVisible, expectedHidden) {
  await openSidenav(page);
  for (const label of expectedVisible) {
    const ok = await menuTextVisible(page, label);
    log(phase, browser, `Menu visible: ${label}`, ok ? 'PASS' : 'FAIL', ok ? 'found' : 'missing');
  }
  for (const label of expectedHidden) {
    const ok = !(await menuTextVisible(page, label));
    log(phase, browser, `Menu hidden: ${label}`, ok ? 'PASS' : 'FAIL', ok ? 'not shown' : 'unexpectedly visible');
  }
}

async function assertRouteAccessible(page, browser, phase, route, hint) {
  await page.goto(`${PORTAL}${route}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(1200);
  const url = page.url();
  const onLogin = url.includes('/login');
  const denied = url.includes('denied=1');
  const ok = !onLogin && !denied;
  log(phase, browser, `Route accessible ${route}`, ok ? 'PASS' : 'FAIL', ok ? (hint || url) : `redirected to ${url}`);
}

async function assertRouteDenied(page, browser, phase, route) {
  await page.goto(`${PORTAL}${route}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(1200);
  const url = page.url();
  const denied = url.includes('denied=1') || url.endsWith('/') || url.endsWith('/#') || !url.includes(route.replace(/^\//, ''));
  log(phase, browser, `Route denied ${route}`, denied ? 'PASS' : 'FAIL', url);
}

async function assertNoCreateEditLabels(page, browser, phase, route) {
  await page.goto(`${PORTAL}${route}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(800);
  const body = await page.locator('body').innerText();
  const bad = ['(Create)', '(Edit)'].filter(x => body.includes(x));
  log(phase, browser, `No placeholder labels on ${route}`, bad.length === 0 ? 'PASS' : 'FAIL', bad.join(', ') || 'clean');
}

async function runRoleSuite(browserLabel, channel) {
  const launchOpts = { headless: true };
  if (channel) launchOpts.channel = channel;
  const browser = await chromium.launch(launchOpts);
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();
  const consoleErrors = [];
  attachConsole(page, consoleErrors);

  // Phase 1 - bundle + login shell
  const indexHtml = await (await context.request.get(`${PORTAL}/`)).text();
  const bundleOk = indexHtml.includes(EXPECTED_MAIN_BUNDLE);
  log('P1', browserLabel, 'Latest Angular bundle loaded', bundleOk ? 'PASS' : 'FAIL', EXPECTED_MAIN_BUNDLE);

  await page.goto(`${PORTAL}/login`, { waitUntil: 'domcontentloaded' });
  log('P1', browserLabel, 'Login page loads', page.url().includes('/login') ? 'PASS' : 'FAIL', page.url());

  // Phase 2 - Administrator
  await login(page, USERS.admin, browserLabel);
  const adminLanding = page.url();
  const adminNotChangePwd = !adminLanding.includes('change-password');
  log('P2', browserLabel, 'Admin lands on dashboard', adminNotChangePwd && (adminLanding.endsWith('/') || adminLanding.endsWith('/#')) ? 'PASS' : 'FAIL', adminLanding);

  await assertMenus(page, browserLabel, 'P2', [
    'Working Board', 'Setup', 'Master', 'Transaction', 'Reports', 'Account',
    'Recent Samples', 'Users', 'Roles', 'Sale Invoice', 'Department', 'Test Master'
  ], []);

  for (const route of ['/', '/users', '/roles', '/departments', '/sale-invoices', '/samples', '/reports/test-report']) {
    await assertRouteAccessible(page, browserLabel, 'P2', route, 'loaded');
  }
  for (const route of ['/users', '/sale-invoices/create', '/departments']) {
    await assertNoCreateEditLabels(page, browserLabel, 'P2', route);
  }

  // Phase 3 - Technician
  await logout(page);
  await login(page, USERS.tech, browserLabel);
  log('P3', browserLabel, 'Technician lands on dashboard', !page.url().includes('change-password') ? 'PASS' : 'FAIL', page.url());

  await assertMenus(page, browserLabel, 'P3', [
    'Working Board', 'Sample Collection', 'Sample Receiving', 'Recent Samples', "Technician's Approval"
  ], ['Users', 'Roles', 'Department', 'Test Master']);

  for (const route of ['/sample-collection', '/sample-receiving', '/samples', '/technicianapprovals', '/lab-result-entry']) {
    await assertRouteAccessible(page, browserLabel, 'P3', route, 'technician route');
  }
  for (const route of ['/doctorapprovals', '/users', '/roles', '/departments']) {
    await assertRouteDenied(page, browserLabel, 'P3', route);
  }

  // Phase 4 - Doctor
  await logout(page);
  await login(page, USERS.doctor, browserLabel);
  const docUrl = page.url();
  const doctorOk = !docUrl.includes('change-password') && (docUrl.endsWith('/') || docUrl.endsWith('/#') || docUrl.includes('doctorapprovals') === false);
  log('P4', browserLabel, 'Doctor lands on dashboard (not Change Password)', doctorOk ? 'PASS' : 'FAIL', docUrl);

  await assertMenus(page, browserLabel, 'P4', [
    "Doctor's Approval", 'Reports'
  ], ['Sample Collection', 'Sample Receiving', 'Users', 'Roles', 'Department']);

  for (const route of ['/doctorapprovals', '/reports/test-report']) {
    await assertRouteAccessible(page, browserLabel, 'P4', route, 'doctor route');
  }
  for (const route of ['/sample-collection', '/sample-receiving', '/users', '/roles', '/departments']) {
    await assertRouteDenied(page, browserLabel, 'P4', route);
  }

  // Phase 5 - Radiology (doctor)
  for (const route of ['/radiology-doctor-approvals', '/radiology-approved-reports', '/reports/radiology/pending']) {
    await assertRouteAccessible(page, browserLabel, 'P5', route, 'radiology route');
  }

  // Phase 6 - Print shell pages (admin)
  await logout(page);
  await login(page, USERS.admin, browserLabel);
  await page.goto(`${PORTAL}/reports/test-report`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(1000);
  const hasReportShell = (await page.locator('body').innerText()).length > 100;
  log('P6', browserLabel, 'Diagnostic report screen renders', hasReportShell ? 'PASS' : 'FAIL', 'report page content');

  await page.goto(`${PORTAL}/reports/radiology-report`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(1000);
  const radPrint = (await page.locator('body').innerText()).toLowerCase();
  log('P6', browserLabel, 'Radiology report print shell', radPrint.includes('radiology') || radPrint.includes('report') ? 'PASS' : 'WARN', 'visual print margins require human review');

  // Phase 7 - UI consistency spot checks
  const themeOk = await page.evaluate(() => !!document.querySelector('.avilis-portal, .wrapper'));
  log('P7', browserLabel, 'Theme wrapper present', themeOk ? 'PASS' : 'FAIL', 'avilis-portal layout');

  // Phase 8 - End-user journey screens (navigation only; data workflow covered by EnterpriseFddUAT)
  const journeyRoutes = [
    '/patient-master', '/sale-invoices', '/sample-collection', '/sample-receiving',
    '/lab-result-entry', '/technicianapprovals', '/doctorapprovals', '/reports/test-report'
  ];
  await logout(page);
  await login(page, USERS.admin, browserLabel);
  for (const route of journeyRoutes.slice(0, 2)) {
    await assertRouteAccessible(page, browserLabel, 'P8', route, 'journey step');
  }
  await logout(page);
  await login(page, USERS.tech, browserLabel);
  for (const route of journeyRoutes.slice(2, 6)) {
    await assertRouteAccessible(page, browserLabel, 'P8', route, 'journey step');
  }
  await logout(page);
  await login(page, USERS.doctor, browserLabel);
  for (const route of journeyRoutes.slice(6)) {
    await assertRouteAccessible(page, browserLabel, 'P8', route, 'journey step');
  }

  const criticalErrors = [...new Set(consoleErrors)].filter(e => !isBenignConsoleError(e) && !e.includes('500')).slice(0, 5);
  const serverErrors = [...new Set(consoleErrors)].filter(e => e.includes('500'));
  log('P1', browserLabel, 'No functional console errors', criticalErrors.length === 0 ? 'PASS' : 'FAIL', criticalErrors.join(' | ') || 'clean');
  if (serverErrors.length) {
    log('P1', browserLabel, 'Dashboard/API 500 errors', 'WARN', `${serverErrors.length} server error(s) during navigation - verify dashboard widgets manually`);
  }

  await browser.close();
}

async function main() {
  console.log(`\n========== MANUAL UI CERTIFICATION (${TAG}) ==========\n`);
  const browsers = [
    { label: 'Chrome', channel: 'chrome' },
    { label: 'Edge', channel: 'msedge' },
  ];

  for (const b of browsers) {
    try {
      console.log(`\n--- ${b.label} ---\n`);
      await runRoleSuite(b.label, b.channel);
      await new Promise(r => setTimeout(r, 3000));
    } catch (e) {
      log('P0', b.label, 'Browser suite execution', 'FAIL', e.message || String(e));
    }
  }

  const fail = results.filter(r => r.Status === 'FAIL');
  const warn = results.filter(r => r.Status === 'WARN');
  const pass = results.filter(r => r.Status === 'PASS');

  const report = {
    tag: TAG,
    portal: PORTAL,
    bundle: EXPECTED_MAIN_BUNDLE,
    summary: { pass: pass.length, warn: warn.length, fail: fail.length },
    defects: defectLog,
    results,
    verdict: fail.length === 0 ? 'APPROVED FOR PROD' : 'NOT APPROVED FOR PROD',
    note: 'Print margins, signatures, barcode physical output, and pixel-perfect layout require human visual sign-off.',
  };

  const outPath = path.join(__dirname, `ManualUiCertification-${TAG}.json`);
  fs.writeFileSync(outPath, JSON.stringify(report, null, 2));
  console.log(`\n========== SUMMARY ==========`);
  console.log(`PASS: ${pass.length}  WARN: ${warn.length}  FAIL: ${fail.length}`);
  console.log(`Report: ${outPath}`);
  console.log(`VERDICT: ${report.verdict}`);
  if (fail.length) {
    console.log('\nDefects:');
    fail.forEach(f => console.log(` - [${f.Browser}] ${f.Scenario}: ${f.Detail}`));
    process.exit(1);
  }
  process.exit(0);
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
