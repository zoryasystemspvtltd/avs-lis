# Verifies EF migration history is current for ZoryaLMS.
param(
    [string]$Server = ".\SQLEXPRESS",
    [string]$Database = "ZoryaLMS"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Ef6 = Join-Path $Root "packages\EntityFramework.6.4.4\tools\net45\any\ef6.exe"
$conn = "Server=$Server;Database=$Database;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true"

$expectedDataAccess = @(
    "202605170711055_fresh",
    "202606191317350_FddSampleRadiology",
    "202606200950114_patientaddress",
    "202606261200000_DepartmentProcessingCategory",
    "202606271200000_PatientMrVisitPrefix",
    "202607081200000_SaleInvoiceDiscountFields",
    "202607081210000_SaleInvoiceDetailNullableRequestDetail",
    "202607081220000_LabResultRestructure",
    "202607081802343_SchemaModelSync"
)

$expectedIdentity = @(
    "202604270642026_fresh",
    "202606281200000_DoctorDesignationSignature",
    "202607081804055_IdentityModelSync"
)

function Get-AppliedMigrations([string]$assembly, [string]$config, [string]$migrationsConfig) {
    $out = & $Ef6 migrations list `
        --assembly $assembly `
        --config $config `
        --connection-string $conn `
        --connection-provider "System.Data.SqlClient" `
        --migrations-config $migrationsConfig 2>&1
    if ($LASTEXITCODE -ne 0) { throw "ef6 migrations list failed: $out" }
    return @($out | Where-Object { $_ -and $_.Trim() -ne '' } | ForEach-Object { $_.Trim() })
}

Write-Host "=== EF Migration Verification ($Database) ===" -ForegroundColor Cyan

$dataDll = Join-Path $Root "LIS.DataModel\bin\Release\LIS.DataAccess.dll"
$apiDll = Join-Path $Root "web\Lis.Api\bin\Lis.Api.dll"
if (-not (Test-Path $dataDll)) {
    $msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
    & $msbuild (Join-Path $Root "LIS.DataModel\LIS.DataAccess.csproj") /t:Build /p:Configuration=Release /v:minimal | Out-Null
    & $msbuild (Join-Path $Root "web\Lis.Api\Lis.Api.csproj") /t:Build /p:Configuration=Release /v:minimal | Out-Null
}

$appliedData = Get-AppliedMigrations $dataDll (Join-Path $Root "LIS.DataModel\App.config") "LIS.DataAccess.Migrations.Configuration"
$appliedApi = Get-AppliedMigrations $apiDll (Join-Path $Root "web\Lis.Api\Web.config") "Lis.Api.Migrations.Configuration"

Write-Host "`nLIS.DataAccess applied:" -ForegroundColor Yellow
$appliedData | ForEach-Object { Write-Host "  $_" }

Write-Host "`nLis.Api applied:" -ForegroundColor Yellow
$appliedApi | ForEach-Object { Write-Host "  $_" }

$missingData = $expectedDataAccess | Where-Object { $appliedData -notcontains $_ }
$missingApi = $expectedIdentity | Where-Object { $appliedApi -notcontains $_ }

if ($missingData.Count -gt 0 -or $missingApi.Count -gt 0) {
    if ($missingData.Count -gt 0) { Write-Host "Missing DataAccess migrations: $($missingData -join ', ')" -ForegroundColor Red }
    if ($missingApi.Count -gt 0) { Write-Host "Missing Identity migrations: $($missingApi -join ', ')" -ForegroundColor Red }
    exit 1
}

Write-Host "`nApplying database update (should report no pending explicit migrations)..." -ForegroundColor Cyan
& $Ef6 database update --assembly $dataDll --config (Join-Path $Root "LIS.DataModel\App.config") --connection-string $conn --connection-provider "System.Data.SqlClient" --migrations-config "LIS.DataAccess.Migrations.Configuration" 2>&1 | Write-Host
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $Ef6 database update --assembly $apiDll --config (Join-Path $Root "web\Lis.Api\Web.config") --connection-string $conn --connection-provider "System.Data.SqlClient" --migrations-config "Lis.Api.Migrations.Configuration" 2>&1 | Write-Host
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`nEF migrations are up to date." -ForegroundColor Green
