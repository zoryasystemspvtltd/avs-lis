/**
 * Browser UAT: Sale Invoice item search UX (Playwright)
 * Run: cd Scripts && npm install playwright@1.49.0 --no-save && node SaleInvoiceBrowserUAT.cjs
 */
const { chromium } = require('playwright');

const PORTAL = process.env.ZORYALIS_PORTAL || 'http://localhost:8080';
const USER = process.env.ZORYALIS_USER || 'admin@zorya.co.in';
const PASS = process.env.ZORYALIS_PASS || 'zorKol@1';

const results = [];
function gate(area, test, pass, detail) {
  const status = pass ? 'PASS' : 'FAIL';
  results.push({ area, test, status, detail });
  console.log(`[${status}] ${area} :: ${test} - ${detail}`);
  if (!pass) throw new Error(`BROWSER UAT FAILED: ${area} :: ${test}`);
}

async function openSaleInvoiceCreate(page) {
  const menuBtn = page.locator('button[title="Open Left Menu"]');
  if (await menuBtn.count() > 0) {
    await menuBtn.click();
    await page.waitForTimeout(400);
  }
  const txn = page.locator('a[href="#collapseTransaction"]');
  if (await txn.count() > 0) {
    await txn.click();
    await page.waitForTimeout(400);
  }
  const saleLink = page.locator('a[routerlink="/sale-invoices"], a[routerLink="/sale-invoices"]');
  await saleLink.first().click({ force: true });
  await page.waitForTimeout(2000);
  const createBtn = page.locator('a:has-text("Create New")');
  if (await createBtn.count() > 0) {
    await createBtn.first().click({ force: true });
    await page.waitForTimeout(2500);
  }
  return page.url();
}

async function main() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  const consoleErrors = [];
  page.on('console', msg => {
    if (msg.type() === 'error') consoleErrors.push(msg.text());
  });
  page.on('pageerror', err => consoleErrors.push(err.message));

  await page.goto(`${PORTAL}/login`, { waitUntil: 'networkidle', timeout: 60000 });
  gate('UI', 'Login page loads', page.url().includes('/login'), page.url());

  await page.fill('#login-email', USER);
  await page.fill('#login-password', PASS);
  await page.click('button[type="submit"]');
  await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 60000 });
  await page.waitForFunction(() => {
    try {
      const u = JSON.parse(localStorage.getItem('currentUser') || 'null');
      return u && u.accessToken;
    } catch { return false; }
  }, { timeout: 30000 });
  gate('UI', 'Login succeeds', true, page.url());

  const finalUrl = await openSaleInvoiceCreate(page);
  const url = page.url();
  const bodySnippet = ((await page.locator('body').innerText()) || '').substring(0, 250).replace(/\s+/g, ' ');
  const table = page.locator('table.invoice-lines-table');
  const addBtn = page.getByRole('button', { name: /Add Test/i });
  const found = await table.count() > 0 || await addBtn.count() > 0;
  gate('UI', 'Sale invoice form visible', found, `url=${url} nav=${finalUrl} body=${bodySnippet}`);
  if (!found) throw new Error('Sale invoice form not found');

  const itemType = page.locator('tbody tr').first().locator('select[formcontrolname="itemType"]');
  gate('UI', 'Item Type control enabled', await itemType.isEnabled(), 'test/profile select');

  const itemSelect = page.locator('.sale-invoice-test-select').first();
  await itemSelect.click();
  const billableResponse = page.waitForResponse(
    r => r.url().includes('/SaleInvoice/BillableItems') && r.status() === 200,
    { timeout: 20000 }
  );
  await page.locator('.sale-invoice-test-select input').first().fill('CBC');
  await page.waitForTimeout(600);
  const apiResp = await billableResponse.catch(() => null);
  gate('UI', 'BillableItems API from portal', !!apiResp, apiResp ? '200 OK' : 'no response');

  await page.waitForSelector('.ng-option', { timeout: 15000 });
  const options = await page.locator('.ng-option:not(.ng-option-disabled)').count();
  gate('UI', 'Test search returns options', options > 0, `options=${options}`);

  await page.locator('.ng-option:not(.ng-option-disabled)').first().click();
  await page.waitForTimeout(500);
  const deptText = await page.locator('.department-readonly').first().textContent();
  gate('UI', 'Department auto-displayed', deptText && deptText.trim() !== '—' && deptText.trim().length > 0, `dept=${(deptText || '').trim()}`);

  await page.locator('tbody tr').first().locator('select[formcontrolname="itemType"]').selectOption('profile');
  await page.waitForTimeout(300);
  await page.locator('.sale-invoice-test-select').first().click();
  await page.locator('.sale-invoice-test-select input').first().fill('Diab');
  await page.waitForTimeout(600);
  await page.waitForSelector('.ng-option', { timeout: 15000 });
  const profOpts = await page.locator('.ng-option:not(.ng-option-disabled)').count();
  gate('UI', 'Profile search returns options', profOpts > 0, `options=${profOpts}`);

  const criticalErrors = consoleErrors.filter(e =>
    !e.includes('favicon') && !e.includes('404') && !e.includes('NG0100'));
  gate('UI', 'No critical console errors', criticalErrors.length === 0, criticalErrors.slice(0, 3).join(' | ') || 'clean');

  await browser.close();
  console.log(`\nALL BROWSER UAT CHECKS PASSED (${results.length})`);
}

main().catch(err => {
  console.error(err.message || err);
  process.exit(1);
});
