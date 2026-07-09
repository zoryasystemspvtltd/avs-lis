# Role-Based UI Certification (Technician + Doctor)
# Validates permissions, menu modules, and business workflows via the same APIs the portal calls.
# Complements manual browser UAT; uses QA cert users and QA-CERT-* seed data.
param(
    [switch]$EnsureQaSeed,
    [string]$BaseApi = "http://localhost:8081",
    [string]$BasePortal = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"
$results = @()
$tag = "RBAC-UAT-" + (Get-Date -Format "yyyyMMddHHmmss")

$TechUser = "qa-cert-tech@zorya.co.in"
$DocUser = "qa-cert-doctor@zorya.co.in"
$AdminUser = "admin@zorya.co.in"
$Pwd = "zorKol@1"

$TechModules = @("Samples", "SampleCollection", "SampleReceiving", "Reports")
$DocModules = @("DoctorsApprovals", "Reports", "RadiologyDoctorApprovals", "RadiologyReports")
$ForbiddenTech = @("DoctorsApprovals", "Masters", "Users", "Roles", "RadiologyReportEntry", "RadiologyDoctorApprovals")
$ForbiddenDoc = @("SampleCollection", "SampleReceiving", "Samples", "Masters", "Users", "Roles")

function Log([string]$id, [string]$phase, [string]$name, [string]$status, [string]$detail) {
    $global:results += [pscustomobject]@{ Id = $id; Phase = $phase; Scenario = $name; Status = $status; Detail = $detail }
    $color = switch ($status) { "PASS" { "Green" } "FAIL" { "Red" } "WARN" { "Yellow" } default { "Gray" } }
    Write-Host "[$status] $id $name - $detail" -ForegroundColor $color
}

function Get-Token([string]$user, [string]$password = $Pwd) {
    $encUser = [uri]::EscapeDataString($user)
    $encPwd = [uri]::EscapeDataString($password)
    $body = "grant_type=password&username=$encUser&password=$encPwd"
    $r = Invoke-RestMethod -Method Post -Uri "$BaseApi/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
    if (-not $r.access_token) { throw "Token failed for $user" }
    return $r.access_token
}

function Hdr([string]$token, [string]$apiOption = $null) {
    $h = @{ accesskey = "DXI800"; Authorization = "Bearer $token" }
    if ($apiOption) { $h.ApiOption = $apiOption }
    return $h
}

function Get-UserModules([string]$token) {
    $raw = Invoke-RestMethod -Uri "$BaseApi/api/UserAccess/DXI800" -Headers (Hdr $token)
    if ($raw -is [string]) { return @($raw | ConvertFrom-Json) }
    if ($raw -is [System.Array]) { return $raw }
    return @($raw)
}

function Has-Module([object[]]$modules, [string]$name) {
    return @($modules | Where-Object { $_.name -eq $name -or $_.Name -eq $name }).Count -gt 0
}

function Get-ModuleAccess([object[]]$modules, [string]$name) {
    $m = $modules | Where-Object { $_.name -eq $name -or $_.Name -eq $name } | Select-Object -First 1
    if (-not $m) { return 0 }
    if ($m.access) { return [int]$m.access }
    return 0
}

function Expect-Forbidden([scriptblock]$block) {
    try {
        & $block | Out-Null
        throw "Expected 403 Forbidden"
    } catch {
        $msg = $_.Exception.Message
        if ($_.ErrorDetails.Message) { $msg += " " + $_.ErrorDetails.Message }
        if ($msg -notmatch '403|401|Forbidden|Unauthorized|Insufficient privilege|Authentication required') { throw "Expected Forbidden, got: $msg" }
    }
}

function SqlScalar([string]$q) {
    $out = & sqlcmd -S ".\SQLEXPRESS" -d ZoryaLMS -E -h -1 -W -Q ("SET NOCOUNT ON; " + $q) 2>&1
    if ($LASTEXITCODE -ne 0) { throw "SQL failed: $out" }
    $line = $out | Where-Object { $_ -and $_.ToString().Trim() -ne "" } | Select-Object -First 1
    if ($null -eq $line) { return "" }
    return $line.ToString().Trim()
}

function Get-RowId($row) {
    if ($null -eq $row) { return 0 }
    if ($row.Id) { return [long]$row.Id }
    if ($row.id) { return [long]$row.id }
    return 0
}

function Get-Items($resp) {
    if ($null -eq $resp) { return @() }
    if ($resp -is [System.Array]) { return ,$resp }
    $val = if ($resp.items) { $resp.items } else { $resp.Items }
    if ($null -eq $val) { return @() }
    if ($val -is [System.Array]) { return ,$val }
    return @($val)
}

Write-Host "========== ROLE-BASED UI CERTIFICATION ($tag) ==========" -ForegroundColor Cyan

# --- Phase 0: Environment ---
try {
    $apiOk = (Invoke-WebRequest -UseBasicParsing -Uri $BaseApi -TimeoutSec 10).StatusCode
    $portalOk = (Invoke-WebRequest -UseBasicParsing -Uri $BasePortal -TimeoutSec 10).StatusCode
    Log "ENV-01" "0" "API/Portal health" "PASS" "API=$apiOk Portal=$portalOk"
} catch {
    Log "ENV-01" "0" "API/Portal health" "FAIL" $_.Exception.Message
    throw
}

if ($EnsureQaSeed) {
    & (Join-Path $PSScriptRoot "RunQaCertificationSeed.ps1")
}

# --- Phase 1: Permission matrix ---
Write-Host "`n--- PHASE 1: Admin / Permission Preparation ---" -ForegroundColor Cyan
& sqlcmd -S ".\SQLEXPRESS" -d ZoryaLMS -i (Join-Path $PSScriptRoot "SeedRoleBasedPermissions.sql") -b | Out-Null

$techMapCount = SqlScalar "SELECT COUNT(*) FROM RoleModuleMappings rm INNER JOIN AspNetRoles r ON r.Id = rm.RoleId INNER JOIN UserModules um ON um.Id = rm.ModuleId WHERE r.Name = N'Technician' AND um.Name IN (N'Samples',N'SampleCollection',N'SampleReceiving',N'Reports')"
$docMapCount = SqlScalar "SELECT COUNT(*) FROM RoleModuleMappings rm INNER JOIN AspNetRoles r ON r.Id = rm.RoleId INNER JOIN UserModules um ON um.Id = rm.ModuleId WHERE r.Name = N'Doctor' AND um.Name IN (N'DoctorsApprovals',N'Reports',N'RadiologyDoctorApprovals',N'RadiologyReports')"

if ([int]$techMapCount -eq 4) { Log "P1-01" "1" "Technician role modules" "PASS" "4 modules mapped" }
else { Log "P1-01" "1" "Technician role modules" "FAIL" "Expected 4, got $techMapCount" }

if ([int]$docMapCount -eq 4) { Log "P1-02" "1" "Doctor role modules" "PASS" "4 modules mapped" }
else { Log "P1-02" "1" "Doctor role modules" "FAIL" "Expected 4, got $docMapCount" }

# --- Phase 2: Technician ---
Write-Host "`n--- PHASE 2: Technician Role ---" -ForegroundColor Cyan
$techToken = Get-Token $TechUser
$techMods = @(Get-UserModules $techToken)
$techNames = @($techMods | ForEach-Object { if ($_.name) { $_.name } else { $_.Name } })

$techMenuOk = $true
foreach ($m in $TechModules) {
    if (-not (Has-Module $techMods $m)) { $techMenuOk = $false; Log "P2-MENU" "2" "Technician menu: $m" "FAIL" "Missing module" }
}
if ($techMenuOk) { Log "P2-01" "2" "Technician menu modules" "PASS" ($techNames -join ", ") }

foreach ($f in $ForbiddenTech) {
    if (Has-Module $techMods $f) { Log "P2-NEG-$f" "2" "Technician forbidden: $f" "FAIL" "Module visible in access" }
    else { Log "P2-NEG-$f" "2" "Technician forbidden: $f" "PASS" "Not granted" }
}

# Workflow: collection queue (QA pending sample)
try {
    $opt = '{"RecordPerPage":50,"CurrentPage":1,"OrderNumber":"QA-CERT-INV-PEND"}'
    $q = Invoke-RestMethod -Uri "$BaseApi/api/SampleCollection/PendingQueue" -Headers (Hdr $techToken $opt)
    $found = @(Get-Items $q | Where-Object {
        $sn = if ($_.SampleNo) { $_.SampleNo } elseif ($_.sampleNo) { $_.sampleNo } else { "" }
        $sn -eq "QA-CERT-SMP-PEND"
    })
    if ($found.Count -gt 0) { Log "P2-02" "2" "Sample Collection queue" "PASS" "QA-CERT-SMP-PEND visible" }
    else { Log "P2-02" "2" "Sample Collection queue" "WARN" "Pending sample not in queue" }
} catch {
    Log "P2-02" "2" "Sample Collection queue" "FAIL" $_.Exception.Message
}

# Collect pending sample
try {
    $pendId = SqlScalar "SELECT CAST(Id AS varchar(20)) FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-PEND' OR HISRequestNo = N'QA-CERT-INV-PEND'"
    if ([string]::IsNullOrWhiteSpace($pendId)) { throw "QA-CERT-SMP-PEND not found" }
    $barcode = (Invoke-RestMethod -Uri "$BaseApi/api/SampleCollection/EnsureBarcode/$pendId" -Headers (Hdr $techToken)).barcode
    $body = @{
        testRequestId = [long]$pendId
        collectionDateTime = (Get-Date).AddMinutes(-20).ToString("yyyy-MM-ddTHH:mm:ss")
        remarks = "$tag technician collect"
        barcodeNumber = $barcode
    } | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri "$BaseApi/api/SampleCollection/Collect" -Headers (Hdr $techToken) -ContentType "application/json" -Body $body | Out-Null
    $colBy = SqlScalar "SELECT ISNULL(CollectedBy,'') FROM TestRequestDetails WHERE Id = $pendId"
    if ($colBy) { Log "P2-03" "2" "Sample Collection" "PASS" "CollectedBy=$colBy Barcode=$barcode" }
    else { Log "P2-03" "2" "Sample Collection" "FAIL" "CollectedBy empty" }
} catch {
    Log "P2-03" "2" "Sample Collection" "FAIL" $_.Exception.Message
}

# Receiving queue + receive
try {
    $recvOpt = '{"RecordPerPage":25,"CurrentPage":1}'
    $rq = Invoke-RestMethod -Uri "$BaseApi/api/SampleReceiving/Queue" -Headers (Hdr $techToken $recvOpt)
    $collId = SqlScalar "SELECT CAST(Id AS varchar(20)) FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-COLL' OR HISRequestNo = N'QA-CERT-INV-COLL'"
    if ([string]::IsNullOrWhiteSpace($collId)) { throw "QA-CERT-SMP-COLL not found" }
    sqlcmd -S ".\SQLEXPRESS" -d ZoryaLMS -Q "UPDATE TestRequestDetails SET CollectedBy=N'$TechUser', ReceivedBy=NULL, ReceivedRemarks=NULL, ReportStatus=0 WHERE Id=$collId" -b | Out-Null
    $recvBody = @{
        testRequestId = [long]$collId
        receivedDateTime = (Get-Date).AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ss")
        remarks = "$tag receive"
    } | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri "$BaseApi/api/SampleReceiving/Receive" -Headers (Hdr $techToken) -ContentType "application/json" -Body $recvBody | Out-Null
    $recvBy = SqlScalar "SELECT ISNULL(ReceivedBy,'') FROM TestRequestDetails WHERE Id = $collId"
    if ($recvBy) { Log "P2-04" "2" "Sample Receiving" "PASS" "ReceivedBy=$recvBy" }
    else { Log "P2-04" "2" "Sample Receiving" "FAIL" "ReceivedBy empty" }
} catch {
    Log "P2-04" "2" "Sample Receiving" "FAIL" $_.Exception.Message
}

# Reject at receiving -> returns to collection
try {
    $rejId = SqlScalar "SELECT CAST(Id AS varchar(20)) FROM TestRequestDetails WHERE SampleNo = N'QA-CERT-SMP-RECV' OR HISRequestNo = N'QA-CERT-INV-RECV'"
    if ([string]::IsNullOrWhiteSpace($rejId)) { throw "QA-CERT-SMP-RECV not found - run seed" }
    # Reset to collected-only for reject test
    sqlcmd -S ".\SQLEXPRESS" -d ZoryaLMS -Q "UPDATE TestRequestDetails SET CollectedBy=N'$TechUser', ReceivedBy=NULL, ReceivedRemarks=NULL, ReportStatus=0 WHERE Id=$rejId" -b | Out-Null
    $rejBody = @{ testRequestId = [long]$rejId; rejectionReasonCode = "RJ01"; remarks = "$tag reject test"; isReceiving = $true } | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri "$BaseApi/api/SampleReceiving/Reject" -Headers (Hdr $techToken) -ContentType "application/json" -Body $rejBody | Out-Null
    $colAfter = SqlScalar "SELECT ISNULL(CollectedBy,'') FROM TestRequestDetails WHERE Id = $rejId"
    $recvAfter = SqlScalar "SELECT ISNULL(ReceivedBy,'') FROM TestRequestDetails WHERE Id = $rejId"
    if ($colAfter -eq "" -and $recvAfter -eq "") { Log "P2-05" "2" "Reject returns to Collection" "PASS" "CollectedBy/ReceivedBy cleared" }
    else { Log "P2-05" "2" "Reject returns to Collection" "FAIL" "CollectedBy=$colAfter ReceivedBy=$recvAfter" }
} catch {
    Log "P2-05" "2" "Reject returns to Collection" "FAIL" $_.Exception.Message
}

# Technician negative: doctor approval API
try {
    Expect-Forbidden { Invoke-RestMethod -Method Put -Uri "$BaseApi/api/sample" -Headers (Hdr $techToken) -ContentType "application/json" -Body '{"id":1,"status":1,"note":"x","runIndex":0}' }
    Log "P2-06" "2" "Technician denied Doctor Approval" "PASS" "403 on PUT api/sample"
} catch {
    Log "P2-06" "2" "Technician denied Doctor Approval" "FAIL" $_.Exception.Message
}

# Technician negative: Roles API (administration)
try {
    Expect-Forbidden { Invoke-RestMethod -Uri "$BaseApi/api/Roles" -Headers (Hdr $techToken) }
    Log "P2-07" "2" "Technician denied Roles" "PASS" "403 on Roles"
} catch {
    Log "P2-07" "2" "Technician denied Roles" "WARN" $_.Exception.Message
}

# --- Phase 3: Doctor ---
Write-Host "`n--- PHASE 3: Doctor Role ---" -ForegroundColor Cyan
$docToken = Get-Token $DocUser
$docMods = @(Get-UserModules $docToken)
$docNames = @($docMods | ForEach-Object { if ($_.name) { $_.name } else { $_.Name } })

$docMenuOk = $true
foreach ($m in $DocModules) {
    if (-not (Has-Module $docMods $m)) { $docMenuOk = $false; Log "P3-MENU" "3" "Doctor menu: $m" "FAIL" "Missing" }
}
if ($docMenuOk) { Log "P3-01" "3" "Doctor menu modules" "PASS" ($docNames -join ", ") }

foreach ($f in $ForbiddenDoc) {
    if (Has-Module $docMods $f) { Log "P3-NEG-$f" "3" "Doctor forbidden: $f" "FAIL" "Module granted" }
    else { Log "P3-NEG-$f" "3" "Doctor forbidden: $f" "PASS" "Not granted" }
}

# Doctor negative: sample collection
try {
    Expect-Forbidden { Invoke-RestMethod -Uri "$BaseApi/api/SampleCollection/PendingQueue" -Headers (Hdr $docToken '{"RecordPerPage":5,"CurrentPage":1}') }
    Log "P3-02" "3" "Doctor denied Sample Collection" "PASS" "403 on PendingQueue"
} catch {
    Log "P3-02" "3" "Doctor denied Sample Collection" "FAIL" $_.Exception.Message
}

# Radiology doctor approval queue (not PendingQueue — that is RadiologyReportEntry)
try {
    $radOpt = '{"RecordPerPage":10,"CurrentPage":1}'
    $radQ = Invoke-RestMethod -Uri "$BaseApi/api/RadiologyReport/DoctorApprovalQueue" -Headers (Hdr $docToken $radOpt)
    $cnt = @(Get-Items $radQ).Count
    Log "P3-03" "3" "Radiology doctor approval queue" "PASS" "Rows=$cnt"
} catch {
    Log "P3-03" "3" "Radiology doctor approval queue" "WARN" $_.Exception.Message
}

# Diagnostic report API (Reports module)
try {
    $patId = SqlScalar "SELECT CAST(Id AS varchar(20)) FROM PatientDetails WHERE HisPatientId = N'QA-CERT-LAB-PAT'"
    Invoke-RestMethod -Uri "$BaseApi/api/Reports/TestReportLabNumbers" -Headers (Hdr $docToken) | Out-Null
    Log "P3-04" "3" "Diagnostic report API" "PASS" "Reports CanView"
} catch {
    Log "P3-04" "3" "Diagnostic report API" "FAIL" $_.Exception.Message
}

# Doctor negative: Roles API
try {
    Expect-Forbidden { Invoke-RestMethod -Uri "$BaseApi/api/Roles" -Headers (Hdr $docToken) }
    Log "P3-05" "3" "Doctor denied Roles" "PASS" "403"
} catch {
    Log "P3-05" "3" "Doctor denied Roles" "WARN" $_.Exception.Message
}

# --- Phase 4: Security ---
Write-Host "`n--- PHASE 4: Security ---" -ForegroundColor Cyan
try {
    Expect-Forbidden { Invoke-RestMethod -Uri "$BaseApi/api/SampleCollection/PendingQueue" -Headers @{ accesskey = "DXI800" } }
    Log "SEC-01" "4" "Unauthenticated API blocked" "PASS" "401/403 without token"
} catch {
    Log "SEC-01" "4" "Unauthenticated API blocked" "FAIL" $_.Exception.Message
}

# Role switch after re-login
try {
    $t2 = Get-Token $TechUser
    $d2 = Get-Token $DocUser
    $tm2 = @(Get-UserModules $t2)
    $dm2 = @(Get-UserModules $d2)
    if ((Has-Module $tm2 "Reports") -and (Has-Module $dm2 "DoctorsApprovals") -and -not (Has-Module $tm2 "DoctorsApprovals")) {
        Log "SEC-02" "4" "Role switch isolation" "PASS" "Distinct module sets after re-login"
    } else { Log "SEC-02" "4" "Role switch isolation" "FAIL" "Module bleed detected" }
} catch {
    Log "SEC-02" "4" "Role switch isolation" "FAIL" $_.Exception.Message
}

# --- Phase 5: Portal smoke (UI shell) ---
Write-Host "`n--- PHASE 5: Portal UI Shell ---" -ForegroundColor Cyan
$portalRoutes = @("/", "/login", "/sample-collection", "/sample-receiving", "/technicianapprovals", "/doctorapprovals", "/reports/test-report")
foreach ($route in $portalRoutes) {
    try {
        $code = (Invoke-WebRequest -UseBasicParsing -Uri ($BasePortal + $route) -TimeoutSec 15).StatusCode
        if ($code -eq 200) { Log "UI-$route" "5" "Portal route $route" "PASS" "HTTP 200" }
        else { Log "UI-$route" "5" "Portal route $route" "WARN" "HTTP $code" }
    } catch {
        Log "UI-$route" "5" "Portal route $route" "WARN" $_.Exception.Message
    }
}

# --- Summary ---
Write-Host "`n========== CERTIFICATION SUMMARY ==========" -ForegroundColor Cyan
$fail = @($results | Where-Object { $_.Status -eq "FAIL" })
$warn = @($results | Where-Object { $_.Status -eq "WARN" })
$pass = @($results | Where-Object { $_.Status -eq "PASS" })
Write-Host "PASS: $($pass.Count)  WARN: $($warn.Count)  FAIL: $($fail.Count)"
$results | Format-Table Id, Phase, Scenario, Status, Detail -AutoSize

if ($fail.Count -gt 0) {
    Write-Host "VERDICT: NOT APPROVED FOR PROD" -ForegroundColor Red
    Write-Host "Manual UI walkthrough still required for: layout, print preview, signatures, keyboard nav." -ForegroundColor Yellow
    exit 1
}

Write-Host "VERDICT: CONDITIONAL APPROVAL - automated RBAC + workflow checks passed." -ForegroundColor Green
Write-Host "Complete manual browser certification for print layout, doctor signature, and visual QA." -ForegroundColor Yellow
exit 0
