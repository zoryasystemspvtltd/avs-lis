# Commission Management Step 2 — Collector Master Discovery

**Date:** 2026-09-26  
**Result:** A safe implementation path exists. Scope stays Sample Collector Master and optional invoice attribution.

---

## Inspected entities and tables

| Item | Actual |
|------|--------|
| Referral Doctor | `ReferralDoctorMaster` / `dbo.ReferralDoctorMaster`. `ReferralDoctorManager` extends `MasterCrudManager`. API `api/ReferralDoctor` on module `Masters`. Angular list/form via `master-schemas` and `LOOKUP_FIELDS.referral`. Menu `MASTER_REFERRAL_DOCTOR` / route `/referral-doctors` |
| Sale Invoice | `SaleInvoice` / `dbo.SaleInvoice`. `ReferralDoctorId` and `CorporateId` are nullable ints with **no database foreign key** in the current schema. Save/update is `SaleInvoiceManager.Save` / `ApplyInvoiceHeaderUpdate`. Angular `sale-invoice-form` |
| CollectedBy | `TestRequestDetail.CollectedBy`, set in `SampleCollectionManager.CollectSample` from the signed-in user. Not a master id |
| Users | `AspNetUsers` / Account → Users. Not a collector business identity |
| BusinessUnit | Not present. Out of this step |

## Master pattern selected

`ReferralDoctorMaster` + `MasterCrudManager` + `MasterApiControllerBase` + generic Angular master list/form.

Sample Collector is an **independent master**. It is not a user account and not `CollectedBy`.

`MasterCrudManager` requires `Code`, `Name`, and `IsActive`. Referral Doctor also stores phone, email, and address. This step keeps code, name, phone, active, and the same audit columns. Email and address are omitted.

Referral Doctor has no unique code index and no duplicate-code check. Sample Collector follows that. Duplicate codes are not rejected.

Delete on these masters sets `IsActive = false`.

## Invoice persistence

`SaleInvoice.CollectorId` is a nullable `int`.

- Create and update copy it in `SaveCore` / `ApplyInvoiceHeaderUpdate`.
- `0` or negative is stored as NULL.
- A missing id is rejected.
- An inactive collector cannot be newly selected. An invoice that already stores that id can still be saved.
- `ReferralDoctorId` and `CollectorId` may both be set. No mutual exclusion.
- `CollectedBy` is not read or written by this change.
- No commission math.

Referral Doctor has no FK. This step adds an FK because the approved model says `CollectorId` references `SampleCollectorMaster.Id`. Delete of a collector is restricted while invoices reference it (`ON DELETE NO ACTION`).

## RBAC

Same module as Referral Doctor: `Masters`.

New menu key `MASTER_SAMPLE_COLLECTOR`, route `/sample-collectors`, left nav next to Referral Doctor.

Deployment SQL copies `RoleMenuPermission` rows from `MASTER_REFERRAL_DOCTOR` onto the new key for the same role. Roles that cannot manage Referral Doctor do not receive the new menu. Invoice edit stays on the existing Sale Invoice permission.

## Migration

Hand-written EF migration plus `Scripts/add-sample-collector-master.sql`, same style as other additive scripts. Idempotent `IF NOT EXISTS`.

## Unresolved

None that block this step. Commission rates, reports, BusinessUnit, and Inside/Outside remain out of scope. Applying the migration to `ZoryaLMS` is a database change and is not part of a production IIS deploy; it is required before the new column can be saved.
