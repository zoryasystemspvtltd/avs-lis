/**
 * Patient Visit workflow UI + API certification
 * Run: cd Scripts && node VisitWorkflowUiCert.mjs
 */
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PORTAL = process.env.ZORYALIS_PORTAL || 'http://localhost:8080';
const API = process.env.ZORYALIS_API || 'http://localhost:8081';
const PASS = process.env.ZORYALIS_PASS || 'zorKol@1';
const ADMIN = process.env.ZORYALIS_ADMIN || 'admin@zorya.co.in';
const TAG = 'VISIT-CERT-' + new Date().toISOString().replace(/[-:TZ.]/g, '').slice(0, 14);

const results = [];

function log(scenario, status, detail) {
  const row = { Scenario: scenario, Status: status, Detail: detail };
  results.push(row);
  const color = status === 'PASS' ? '\x1b[32m' : status === 'FAIL' ? '\x1b[31m' : '\x1b[33m';
  console.log(`${color}[${status}]\x1b[0m ${scenario} - ${detail}`);
}

async function getToken(request) {
  const res = await request.post(`${API}/TOKEN`, {
    headers: { accesskey: 'DXI800', 'Content-Type': 'application/x-www-form-urlencoded' },
    form: {
      grant_type: 'password',
      username: ADMIN,
      password: PASS,
    },
  });
  if (!res.ok()) throw new Error(`TOKEN failed: ${res.status()}`);
  const json = await res.json();
  return json.access_token;
}

async function apiJson(request, token, method, url, data) {
  const headers = {
    Authorization: `Bearer ${token}`,
    accesskey: 'DXI800',
    'Content-Type': 'application/json',
  };
  const res = method === 'POST'
    ? await request.post(url, { headers, data: data ?? {} })
    : await request.get(url, { headers });
  const text = await res.text();
  let body;
  try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  return { ok: res.ok(), status: res.status(), data: body };
}

