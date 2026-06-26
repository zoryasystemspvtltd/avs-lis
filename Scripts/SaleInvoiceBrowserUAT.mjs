/**
 * Browser UAT: Sale Invoice item search UX (Playwright)
 * Run: npx --yes playwright@1.49.0 install chromium && node Scripts/SaleInvoiceBrowserUAT.mjs
 */
import { chromium } from 'playwright';

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

async function main() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();
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
  gate('UI', 'Login succeeds', true, page.url());

  await page.goto(`${PORTAL}/sale-invoices/create`, { waitUntil: 'networkidle', timeout: 60000 });
  await page.waitForSelector('.sale-invoice-page', { timeout: 30000 });
  gate('UI', 'Sale invoice form visible', await page.isVisible('.sale-invoice-page'), 'form rendered');

  const itemType = page.locator('tbody tr').first().locator('select[formcontrolname="itemType"]');
  gate('UI', 'Item Type control enabled', await itemType.isEnabled(), 'test/profile select');

  const itemSelect = page.locator('.sale-invoice-test-select').first();
  await itemSelect.click();
  const searchInput = page.locator('.sale-invoice-test-select input').first();
  await searchInput.fill('CBC');
  await page.waitForTimeout(600);
  await page.waitForSelector('.ng-option', { timeout: 15000 });
  const options = await page.locator('.ng-option:not(.ng-option-disabled)').count();
  gate('UI', 'Test search returns options', options > 0, `options=${options}`);

  await page.locator('.ng-option:not(.ng-option-disabled)').first().click();
  await page.waitForTimeout(500);
  const deptText = await page.locator('.department-readonly').first().textContent();
  gate('UI', 'Department auto-displayed', deptText && deptText.trim() !== '—' && deptText.trim().length > 0, `dept=${(deptText || '').trim()}`);

  await page.locator('tbody tr').first().locator('select[formcontrolname="itemType"]').selectOption('profile');
  await page.waitForTimeout(300);
  const profileSelect = page.locator('.sale-invoice-test-select').first();
  await profileSelect.click();
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
