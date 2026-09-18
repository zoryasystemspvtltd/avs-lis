# Report Template Configuration UI Tabs — PROD Portal Deployment Report

**Deployment stamp:** `20260918-231533`  
**Scope:** **PORTAL ONLY**

---

## Summary

| Item | Result |
|------|--------|
| **PORTAL DEPLOYMENT** | **PASS** |
| **API DEPLOYMENT** | **NOT PERFORMED** |
| **DATABASE CHANGE** | **NONE** |
| **GATE 6** | **UNCHANGED** (`true`) |

---

## Paths

| Item | Value |
|------|--------|
| Source build | `i:\Projects\LIS\avs-lis\web\Lis.Web\dist\DxI800` |
| PROD Portal | `I:\Projects\PROD\AVILIS\PORTAL` |
| Backup | `I:\Projects\PROD\AVILIS\_backup\phase-template-ui-tabs-20260918-231533` |
| Previous bundle | `main-es2015.5476a2e0172520f0c5b4.js` |
| Deployed bundle | `main-es2015.e7acacd25ceb566d4c8e.js` |

---

## IIS

| Check | Result |
|-------|--------|
| `AVILIS_PORTAL_POOL` | Recycled → **Started** |
| `AVILIS_API_POOL` | **Started** (not recycled) |
| `Lis.Api.dll` timestamp | 2026-09-12 23:52:05 (unchanged) |

---

## PROD verification (non-mutating)

| Check | Status |
|-------|--------|
| Browser loads new bundle | **PASS** |
| Page loads | **PASS** |
| Tabs Generic / Test Specific / Profile Specific | **PASS** |
| Default tab = Generic only | **PASS** |
| Switch to Test hides Generic list | **PASS** |
| Profile tab + empty state | **PASS** |
| Create Custom in-section / in viewport | **PASS** |
| Create Generic in-section / in viewport | **PASS** |
| Create Specific in-section / in viewport | **PASS** |
| Persisted mode Custom preserved | **PASS** (UI + API) |
| Page errors | **PASS** (none) |
| Create & Design submit (actual create) | **SKIPPED** |
| Mode switch | **SKIPPED** (must not alter customer mode) |

---

## Rollback

Backup retained. **Rollback not required.**

---

## Stop

Portal-only deployment complete. No API/DB/Gate 6 changes.
