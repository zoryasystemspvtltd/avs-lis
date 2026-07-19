// Direct-URL access audit for a role: which routes are reachable vs blocked.
// Usage: node ReceptionistUrlAudit.mjs [username] [password]
import { chromium } from 'playwright-core';

const BASE = 'http://localhost:8080';
const USER = process.argv[2] || 'recep1@zorya.co.in';
const PASS = process.argv[3] || 'zorKol@1';
const exe = process.env.EDGE_PATH || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';

const ROUTES = [
  // granted
  '/test-master', '/test-rates', '/referral-doctors', '/corporates',
  '/patient-master', '/sale-invoices', '/reports/sale-invoice-register',
  '/reports/test-report', '/reports/radiology-report',
  // not granted (should be blocked)
  '/samples', '/sample-collection', '/sample-receiving',
  '/technicianapprovals', '/edit-test-results', '/doctorapprovals',
  '/approvedsamples', '/rejectedsamples', '/quality-controls',
  '/departments', '/units', '/methods', '/equipments', '/equipment-heartbeat',
  '/test-profiles', '/specimens', '/his-parameters', '/test-parameters',
  '/test-mappings', '/his-parameter-ranges',
  '/users', '/roles', '/client-application',
  '/radiology-report-entry', '/radiology-doctor-approvals', '/radiology-approved-reports',
  // catalog-less master pages (URL-only)
  '/test-groups', '/test-categories', '/sample-types', '/containers'
];

async function main() {
  const browser = await chromium.launch({ executablePath: exe, headless: true });
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
  const page = await ctx.newPage();

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  const emailInput = await page.$('#login-email') || await page.$('input[formcontrolname="username"]');
  const passInput = await page.$('#login-password') || await page.$('input[formcontrolname="password"]');
  if (emailInput) { await emailInput.fill(USER); }
  if (passInput) { await passInput.fill(PASS); }
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'networkidle', timeout: 30000 }).catch(() => {}),
    page.click('button[type="submit"], input[type="submit"]')
  ]);
  // Wait until the session (with menu access) is persisted.
  try {
    await page.waitForFunction(() => {
      try {
        const u = JSON.parse(localStorage.getItem('currentUser') || 'null');
        return !!(u && u.accessToken && Array.isArray(u.access) && u.access.length);
      } catch { return false; }
    }, { timeout: 30000 });
  } catch (e) {
    const debug = await page.evaluate(() => ({
      url: location.href,
      body: (document.body.innerText || '').slice(0, 500),
      ls: localStorage.getItem('currentUser')
    }));
    console.error('LOGIN DID NOT COMPLETE. DEBUG:', JSON.stringify(debug, null, 2));
    throw e;
  }
  const session = await page.evaluate(() => {
    const u = JSON.parse(localStorage.getItem('currentUser'));
    return { userName: u.userName, modules: (u.access || []).length, menus: (u.menuAccess || []).length };
  });
  console.log('SESSION:', JSON.stringify(session));

  const rows = [];
  for (const route of ROUTES) {
    await page.goto(BASE + route, { waitUntil: 'networkidle', timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(800);
    const finalUrl = await page.evaluate(() => location.pathname + location.search);
    const blocked = finalUrl !== route && (finalUrl === '/' || finalUrl.indexOf('denied=1') >= 0 || finalUrl.indexOf('/login') === 0);
    rows.push({ route, finalUrl, result: blocked ? 'BLOCKED' : (finalUrl === route ? 'ALLOWED' : 'REDIRECT:' + finalUrl) });
  }

  for (const r of rows) {
    console.log(`${r.result.padEnd(10)} ${r.route}  ->  ${r.finalUrl}`);
  }
  await browser.close();
}

main().catch(e => { console.error('AUDIT FAILED:', e); process.exit(1); });
