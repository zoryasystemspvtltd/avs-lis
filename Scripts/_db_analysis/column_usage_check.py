# -*- coding: utf-8 -*-
"""Compare ZoryaLMS columns to EF entity properties + light usage search."""
import re
from pathlib import Path
from collections import defaultdict

root = Path(r"I:\Projects\LIS\avs-lis")

db_cols = []
with open(root / "Scripts/_db_analysis/all_columns.tsv", encoding="utf-8", errors="ignore") as f:
    for line in f:
        line = line.strip()
        if not line or line.startswith("Changed") or set(line) <= set("- "):
            continue
        if "." in line and "\t" not in line:
            db_cols.append(line)
        elif "\t" in line:
            # ignore header-ish
            parts = line.split("\t")
            if len(parts) >= 1 and "." in parts[0]:
                db_cols.append(parts[0])

table_to_props = {}


def add_props(table, props):
    table_to_props.setdefault(table, set()).update(props)


def props_from_chunk(chunk):
    props = re.findall(
        r"public\s+(?:virtual\s+|override\s+|new\s+)?[\w.<>,\?\[\]\s]+\s+(\w+)\s*\{\s*get",
        chunk,
    )
    skip = {"class", "get", "set", "value"}
    return {p for p in props if p not in skip}


# [Table("X")] entities
for fp in list((root / "LIS.DtoModel").rglob("*.cs")) + list((root / "web/Lis.Api/Models").rglob("*.cs")):
    text = fp.read_text(encoding="utf-8", errors="ignore")
    for m in re.finditer(
        r'\[Table\("(?:dbo\.)?([^"]+)"\)\]\s*(?:\[[^\]]+\]\s*)*public\s+class\s+(\w+)',
        text,
    ):
        table = m.group(1)
        start = m.end()
        nxt = re.search(r"\n\s*public\s+(?:partial\s+)?class\s+", text[start:])
        chunk = text[start : start + (nxt.start() if nxt else 12000)]
        add_props(table, props_from_chunk(chunk))

# Identity / convention entities
identity_map = {
    "RoleModuleMappings": "RoleModuleMappings",
    "UserModule": "UserModules",
    "UserApplicationMapping": "UserApplicationMappings",
    "RefreshToken": "RefreshTokens",
    "ApplicationUser": "AspNetUsers",
    "RoleMenuPermission": "RoleMenuPermission",
    "ClientApplication": "ClientApplication",
}
for fp in (root / "web/Lis.Api/Models").rglob("*.cs"):
    text = fp.read_text(encoding="utf-8", errors="ignore")
    for cls, table in identity_map.items():
        m = re.search(rf"public\s+class\s+{cls}\b", text)
        if not m:
            continue
        start = m.end()
        nxt = re.search(r"\n\s*public\s+(?:partial\s+)?class\s+", text[start:])
        chunk = text[start : start + (nxt.start() if nxt else 8000)]
        add_props(table, props_from_chunk(chunk))

# AspNet standard columns often from Identity base (not all on ApplicationUser)
aspnet_extra = {
    "AspNetUsers": {
        "Id", "Email", "EmailConfirmed", "PasswordHash", "SecurityStamp",
        "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEndDateUtc",
        "LockoutEnabled", "AccessFailedCount", "UserName", "Discriminator",
    },
    "AspNetRoles": {"Id", "Name"},
    "AspNetUserRoles": {"UserId", "RoleId"},
    "AspNetUserClaims": {"Id", "UserId", "ClaimType", "ClaimValue"},
    "AspNetUserLogins": {"LoginProvider", "ProviderKey", "UserId"},
}
for t, props in aspnet_extra.items():
    add_props(t, props)

unmapped = []
for full in db_cols:
    table, col = full.split(".", 1)
    props = table_to_props.get(table)
    if props is None:
        unmapped.append((full, "NO_EF_ENTITY"))
        continue
    if col.lower() not in {p.lower() for p in props}:
        unmapped.append((full, "NOT_ON_ENTITY"))

print(f"DB_COLUMNS={len(db_cols)}")
print(f"EF_TABLES_PARSED={len(table_to_props)}")
print(f"UNMAPPED_OR_UNPARSED={len(unmapped)}")

by_table = defaultdict(list)
for full, reason in unmapped:
    t, c = full.split(".", 1)
    by_table[t].append((c, reason))

# Light usage: search column name in cs/ts/sql (token)
search_roots = [
    root / "LIS.Com.Businesslogic",
    root / "LIS.DataModel",
    root / "LIS.DtoModel",
    root / "web/Lis.Api",
    root / "web/Lis.Web/src",
    root / "Scripts",
]

suspects = []  # unmapped AND rarely referenced


def ref_count(name: str) -> int:
    # count files containing name as whole-ish word
    pat = re.compile(rf"\b{re.escape(name)}\b")
    n = 0
    for base in search_roots:
        if not base.exists():
            continue
        for fp in base.rglob("*"):
            if fp.suffix.lower() not in {".cs", ".ts", ".sql", ".html"}:
                continue
            try:
                txt = fp.read_text(encoding="utf-8", errors="ignore")
            except Exception:
                continue
            if pat.search(txt):
                n += 1
    return n


print("\n=== DB columns not found on parsed EF entity properties ===")
for t in sorted(by_table):
    cols = by_table[t]
    print(f"\n{t} ({len(cols)})")
    for c, r in cols:
        rc = ref_count(c)
        flag = ""
        if rc <= 1:
            flag = "  << LOW_CODE_REFS"
            suspects.append((t, c, r, rc))
        print(f"  {c:40} {r:16} refs~{rc}{flag}")

print("\n=== LOW-REF suspects (not on entity parse + <=1 file refs) ===")
for t, c, r, rc in suspects:
    print(f"{t}.{c}\t{r}\trefs={rc}")

# Also: EF props with no DB column (orphan properties)
print("\n=== EF properties with no matching DB column (sample) ===")
db_by_table = defaultdict(set)
for full in db_cols:
    t, c = full.split(".", 1)
    db_by_table[t].add(c.lower())

orphan_props = []
for t, props in sorted(table_to_props.items()):
    dbset = db_by_table.get(t)
    if not dbset:
        continue
    for p in sorted(props):
        if p.lower() not in dbset and p.lower() not in {"equals", "gethashcode", "tostring"}:
            # navigation collections often not columns
            if p.endswith("s") and p[:1].isupper():
                # still list
                pass
            orphan_props.append((t, p))

for t, p in orphan_props[:60]:
    print(f"{t}.{p}")
print(f"ORPHAN_PROP_COUNT={len(orphan_props)}")
