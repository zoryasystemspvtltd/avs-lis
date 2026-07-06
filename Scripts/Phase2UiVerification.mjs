/**
 * Phase 2 - UI QA Certification (Playwright)
 * Validates sprint-modified screens load without console errors after login.
 */
import { chromium } from 'playwright';
import { writeFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const portal = process.env.PORTAL_URL || 'http://localhost:8080';
const loginUser = process.env.UAT_USER || 'admin@zorya.co.in';
const loginPass = process.env.UAT_PASS || 'zorKol@1';
const results = [];

function log(id, name, status, detail) {
  results.push({ id, name, status, detail });
  console.log(`[${status}] ${id} ${name} - ${detail}`);
}

const screens = [
  { id: 'UI-NAV-01', path: '/login', terms: ['Secure access'], auth: false },
  { id: 'UI-PAT-01', path: '/patient-master', terms: ['Patient'] },
  { id: 'UI-SINV-01', path: '/sale-invoices', terms: ['Sale Invoice', 'Invoice'] },
  { id: 'UI-SINV-02', path: '/sale-invoices/create', terms: ['Sale Invoice', 'Patient'] },
  { id: 'UI-TR-01', path: '/test-rates', terms: ['Test Rate', 'Rate Type'] },
  { id: 'UI-PARAM-01', path: '/his-parameter-ranges', terms: ['Parameter Range', 'Range Code'] },
  { id: 'UI-SC-01', path: '/sample-collection', terms: ['Collection', 'Pending'] },
  { id: 'UI-SR-01', path: '/sample-receiving', terms: ['Receiv', 'Sample'] },
  { id: 'UI-LRE-01', path: '/lab-result-entry', terms: ['Result', 'Lab'] },
  { id: 'UI-RAD-01', path: '/radiology-report-entry', terms: ['Radiology', 'Pending'] },
  { id: 'UI-RAD-02', path: '/radiology-doctor-approvals', terms: ['Doctor', 'Approval'] },
  { id: 'UI-RAD-03', path: '/radiology-approved-reports', terms: ['Approved', 'Released'] },
  { id: 'UI-RAD-04', path: '/reports/radiology-report', terms: ['Radiology', 'Print'] },
  { id: 'UI-RPT-01', path: '/reports/test-report', terms: ['Report', 'Diagnostic'] },
];

async function login(page) {
  await page.goto(`${portal}/login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.fill('#login-email', loginUser);
  await page.fill('#login-password', loginPass);
  await page.click('button[type="submit"]');
  await page.waitForFunction(() => !window.location.pathname.endsWith('/login'), null, { timeout: 30000 });
  await page.waitForTimeout(2000);
}

async function pageHaystack(page) {
  const body = await page.locator('body').innerText();
  const headings = await page.locator('h1, h2, h3, h4, .invoice-title').allTextContents();
  return (body + ' ' + headings.join(' ')).toLowerCase();
}

async function runBrowser(name, launchOpts) {
  const browser = await chromium.launch(launchOpts);
  const context = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await context.newPage();
  const consoleErrors = [];
  const pageErrors = [];
  page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
  page.on('pageerror', err => pageErrors.push(err.message));

  try {
    await login(page);
    log(`UI-${name}-LOGIN`, `${name} login`, 'PASS', 'Authenticated');

    for (const scr of screens.filter(s => s.auth !== false)) {
      const errorsBefore = consoleErrors.length;
      try {
        await page.goto(`${portal}${scr.path}`, { waitUntil: 'domcontentloaded', timeout: 45000 });
        await page.waitForSelector('h1, .invoice-title, app-sample-collection, app-radiology-report-entry', { timeout: 15000 }).catch(() => {});
        await page.waitForTimeout(2000);
        if (page.url().includes('/login')) {
          log(scr.id, scr.path, 'FAIL', 'Redirected to login (session/auth)');
          continue;
        }
        const hay = await pageHaystack(page);
        const textOk = scr.terms.some(t => hay.includes(t.toLowerCase()));
        const newErrs = consoleErrors.slice(errorsBefore).filter(e =>
          !e.includes('favicon') && !e.includes('404') && !e.includes('net::ERR')
        );
        if (!textOk) {
          log(scr.id, scr.path, 'FAIL', `Expected one of [${scr.terms.join(', ')}]; snippet=${hay.slice(0, 120)}`);
        } else if (newErrs.length) {
          log(scr.id, scr.path, 'FAIL', `Console errors: ${newErrs.slice(0, 2).join(' | ')}`);
        } else {
          log(scr.id, scr.path, 'PASS', 'Loaded; no critical console errors');
        }
      } catch (e) {
        log(scr.id, scr.path, 'FAIL', e.message);
      }
    }

    for (const w of [1280, 1024]) {
      await page.setViewportSize({ width: w, height: 800 });
      await page.goto(`${portal}/radiology-doctor-approvals`, { waitUntil: 'domcontentloaded', timeout: 45000 });
      await page.waitForTimeout(2000);
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 20);
      log(`UI-RESP-${w}`, `Responsive ${w}px`, overflow ? 'FAIL' : 'PASS', overflow ? 'Horizontal overflow' : 'No overflow');
    }

    log('UI-JS-ERR', 'Uncaught page errors', pageErrors.length ? 'FAIL' : 'PASS', pageErrors.length ? pageErrors.slice(0, 2).join(' | ') : 'None');
  } catch (e) {
    log(`UI-${name}-FATAL`, `${name} session`, 'FAIL', e.message);
  } finally {
    await browser.close();
  }
}

console.log('========== PHASE 2 UI QA ==========');
{
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  try {
    await page.goto(`${portal}/login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    const hay = await pageHaystack(page);
    log('UI-NAV-01', '/login', hay.includes('secure access') ? 'PASS' : 'FAIL', 'Login page render');
  } catch (e) {
    log('UI-NAV-01', '/login', 'FAIL', e.message);
  }
  await browser.close();
}

await runBrowser('CHROME', { headless: true, channel: 'chrome' }).catch(async () => {
  await runBrowser('CHROMIUM', { headless: true });
});

try {
  await runBrowser('EDGE', { headless: true, channel: 'msedge' });
} catch {
  log('UI-EDGE-SKIP', 'Microsoft Edge', 'INFO', 'Edge channel not available');
}

const fail = results.filter(r => r.status === 'FAIL');
const pass = results.filter(r => r.status === 'PASS');
console.log(`\n========== PHASE 2 SUMMARY: PASS=${pass.length} FAIL=${fail.length} ==========`);
writeFileSync(join(__dirname, 'Phase2UiVerification-results.json'), JSON.stringify(results, null, 2));
process.exit(fail.length > 0 ? 1 : 0);
