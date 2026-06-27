# Creates fresh ZoryaLMS database, applies EF migrations, and runs seed scripts.
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Server = ".\SQLEXPRESS"
$Database = "ZoryaLMS"
$Ef6 = Join-Path $Root "packages\EntityFramework.6.4.4\tools\net45\any\ef6.exe"

function Invoke-Sql([string]$Query) {
    $out = sqlcmd -S $Server -Q $Query -b 2>&1
    if ($LASTEXITCODE -ne 0) { throw "SQL failed: $out" }
    return $out
}

Write-Host "=== ZoryaLMS fresh database initialization ===" -ForegroundColor Cyan

Write-Host "[1/8] Stopping IIS to release DB connections..."
iisreset /stop | Out-Null
Start-Sleep -Seconds 3

Write-Host "[2/8] Recreating database $Database..."
Invoke-Sql "IF DB_ID(N'$Database') IS NOT NULL BEGIN ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Database]; END"
Invoke-Sql "CREATE DATABASE [$Database];"

Write-Host "[3/8] Building solution..."
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
& $msbuild (Join-Path $Root "web\Lis.Api\Lis.Api.csproj") /t:Build /p:Configuration=Release /v:minimal | Out-Null
& $msbuild (Join-Path $Root "LIS.DataModel\LIS.DataAccess.csproj") /t:Build /p:Configuration=Release /v:minimal | Out-Null

$apiDll = Join-Path $Root "web\Lis.Api\bin\Lis.Api.dll"
$dataDll = Join-Path $Root "LIS.DataModel\bin\Release\LIS.DataAccess.dll"
$apiConfig = Join-Path $Root "web\Lis.Api\Web.config"
$dataConfig = Join-Path $Root "LIS.DataModel\App.config"
$conn = "Server=$Server;Database=$Database;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true"

Write-Host "[4/8] Applying Lis.Api (Identity) migrations..."
& $Ef6 database update `
    --assembly $apiDll `
    --config $apiConfig `
    --connection-string $conn `
    --connection-provider "System.Data.SqlClient" `
    --migrations-config "Lis.Api.Migrations.Configuration" `
    --verbose
if ($LASTEXITCODE -ne 0) { throw "Lis.Api migration failed." }

Write-Host "[5/8] Applying LIS.DataAccess migrations..."
& $Ef6 database update `
    --assembly $dataDll `
    --config $dataConfig `
    --connection-string $conn `
    --connection-provider "System.Data.SqlClient" `
    --migrations-config "LIS.DataAccess.Migrations.Configuration" `
    --verbose
if ($LASTEXITCODE -ne 0) { throw "LIS.DataAccess migration failed." }

Write-Host "[6/9] Seeding identity bootstrap..."
sqlcmd -S $Server -d $Database -i (Join-Path $PSScriptRoot "SeedZoryaLMSIdentityBootstrap.sql") -b
if ($LASTEXITCODE -ne 0) { throw "Identity bootstrap failed." }

Write-Host "[7/9] Granting IIS app pool access..."
sqlcmd -S $Server -d $Database -i (Join-Path $PSScriptRoot "GrantZoryaLMSIisAccess.sql") -b
if ($LASTEXITCODE -ne 0) { throw "IIS access grant failed." }

Write-Host "[8/9] Seeding masters and demo data..."
$seedScripts = @(
    "add-patient-mr-visit-prefix.sql",
    "sale-invoice-detail-requestdetail-nullable.sql",
    "fdd-sample-radiology-schema.sql",
    "SeedMasterModules.sql",
    "SeedSampleData.sql",
    "SeedFullDatabaseInsert.sql",
    "SeedAllCrudTestData.sql"
)
foreach ($script in $seedScripts) {
    $path = Join-Path $PSScriptRoot $script
    if (-not (Test-Path $path)) { throw "Missing seed script: $path" }
    Write-Host "  -> $script"
    sqlcmd -S $Server -d $Database -i $path -b
    if ($LASTEXITCODE -ne 0) { throw "Seed failed: $script" }
}

Write-Host "[9/9] Verifying..."
$tableCount = sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'" -h -1 -W | Where-Object { $_ -match '^\d+$' } | Select-Object -First 1
$userCount = sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM AspNetUsers" -h -1 -W | Where-Object { $_ -match '^\d+$' } | Select-Object -First 1
Write-Host "Tables: $tableCount | Users: $userCount" -ForegroundColor Green

Write-Host "Restarting IIS..."
iisreset /start | Out-Null

Write-Host "=== ZoryaLMS initialization complete ===" -ForegroundColor Green
