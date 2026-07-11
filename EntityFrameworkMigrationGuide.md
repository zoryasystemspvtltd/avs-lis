# Entity Framework Migration Guide (ZoryaLMS / avs-lis)

This repository uses **Entity Framework 6.4.4** with **two** migration contexts that share the same SQL Server database (`DefaultConnection`).

| Context | Project | Configuration | History table |
|---------|---------|---------------|---------------|
| Application / domain | `LIS.DataModel` (`LIS.DataAccess`) | `LIS.DataAccess.Migrations.Configuration` | `dbo.MigrationHistory` |
| Identity / security | `web/Lis.Api` | `Lis.Api.Migrations.Configuration` | `dbo.MigrationHistory` |

History uses a **custom** table name and column (`Migration_PK`) via `ApplicationHistoryContext` / `EntityFrameworkConfiguration`. Do **not** expect `__MigrationHistory`.

Automatic migrations are **disabled**. Schema changes must be explicit migrations.

---

## Developer daily workflow (after Git Pull)

1. Pull latest code.
2. Restore NuGet packages (Visual Studio restore / `nuget restore`).
3. Update **both** contexts:

```powershell
# Package Manager Console — set Default project appropriately each time

# 1) Domain / ApplicationDBContext
Update-Database -ProjectName LIS.DataAccess -StartUpProjectName Lis.Api -ConfigurationTypeName LIS.DataAccess.Migrations.Configuration

# 2) Identity / IdentityDbContext
Update-Database -ProjectName Lis.Api -StartUpProjectName Lis.Api -ConfigurationTypeName Lis.Api.Migrations.Configuration
```

4. Build and run `Lis.Api` (and Portal as needed).

**Expected result:** either “Running …” for pending migrations, or “No pending migrations.” No manual edits to migration files.

---

## Creating a new migration

### Naming convention

`yyyyMMddHHmmss_ShortPascalDescription`

Examples:

- `202607111930000_AddRoleMenuPermission`
- `202607100420024_fixed` (historical; prefer descriptive names going forward)

### Domain model change (`ApplicationDBContext`)

1. Change entities / fluent config in `LIS.DataModel` / `LIS.DtoModel` as required.
2. PMC (Default project = `LIS.DataAccess`, StartUp = `Lis.Api`):

```powershell
Add-Migration ShortPascalDescription -ProjectName LIS.DataAccess -StartUpProjectName Lis.Api -ConfigurationTypeName LIS.DataAccess.Migrations.Configuration
```

3. Review generated `.cs` / `.Designer.cs` / `.resx`.
4. Prefer **idempotent SQL** (`IF NOT EXISTS` / `IF COL_LENGTH … IS NULL`) when the change may already exist on some environments (this team’s established pattern).
5. Commit **all three** artifacts: migration `.cs`, `.Designer.cs`, `.resx`, plus any `.csproj` entries if not auto-included.
6. Run `Update-Database` (domain config) locally before push.

### Identity / security model change (`IdentityDbContext`)

Same steps with:

```powershell
Add-Migration ShortPascalDescription -ProjectName Lis.Api -StartUpProjectName Lis.Api -ConfigurationTypeName Lis.Api.Migrations.Configuration
```

---

## Mandatory commit checklist

Every migration PR/commit **must** include:

- [ ] `*_MigrationName.cs` (Up/Down)
- [ ] `*_MigrationName.Designer.cs` (`IMigrationMetadata` + migration Id)
- [ ] `*_MigrationName.resx` (compressed **Target** model snapshot)
- [ ] Project file entries (`Compile` + `EmbeddedResource`) when the project is non-SDK style

**Never** commit a migration `.cs` without Designer + resx. That breaks teammates: EF ignores the migration and reports pending model changes.

---

## Resolving migration conflicts (two developers branched)

1. Prefer rebasing/merging so **one linear chain** exists.
2. If both added migrations after the same parent:
   - Keep both migrations if Ids differ and order is correct by timestamp.
   - Regenerate the **later** migration if both changed the same tables:  
     `Update-Database` to common parent → remove conflicting local migration → `Add-Migration` again.
3. Never edit another developer’s already-applied migration Id after it is on shared branches.
4. After merge, each developer runs both `Update-Database` commands.

---

## Failed / partially applied migrations

1. Read the exception (table exists, FK missing, timeout, etc.).
2. Check `dbo.MigrationHistory` for applied Ids (`Migration_PK`, `ContextKey`).
3. If the migration is **idempotent** and schema is already correct but history row is missing, re-run `Update-Database` (preferred) or, only with DBA approval, insert the history row after verifying schema.
4. Do **not** delete production data to “fix” migrations.
5. Do **not** remove applied migrations from source control.

---

## Manual SQL vs EF

- Ad-hoc scripts under `Scripts\` are emergency/ops helpers.
- Once a feature ships, the **EF migration** is the source of truth for teammates.
- If you ran SQL first (e.g. `Scripts/AddRoleMenuPermission.sql`), the matching EF migration **must** be idempotent so `Update-Database` succeeds on databases that already have the object.

---

## Clean machine / fresh clone checklist

1. Clone repo.
2. Restore packages.
3. Point `Lis.Api` `Web.config` `DefaultConnection` at a SQL instance (empty DB or shared dev DB).
4. Run **both** `Update-Database` commands.
5. Build `Lis.Api` Release.
6. Start API / Portal.

For an **empty** database, EF will apply the full chain for both contexts. For an **existing** shared DB, only pending Ids are applied.

---

## Optional CLI helper (this repo)

`Scripts/_ef_scaffold/EfUpdateDb.exe` can list pending migrations and apply them (used by tooling). Prefer Package Manager Console for day-to-day work so Visual Studio project context matches.

---

## Quick diagnostics

```sql
SELECT Migration_PK, ContextKey, ProductVersion
FROM dbo.MigrationHistory
ORDER BY Migration_PK;
```

Identity context key: `Lis.Api.Migrations.Configuration`  
Data context key: `LIS.DataAccess.Migrations.Configuration`

If `Add-Migration` produces a huge unexpected diff, the last migration’s **Target** snapshot is out of sync with the model — fix by adding a proper Designer/resx migration (do not hand-edit binary Target strings).
