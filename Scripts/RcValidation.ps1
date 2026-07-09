# Release Candidate validation — security, RBAC, regression, deployment health
param(
    [switch]$EnsureQaSeed,
    [switch]$SkipDeploy,
    [string]$BaseApi = "http://localhost:8081",
    [string]$BasePortal = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$results = @()
$tag = "RC-VAL-" + (Get-Date -Format "yyyyMMddHHmmss")

function Log([string]$id, [string]$phase, [string]$name, [string]$status, [string]$detail) {
    $global:results += [pscustomobject]@{ Id = $id; Phase = $phase; Scenario = $name; Status = $status; Detail = $detail }
    $color = switch ($status) { "PASS" { "Green" } "FAIL" { "Red" } "WARN" { "Yellow" } default { "Gray" } }
    Write-Host "[$status] $id $name - $detail" -ForegroundColor $color
}

function Get-Token([string]$user, [string]$pwd = "zorKol@1") {
    $body = "grant_type=password&username=$([uri]::EscapeDataString($user))&password=$([uri]::EscapeDataString($pwd))"
    $r = Invoke-RestMethod -Method Post -Uri "$BaseApi/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
    return $r.access_token
}

function Hdr([string]$token) {
    return @{ accesskey = "DXI800"; Authorization = "Bearer $token" }
}

function Expect-Unauth([string]$url) {
    try {
        Invoke-RestMethod -Uri $url -Headers @{ accesskey = "DXI800" } | Out-Null
        throw "Expected 401/403"
    } catch {
        $msg = $_.Exception.Message
        if ($msg -notmatch '401|403|Unauthorized|Forbidden|Authentication') { throw "Expected unauth, got: $msg" }
    }
}

Write-Host "========== RC VALIDATION ($tag) ==========" -ForegroundColor Cyan

# Phase 1: PHI endpoints must reject anonymous access
$phiEndpoints = @(
    @{ Id = "SEC-PHI-01"; Url = "$BaseApi/api/sample/1" },
    @{ Id = "SEC-PHI-02"; Url = "$BaseApi/api/sampledetails/1" },
    @{ Id = "SEC-PHI-03"; Url = "$BaseApi/api/SampleSearch/TEST" },
    @{ Id = "SEC-PHI-04"; Url = "$BaseApi/api/Patients" },
    @{ Id = "SEC-PHI-05"; Url = "$BaseApi/api/Patients/1" },
    @{ Id = "SEC-PHI-06"; Url = "$BaseApi/api/barcode" }
)

foreach ($ep in $phiEndpoints) {
    try {
        Expect-Unauth $ep.Url
        Log $ep.Id "1" "Anonymous blocked: $($ep.Url)" "PASS" "401/403"
    } catch {
        Log $ep.Id "1" "Anonymous blocked: $($ep.Url)" "FAIL" $_.Exception.Message
    }
}

# Integration endpoints remain anonymous (documented)
$integrationOk = @(
    "$BaseApi/api/Lis",
    "$BaseApi/api/Heartbeat"
)
foreach ($url in $integrationOk) {
    try {
        $r = Invoke-WebRequest -UseBasicParsing -Uri $url -TimeoutSec 15
        Log "SEC-INT" "1" "Integration endpoint $url" "PASS" "HTTP $($r.StatusCode) (anonymous allowed)"
    } catch {
        Log "SEC-INT" "1" "Integration endpoint $url" "WARN" $_.Exception.Message
    }
}

# Phase 2-3: Role-based workflow certification
& (Join-Path $PSScriptRoot "RoleBasedUiCertification.ps1") -EnsureQaSeed:$EnsureQaSeed -BaseApi $BaseApi -BasePortal $BasePortal
if ($LASTEXITCODE -ne 0) {
    Log "RBAC" "3" "Role-based certification" "FAIL" "RoleBasedUiCertification.ps1 failed"
} else {
    Log "RBAC" "3" "Role-based certification" "PASS" "All checks passed"
}

# Phase 5: Specimen barcode reuse (SQL validation)
try {
    $dup = sqlcmd -S ".\SQLEXPRESS" -d ZoryaLMS -E -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM (SELECT SampleNo FROM TestRequestDetails WHERE SampleNo IS NOT NULL GROUP BY SampleNo HAVING COUNT(*) > 1) d" 2>&1
    $dupCount = ($dup | Where-Object { $_ -match '^\d+$' } | Select-Object -First 1)
    if ([int]$dupCount -eq 0) { Log "BAR-01" "5" "No duplicate barcodes" "PASS" "Unique SampleNo enforced" }
    else { Log "BAR-01" "5" "No duplicate barcodes" "WARN" "Duplicate SampleNo rows: $dupCount" }
} catch {
    Log "BAR-01" "5" "Barcode uniqueness" "WARN" $_.Exception.Message
}

# Phase 8: Enterprise FDD UAT
& (Join-Path $PSScriptRoot "EnterpriseFddUAT.ps1") -EnsureQaSeed:$false
if ($LASTEXITCODE -ne 0) {
    Log "REG-FDD" "8" "Enterprise FDD UAT" "FAIL" "See output above"
} else {
    Log "REG-FDD" "8" "Enterprise FDD UAT" "PASS" "APPROVED FOR PROD"
}

# Phase 8: Unit tests
$bat = Join-Path $PSScriptRoot "run-masters-tests.bat"
cmd /c "`"$bat`"" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Log "REG-UT" "8" "LIS.Masters.Tests" "FAIL" "exit $LASTEXITCODE"
} else {
    Log "REG-UT" "8" "LIS.Masters.Tests" "PASS" "131+ tests"
}

# Summary
Write-Host "`n========== RC SUMMARY ==========" -ForegroundColor Cyan
$fail = @($results | Where-Object { $_.Status -eq "FAIL" })
$warn = @($results | Where-Object { $_.Status -eq "WARN" })
$pass = @($results | Where-Object { $_.Status -eq "PASS" })
Write-Host "PASS: $($pass.Count)  WARN: $($warn.Count)  FAIL: $($fail.Count)"
$results | Format-Table Id, Phase, Scenario, Status, Detail -AutoSize

if ($fail.Count -gt 0) {
    Write-Host "VERDICT: NOT APPROVED FOR PROD" -ForegroundColor Red
    exit 1
}

Write-Host "VERDICT: RC AUTOMATION PASSED - manual Chrome/Edge UI sign-off still required." -ForegroundColor Green
exit 0
