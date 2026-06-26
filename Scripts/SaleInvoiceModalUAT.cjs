/**
 * Browser UAT: Sale Invoice Add Line modal
 */
const { chromium } = require('playwright');

const PORTAL = process.env.ZORYALIS_PORTAL || 'http://localhost:8080';
const USER = process.env.ZORYALIS_USER || 'admin@zorya.co.in';
const PASS = process.env.ZORYALIS_PASS || 'zorKol@1';

async function main() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  const errors = [];
  const apiCalls = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(e.message));
  page.on('response', r => {
    if (r.url().includes('BillableItems')) {
      apiCalls.push({ status: r.status(), url: r.url().substring(0, 120) });
    }
  });

  await page.goto(`${PORTAL}/login`, { waitUntil: 'networkidle', timeout: 60000 });
  await page.fill('#login-email', USER);
  await page.fill('#login-password', PASS);
  await page.click('button[type="submit"]');
  await page.waitForURL(u => !u.pathname.includes('/login'), { timeout: 60000 });
  await page.waitForTimeout(2000);

  const menu = page.locator('button[title="Open Left Menu"]');
  if (await menu.count()) { await menu.click(); await page.waitForTimeout(400); }
  const txn = page.locator('a[href="#collapseTransaction"]');
  if (await txn.count()) { await txn.click(); await page.waitForTimeout(400); }
  await page.locator('a[routerlink="/sale-invoices"], a[routerLink="/sale-invoices"]').first().click({ force: true });
  await page.waitForTimeout(2000);
  const create = page.locator('a:has-text("Create New")');
  if (await create.count()) { await create.first().click({ force: true }); await page.waitForTimeout(2500); }

  console.log('URL:', page.url());
  const addLine = page.getByRole('button', { name: /Add Line/i });
  console.log('Add Line count:', await addLine.count());
  if (!(await addLine.count())) {
    throw new Error('Add Line button not found');
  }
  await addLine.first().click();
  await page.waitForTimeout(2000);

  console.log('Modal display:', await page.locator('.sale-invoice-add-line-modal').evaluate(el => getComputedStyle(el).display));
  console.log('Modal select count:', await page.locator('.sale-invoice-modal-select').count());
  console.log('BillableItems calls so far:', apiCalls.length, apiCalls);

  await page.locator('.sale-invoice-modal-select').click();
  await page.waitForTimeout(2500);
  console.log('BillableItems calls after open:', apiCalls.length, apiCalls);

  const panel = page.locator('.sale-invoice-modal-dropdown, .ng-dropdown-panel').last();
  const optCount = await page.locator('.ng-dropdown-panel .ng-option:not(.ng-option-disabled)').count();
  console.log('Options in panel:', optCount);
  const hint = await page.locator('.modal-items-hint').allTextContents();
  console.log('Hints:', hint);
  const spinner = await page.locator('.sale-invoice-modal-select .ng-spinner-loader').count();
  console.log('Spinner:', spinner);

  if (optCount > 0) {
    await page.locator('.ng-dropdown-panel .ng-option:not(.ng-option-disabled)').first().click({ force: true });
    await page.waitForTimeout(800);
    console.log('Preview visible:', await page.locator('.add-line-preview').isVisible());
    console.log('Add to invoice enabled:', await page.getByRole('button', { name: /Add to invoice/i }).isEnabled());
  } else {
    const selectHtml = await page.locator('.sale-invoice-modal-select').innerHTML().catch(() => 'n/a');
    console.log('Select HTML snippet:', selectHtml.substring(0, 300));
  }

  console.log('Console errors:', errors.slice(0, 8));
  await browser.close();
  if (optCount === 0) process.exit(1);
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
