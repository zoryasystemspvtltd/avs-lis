// Traces UserAccess-related network calls during portal login.
import { chromium } from 'playwright-core';

const BASE = 'http://localhost:8080';
const USER = process.argv[2] || 'recep1@zorya.co.in';
const PASS = process.argv[3] || 'zorKol@1';
const exe = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';

async function main() {
  const browser = await chromium.launch({ executablePath: exe, headless: true });
  const ctx = await browser.newContext();
  const page = await ctx.newPage();

  page.on('console', m => { if (m.type() === 'error' || m.type() === 'warning') console.log('[console]', m.type(), m.text().slice(0, 300)); });
  page.on('requestfailed', r => console.log('[failed]', r.method(), r.url(), r.failure() && r.failure().errorText));
  page.on('response', async r => {
    const u = r.url();
    if (u.includes('UserAccess') || u.includes('/TOKEN') || u.includes('/Token') || u.includes('/api/Roles')) {
      let bodyLen = -1;
      let preview = '';
      try { const b = await r.text(); bodyLen = b.length; preview = b.slice(0, 120); } catch { /* ignore */ }
      console.log('[resp]', r.status(), r.request().method(), u.replace(BASE, ''), 'len=' + bodyLen, preview.replace(/\s+/g, ' '));
    }
  });

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  const emailInput = await page.$('#login-email');
  const passInput = await page.$('#login-password');
  await emailInput.fill(USER);
  await passInput.fill(PASS);
  await page.click('button[type="submit"]');
  await page.waitForTimeout(8000);

  const ls = await page.evaluate(() => {
    const u = JSON.parse(localStorage.getItem('currentUser') || 'null');
    return u ? { modules: (u.access || []).length, menus: (u.menuAccess || []).length } : null;
  });
  console.log('FINAL localStorage:', JSON.stringify(ls));
  await browser.close();
}

main().catch(e => { console.error('TRACE FAILED:', e); process.exit(1); });
