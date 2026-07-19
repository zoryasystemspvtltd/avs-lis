// Validates: (1) browser refresh re-syncs permissions (stale cache repaired),
// (2) logout clears session, (3) re-login reloads correct permissions.
import { chromium } from 'playwright-core';

const BASE = 'http://localhost:8080';
const USER = 'recep1@zorya.co.in';
const PASS = 'zorKol@1';
const exe = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';

async function login(page) {
  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await (await page.$('#login-email')).fill(USER);
  await (await page.$('#login-password')).fill(PASS);
  await page.click('button[type="submit"]');
  await page.waitForFunction(() => {
    try {
      const u = JSON.parse(localStorage.getItem('currentUser') || 'null');
      return !!(u && u.accessToken && (u.access || []).length && (u.menuAccess || []).length);
    } catch { return false; }
  }, { timeout: 30000 });
}

function readState(page) {
  return page.evaluate(() => {
    const u = JSON.parse(localStorage.getItem('currentUser') || 'null');
    const sections = Array.from(document.querySelectorAll('#sidenav .panel .panel-title'))
      .map(t => t.textContent.trim());
    return {
      modules: u ? (u.access || []).length : -1,
      menus: u ? (u.menuAccess || []).length : -1,
      sections
    };
  });
}

async function main() {
  const browser = await chromium.launch({ executablePath: exe, headless: true });
  const page = await (await browser.newContext()).newPage();

  await login(page);
  console.log('1. after login:', JSON.stringify(await readState(page)));

  // Simulate stale cache: wipe menuAccess as if permissions were configured after this login.
  await page.evaluate(() => {
    const u = JSON.parse(localStorage.getItem('currentUser'));
    u.menuAccess = [];
    localStorage.setItem('currentUser', JSON.stringify(u));
  });
  console.log('2. tampered (menuAccess=[]) — simulating stale cache');

  await page.reload({ waitUntil: 'networkidle' });
  await page.waitForTimeout(4000);
  console.log('3. after browser refresh:', JSON.stringify(await readState(page)));

  // Logout via the header link.
  await page.click('a:has-text("logout"), a[href*="logout"]').catch(async () => {
    await page.evaluate(() => { const a = Array.from(document.querySelectorAll('a')).find(x => /logout/i.test(x.textContent)); if (a) a.click(); });
  });
  await page.waitForTimeout(3000);
  const afterLogout = await page.evaluate(() => localStorage.getItem('currentUser'));
  console.log('4. after logout, localStorage currentUser =', afterLogout === null ? 'null (cleared)' : 'STILL PRESENT');

  await login(page);
  console.log('5. after re-login:', JSON.stringify(await readState(page)));

  await browser.close();
}

main().catch(e => { console.error('AUDIT FAILED:', e); process.exit(1); });
