# Applies and verifies the QA certification seed dataset (Scripts/SeedQaCertificationDataset.sql).
param(
    [string]$Server = ".\SQLEXPRESS",
    [string]$Database = "ZoryaLMS",
    [switch]$ManifestOnly
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$SeedSql = Join-Path $PSScriptRoot "SeedQaCertificationDataset.sql"
$ManifestSql = Join-Path $PSScriptRoot "QaCertificationSeedManifest.sql"

function Invoke-SqlFile([string]$Path) {
    if (-not (Test-Path $Path)) { throw "Missing SQL file: $Path" }
    Write-Host "Running $([IO.Path]::GetFileName($Path))..." -ForegroundColor Cyan
    $out = sqlcmd -S $Server -d $Database -i $Path -b 2>&1
    if ($LASTEXITCODE -ne 0) { throw "SQL failed: $out" }
    if ($out) { $out | ForEach-Object { Write-Host $_ } }
}

function Get-Manifest {
    if (-not (Test-Path $ManifestSql)) { throw "Missing manifest query: $ManifestSql" }
    $rows = sqlcmd -S $Server -d $Database -i $ManifestSql -W -s "`t" -h -1 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Manifest query failed: $rows" }
    return $rows | Where-Object { $_ -and $_.ToString().Trim() -ne '' }
}

function Test-ManifestComplete([object[]]$Rows) {
    $required = @(
        'LaboratoryPatient', 'RadiologyPatient', 'TestProfile', 'AnalyzerTest', 'ManualTest',
        'PendingSample', 'CollectedSample', 'ReceivedSample', 'DoctorUser', 'TechnicianUser'
    )
    $found = @()
    foreach ($line in $Rows) {
        $parts = $line.ToString() -split "`t"
        if ($parts.Count -ge 1) { $found += $parts[0].Trim() }
    }
    $missing = $required | Where-Object { $found -notcontains $_ }
    if ($missing.Count -gt 0) {
        throw "Manifest incomplete. Missing: $($missing -join ', ')"
    }
}

Write-Host "=== QA Certification Seed ===" -ForegroundColor Cyan
Write-Host "Server: $Server | Database: $Database"

if (-not $ManifestOnly) {
    Invoke-SqlFile $SeedSql
    $permSql = Join-Path $PSScriptRoot "SeedRoleBasedPermissions.sql"
    Invoke-SqlFile $permSql
}

$manifest = @(Get-Manifest)
if ($manifest.Count -lt 1) { throw "Manifest returned no rows." }

Write-Host "`n--- Seed manifest ---" -ForegroundColor Cyan
$manifest | ForEach-Object { Write-Host $_ }
Test-ManifestComplete $manifest

Write-Host "`nQA certification seed ready." -ForegroundColor Green
Write-Host "Use qa-cert-doctor@zorya.co.in / qa-cert-tech@zorya.co.in (password: zorKol@1)" -ForegroundColor DarkGray