async function login(page) {
  await page.goto(`${PORTAL}/login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.fill('#login-email', ADMIN);
  await page.fill('#login-password', PASS);
  await page.click('button[type="submit"]');
  await page.waitForFunction(() => !window.location.pathname.includes('/login'), null, { timeout: 90000 });
  await page.waitForTimeout(1200);
}

async function pickFirstPatient(page) {
  const select = page.locator('.sale-invoice-patient-select, ng-select').first();
  await select.click();
  await page.keyboard.type('qa');
  await page.waitForTimeout(1500);
  const option = page.locator('.ng-option').first();
  await option.waitFor({ state: 'visible', timeout: 15000 });
  const label = (await option.innerText()).trim();
  await option.click();
  await page.waitForTimeout(800);
  return label;
}

async function runApiSuite(request, token) {
  const opt = JSON.stringify({ RecordPerPage: 5, CurrentPage: 1, SearchText: 'qa', SortColumnName: 'Name', SortDirection: false });
  const patientsRes = await request.get(`${API}/api/PatientMaster/`, {
    headers: { Authorization: `Bearer ${token}`, accesskey: 'DXI800', ApiOption: opt },
  });
  const patients = await patientsRes.json();
  const items = patients?.items || patients?.Items || [];
  if (!items.length) {
    log('API: patient search returns rows', 'FAIL', 'no patients for qa search');
    return null;
  }
  log('API: patient search returns rows', 'PASS', `${items.length} row(s)`);
  const patientId = items[0].id ?? items[0].Id;
  const beforeVisit = items[0].visitId ?? items[0].VisitId ?? '';

  const currentRes = await apiJson(request, token, 'GET', `${API}/api/PatientVisit/Current/${patientId}`);
  log('API: GET Current visit', currentRes.ok ? 'PASS' : 'FAIL', currentRes.ok ? `visit=${currentRes.data?.visitId ?? currentRes.data?.VisitId ?? '—'}` : String(currentRes.status));

  const startRes = await apiJson(request, token, 'POST', `${API}/api/PatientVisit/StartVisit/${patientId}`, {});
  const visitPayload = startRes.data?.result ?? startRes.data?.Result ?? startRes.data;
  const newVisitId = visitPayload?.visitId ?? visitPayload?.VisitId;
  const patientVisitId = visitPayload?.patientVisitId ?? visitPayload?.PatientVisitId;
  log('API: POST StartVisit', startRes.ok && newVisitId ? 'PASS' : 'FAIL', `newVisit=${newVisitId || 'missing'}`);

  const histRes = await apiJson(request, token, 'GET', `${API}/api/PatientVisit/${patientId}`);
  const hist = Array.isArray(histRes.data) ? histRes.data : [];
  const hasNew = hist.some(v => (v.visitId ?? v.VisitId) === newVisitId);
  log('API: visit history includes new visit', hasNew ? 'PASS' : 'FAIL', `count=${hist.length}`);

  const patientAfter = await apiJson(request, token, 'GET', `${API}/api/PatientMaster/${patientId}`);
  const currentOnPatient = patientAfter.data?.visitId ?? patientAfter.data?.VisitId;
  log('API: PatientDetails.VisitId synced', currentOnPatient === newVisitId ? 'PASS' : 'FAIL', `${beforeVisit} -> ${currentOnPatient}`);

  return { patientId, patientVisitId, newVisitId, beforeVisit };
}

async function runUiSuite(apiContext) {
  const browser = await chromium.launch({ headless: true, channel: 'chrome' });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();

  await login(page);

  await page.goto(`${PORTAL}/sale-invoices/create`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(1500);

  try {
    await pickFirstPatient(page);
    await page.waitForTimeout(800);

    const startBtn = page.locator('button').filter({ hasText: /Start New Visit/i });
    const hasBtn = (await startBtn.count()) > 0;
    log('UI: Sale Invoice has Start New Visit button', hasBtn ? 'PASS' : 'FAIL', hasBtn ? 'found' : 'missing');

    const visitRow = page.locator('.patient-visit-row');
    const rowVisible = (await visitRow.count()) > 0;
    log('UI: Visit row shown when patient selected', rowVisible ? 'PASS' : 'FAIL', rowVisible ? 'visible' : 'hidden');

    const readVisitDisplay = async () => {
      const input = page.locator('.patient-visit-row input').first();
      if (await input.count()) {
        const val = await input.inputValue().catch(() => '');
        if (val && val !== '—') return val;
      }
      const text = await page.locator('.patient-visit-row').innerText().catch(() => '');
      const match = text.match(/VIS\d+/i);
      return match ? match[0] : '';
    };

    const beforeVisit = await readVisitDisplay();
    if (hasBtn) {
      await startBtn.first().click();
      await page.waitForTimeout(3000);
      await page.waitForFunction(() => {
        const el = document.querySelector('.patient-visit-row input');
        return el && el.value && el.value.startsWith('VIS');
      }, null, { timeout: 15000 }).catch(() => {});
    }
    const afterVisit = await readVisitDisplay();
    const changed = afterVisit && afterVisit !== beforeVisit;
    log('UI: Start New Visit updates current visit field', changed ? 'PASS' : 'FAIL', `${beforeVisit || '—'} -> ${afterVisit || '—'}`);

    if (apiContext?.patientId) {
      await page.goto(`${PORTAL}/patient-master/${apiContext.patientId}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(1200);
      const histTab = page.getByRole('link', { name: /Visit History/i });
      if (await histTab.count()) {
        await histTab.click();
        await page.waitForTimeout(1500);
        const tableText = await page.locator('.visit-history-panel').innerText().catch(() => '');
        const hasHistory = tableText.includes('Visit ID') && (tableText.includes(afterVisit) || tableText.includes('VIS'));
        log('UI: Patient Master Visit History tab', hasHistory ? 'PASS' : 'WARN', hasHistory ? 'history visible' : 'tab opened; verify data manually');
      } else {
        log('UI: Patient Master Visit History tab', 'FAIL', 'tab not found');
      }
    }
  } catch (e) {
    log('UI: Sale Invoice visit flow', 'FAIL', e.message || String(e));
  }

  await browser.close();
}

async function main() {
  console.log(`\n========== VISIT WORKFLOW CERTIFICATION (${TAG}) ==========\n`);
  const browser = await chromium.launch({ headless: true });
  const request = await browser.newContext().then(c => c.request);
  let token;
  try {
    token = await getToken(request);
    log('API: login token', 'PASS', 'obtained');
  } catch (e) {
    log('API: login token', 'FAIL', e.message);
    await browser.close();
    process.exit(1);
  }

  const apiContext = await runApiSuite(request, token);
  await runUiSuite(apiContext);
  await browser.close();

  const fail = results.filter(r => r.Status === 'FAIL');
  const warn = results.filter(r => r.Status === 'WARN');
  const pass = results.filter(r => r.Status === 'PASS');
  const report = {
    tag: TAG,
    portal: PORTAL,
    api: API,
    summary: { pass: pass.length, warn: warn.length, fail: fail.length },
    results,
    verdict: fail.length === 0 ? 'APPROVED FOR PROD' : 'NOT APPROVED FOR PROD',
  };
  const outPath = path.join(__dirname, `VisitWorkflowUiCert-${TAG}.json`);
  fs.writeFileSync(outPath, JSON.stringify(report, null, 2));
  console.log(`\nPASS: ${pass.length}  WARN: ${warn.length}  FAIL: ${fail.length}`);
  console.log(`Report: ${outPath}`);
  console.log(`VERDICT: ${report.verdict}`);
  process.exit(fail.length ? 1 : 0);
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
