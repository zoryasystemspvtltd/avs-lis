// Audits the left navigation as seen by a given user on the deployed portal.
// Usage: node ReceptionistNavAudit.mjs [username] [password]
import { chromium } from 'playwright-core';

const BASE = 'http://localhost:8080';
const USER = process.argv[2] || 'recep1@zorya.co.in';
const PASS = process.argv[3] || 'zorKol@1';

const exe = process.env.EDGE_PATH || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';

async function main() {
  const browser = await chromium.launch({ executablePath: exe, headless: true });
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
  const page = await ctx.newPage();

  await page.goto(BASE + '/login', { waitUntil: 'networkidle' });
  await page.fill('#login-email, input[formcontrolname="email"], input[formcontrolname="username"]', USER).catch(() => {});
  // fallbacks for the login form controls
  const emailInput = await page.$('input[formcontrolname="email"]') || await page.$('input[type="text"]');
  const passInput = await page.$('#login-password') || await page.$('input[formcontrolname="password"]');
  if (emailInput) { await emailInput.fill(USER); }
  if (passInput) { await passInput.fill(PASS); }
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'networkidle', timeout: 30000 }).catch(() => {}),
    page.click('button[type="submit"], input[type="submit"]')
  ]);
  await page.waitForTimeout(4000);

  const result = await page.evaluate(() => {
    const out = { url: location.href, sections: [] };
    const panels = document.querySelectorAll('#sidenav .panel');
    panels.forEach(p => {
      const title = (p.querySelector('.panel-title') || {}).textContent || '';
      const items = Array.from(p.querySelectorAll('td a')).map(a => ({
        label: a.textContent.trim(),
        href: a.getAttribute('href') || a.getAttribute('routerlink') || ''
      }));
      out.sections.push({ section: title.trim(), itemCount: items.length, items });
    });
    let ls = null;
    try {
      const raw = localStorage.getItem('currentUser');
      if (raw) {
        const u = JSON.parse(raw);
        ls = { userName: u.userName, moduleCount: (u.access || []).length, menuCount: (u.menuAccess || []).length };
      }
    } catch (e) { ls = { error: String(e) }; }
    out.localStorage = ls;
    return out;
  });

  console.log(JSON.stringify(result, null, 2));
  await browser.close();
}

main().catch(e => { console.error('AUDIT FAILED:', e); process.exit(1); });
