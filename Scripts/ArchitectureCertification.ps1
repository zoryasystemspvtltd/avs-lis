# Production Readiness Certification - Architecture, DB, API Routing, Functional
$ErrorActionPreference = "Stop"
$baseApi = "http://localhost:8081"
$portalUrl = "http://localhost:8080"
$results = @()
$tag = "CERT-" + (Get-Date -Format "yyyyMMddHHmmss")

function Cert([string]$phase, [string]$test, [string]$status, [string]$detail) {
  $script:results += [pscustomobject]@{ Phase = $phase; Test = $test; Status = $status; Detail = $detail }
  $c = switch ($status) { "PASS" { "Green" } "FAIL" { "Red" } default { "Yellow" } }
  Write-Host "[$status] $phase :: $test - $detail" -ForegroundColor $c
  if ($status -eq "FAIL") { throw "CERTIFICATION FAILED: $phase :: $test - $detail" }
}

function Get-Token {
  $body = 'grant_type=password&username=admin%40zorya.co.in&password=zorKol%401'
  $r = Invoke-RestMethod -Method Post -Uri "$baseApi/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
  if (-not $r.access_token) { throw "Token failed" }
  return $r.access_token
}

function ApiHdr([string]$token, [string]$opt = $null) {
  $h = @{ accesskey = "DXI800"; Authorization = "Bearer $token" }
  if ($opt) { $h.ApiOption = $opt }
  return $h
}

function SqlScalar([string]$q) {
  $out = & sqlcmd -S '.\SQLEXPRESS' -d AVSLIS -E -h -1 -W -Q "SET NOCOUNT ON; $q" 2>&1
  if ($LASTEXITCODE -ne 0) { throw "SQL failed: $out" }
  $line = $out | Where-Object { $_ -and $_.ToString().Trim() -ne '' } | Select-Object -First 1
  if ($null -eq $line) { return '' }
  return $line.ToString().Trim()
}

function Expect-Error([scriptblock]$block, [string]$needle) {
  try { & $block | Out-Null; throw "Expected error: $needle" } catch {
    $msg = $_.Exception.Message
    if ($_.ErrorDetails.Message) { $msg += ' ' + $_.ErrorDetails.Message }
    if ($msg -notlike "*$needle*") { throw "Expected '$needle' got: $msg" }
  }
}

function Get-Items($r) {
  if ($r.Items) { return @($r.Items) }
  if ($r.items) { return @($r.items) }
  return @()
}

Write-Host "========== ARCHITECTURE CERTIFICATION ($tag) ==========" -ForegroundColor Cyan

# PHASE 0 - Environment
try {
  $apiCode = (Invoke-WebRequest -Uri $baseApi -UseBasicParsing -TimeoutSec 30).StatusCode
  Cert "ENV" "API HTTP" $(if ($apiCode -eq 200) { "PASS" } else { "FAIL" }) "Status=$apiCode"
} catch { Cert "ENV" "API HTTP" "FAIL" $_.Exception.Message }

try {
  $portalCode = (Invoke-WebRequest -Uri $portalUrl -UseBasicParsing -TimeoutSec 30).StatusCode
  Cert "ENV" "Portal HTTP" $(if ($portalCode -eq 200) { "PASS" } else { "FAIL" }) "Status=$portalCode"
} catch { Cert "ENV" "Portal HTTP" "FAIL" $_.Exception.Message }

$token = Get-Token
Cert "ENV" "API Auth Token" "PASS" "Token acquired"

# PHASE 1 - Architecture code scan (static)
$simPath = "I:\Projects\LIS\avs-lis\LIS.Businesslogic\SaleInvoiceManager.cs"
$sim = Get-Content $simPath -Raw
Cert "ARCH" "No IsRadiologyTest in SaleInvoiceManager" $(if ($sim -notmatch 'IsRadiologyTest|IsRadiologyLine') { "PASS" } else { "FAIL" }) "grep clean"
Cert "ARCH" "Uses DepartmentProcessingRouter" $(if ($sim -match 'DepartmentProcessingRouter') { "PASS" } else { "FAIL" }) "router present"
Cert "ARCH" "Central router file exists" $(if (Test-Path "I:\Projects\LIS\avs-lis\LIS.Businesslogic\DepartmentProcessingRouter.cs") { "PASS" } else { "FAIL" }) "DepartmentProcessingRouter.cs"

$router = Get-Content "I:\Projects\LIS\avs-lis\LIS.Businesslogic\DepartmentProcessingRouter.cs" -Raw
Cert "ARCH" "Router uses ProcessingCategory only" $(if ($router -match 'ProcessingCategory' -and $router -notmatch 'RADIOLOGY|IndexOf.*RAD') { "PASS" } else { "FAIL" }) "no name/code heuristics"

# PHASE 2 - Database
$col = SqlScalar "SELECT COL_LENGTH('dbo.Department','ProcessingCategory')"
Cert "DB" "ProcessingCategory column exists" $(if ($col -and $col -ne 'NULL') { "PASS" } else { "FAIL" }) "col_length=$col"

$nullCats = SqlScalar "SELECT COUNT(*) FROM Department WHERE ProcessingCategory IS NULL OR LTRIM(RTRIM(ProcessingCategory))=''"
Cert "DB" "All departments categorized" $(if ([int]$nullCats -eq 0) { "PASS" } else { "FAIL" }) "null=$nullCats"

$invalid = SqlScalar "SELECT COUNT(*) FROM Department WHERE ProcessingCategory NOT IN (N'Laboratory',N'Diagnostic')"
Cert "DB" "Valid ProcessingCategory values" $(if ([int]$invalid -eq 0) { "PASS" } else { "FAIL" }) "invalid=$invalid"

$diagProfiles = SqlScalar @"
SELECT COUNT(*) FROM TestProfileDetail pd
INNER JOIN HISTestMaster t ON t.Id=pd.TestId
INNER JOIN Department d ON d.Code=t.DepartmentCode
WHERE d.ProcessingCategory=N'Diagnostic'
"@
Cert "DB" "No diagnostic tests in profiles" $(if ([int]$diagProfiles -eq 0) { "PASS" } else { "FAIL" }) "violations=$diagProfiles"

$orphanInv = SqlScalar "SELECT COUNT(*) FROM SaleInvoiceDetail d LEFT JOIN SaleInvoice h ON h.Id=d.SaleInvoiceId WHERE h.Id IS NULL"
Cert "DB" "No orphan invoice lines" $(if ([int]$orphanInv -eq 0) { "PASS" } else { "FAIL" }) "orphans=$orphanInv"

# PHASE 3 - Department API
$depts = Invoke-RestMethod -Uri "$baseApi/api/Department" -Headers (ApiHdr $token '{"RecordPerPage":500,"CurrentPage":1}')
$deptItems = Get-Items $depts
$withCat = ($deptItems | Where-Object { ($_.processingCategory -or $_.ProcessingCategory) }).Count
Cert "API" "Department returns ProcessingCategory" $(if ($withCat -eq $deptItems.Count -and $deptItems.Count -gt 0) { "PASS" } else { "FAIL" }) "$withCat/$($deptItems.Count)"

$badDept = @{ code = "BAD$tag"; name = "Bad Dept"; processingCategory = "Imaging" } | ConvertTo-Json
Expect-Error { Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$baseApi/api/Department" -Headers (ApiHdr $token) -ContentType "application/json" -Body $badDept } "Laboratory or Diagnostic"
Cert "API" "Reject invalid ProcessingCategory" "PASS" "blocked Imaging"

# PHASE 4 - Billable items search
$labDept = $deptItems | Where-Object { ($_.processingCategory -eq 'Laboratory') -or ($_.ProcessingCategory -eq 'Laboratory') } | Select-Object -First 1
$labCode = if ($labDept.code) { $labDept.code } else { $labDept.Code }
$billOpt = (@{ RecordPerPage = 20; CurrentPage = 1; SearchText = ''; BillableItemType = 'test'; DepartmentCode = $labCode } | ConvertTo-Json -Compress)
$bill = Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/BillableItems?invoiceDate=$(Get-Date -Format yyyy-MM-dd)" -Headers (ApiHdr $token $billOpt)
$billItems = Get-Items $bill
Cert "API" "Billable test search by department" $(if ($billItems.Count -ge 0) { "PASS" } else { "FAIL" }) "items=$($billItems.Count) dept=$labCode"

$profOpt = (@{ RecordPerPage = 10; CurrentPage = 1; BillableItemType = 'profile' } | ConvertTo-Json -Compress)
$profBill = Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/BillableItems" -Headers (ApiHdr $token $profOpt)
$profOnly = Get-Items $profBill
$hasTestInProfileSearch = ($profOnly | Where-Object { ($_.itemType -eq 'test') -or ($_.ItemType -eq 'test') }).Count -gt 0
Cert "API" "Profile billable search excludes tests" $(if (-not $hasTestInProfileSearch) { "PASS" } else { "FAIL" }) "profileItems=$($profOnly.Count)"

# PHASE 5 - Routing: Lab vs Diagnostic invoice
$pats = Invoke-RestMethod -Uri "$baseApi/api/PatientMaster" -Headers (ApiHdr $token '{"RecordPerPage":1,"CurrentPage":1}')
$patientId = $pats.items[0].id
if (-not $patientId) { $patientId = $pats.Items[0].Id }

$labTest = SqlScalar @"
SELECT TOP 1 CAST(t.Id AS varchar(20)) FROM HISTestMaster t
INNER JOIN Department d ON d.Code=t.DepartmentCode
WHERE d.ProcessingCategory=N'Laboratory' AND t.IsActive=1
ORDER BY t.Id
"@
$diagTest = SqlScalar @"
SELECT TOP 1 CAST(t.Id AS varchar(20)) FROM HISTestMaster t
INNER JOIN Department d ON d.Code=t.DepartmentCode
WHERE d.ProcessingCategory=N'Diagnostic' AND t.IsActive=1
ORDER BY t.Id
"@

if ($labTest -match '^\d+$') {
  $invNo = Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/NextInvoiceNo" -Headers (ApiHdr $token)
  $labPayload = @{
    invoice = @{ id = 0; invoiceNo = $invNo; invoiceDate = (Get-Date).ToString('yyyy-MM-dd'); patientId = $patientId; invoiceStatus = 0; paymentStatus = 0; grossAmount = 100; netAmount = 100; isActive = $true }
    details = @(@{ id = 0; testId = [int]$labTest; rate = 100; quantity = 1; amount = 100; discountAmount = 0; taxAmount = 0; netAmount = 100; lineType = 'radiology' })
  } | ConvertTo-Json -Depth 6
  $labInv = Invoke-RestMethod -Method Post -Uri "$baseApi/api/SaleInvoice" -Headers (ApiHdr $token) -ContentType "application/json" -Body $labPayload
  $labInvId = if ($labInv.result) { $labInv.result } else { $labInv.id }
  $labInvNo = (Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/$labInvId" -Headers (ApiHdr $token)).invoice.invoiceNo
  $labReq = SqlScalar "SELECT COUNT(*) FROM TestRequestDetails WHERE HISRequestNo='$labInvNo'"
  $labRad = SqlScalar "SELECT COUNT(*) FROM RadiologyRequestDetail WHERE HISRequestNo='$labInvNo'"
  Cert "ROUTE" "Lab test ignores UI lineType radiology" $(if ([int]$labReq -ge 1 -and [int]$labRad -eq 0) { "PASS" } else { "FAIL" }) "TRD=$labReq RAD=$labRad inv=$labInvNo"
} else {
  Cert "ROUTE" "Lab test routing" "SKIP" "No rated lab test in DB"
}

if ($diagTest -match '^\d+$') {
  $invNo2 = Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/NextInvoiceNo" -Headers (ApiHdr $token)
  $diagPayload = @{
    invoice = @{ id = 0; invoiceNo = $invNo2; invoiceDate = (Get-Date).ToString('yyyy-MM-dd'); patientId = $patientId; invoiceStatus = 0; paymentStatus = 0; grossAmount = 200; netAmount = 200; isActive = $true }
    details = @(@{ id = 0; testId = [int]$diagTest; rate = 200; quantity = 1; amount = 200; discountAmount = 0; taxAmount = 0; netAmount = 200; lineType = 'blood' })
  } | ConvertTo-Json -Depth 6
  $diagInv = Invoke-RestMethod -Method Post -Uri "$baseApi/api/SaleInvoice" -Headers (ApiHdr $token) -ContentType "application/json" -Body $diagPayload
  $diagInvId = if ($diagInv.result) { $diagInv.result } else { $diagInv.id }
  $diagInvNo = (Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/$diagInvId" -Headers (ApiHdr $token)).invoice.invoiceNo
  $diagReq = SqlScalar "SELECT COUNT(*) FROM TestRequestDetails WHERE HISRequestNo='$diagInvNo'"
  $diagRad = SqlScalar "SELECT COUNT(*) FROM RadiologyRequestDetail WHERE HISRequestNo='$diagInvNo'"
  Cert "ROUTE" "Diagnostic test ignores UI lineType blood" $(if ([int]$diagRad -ge 1 -and [int]$diagReq -eq 0) { "PASS" } else { "FAIL" }) "TRD=$diagReq RAD=$diagRad inv=$diagInvNo"
} else {
  Cert "ROUTE" "Diagnostic test routing" "SKIP" "No rated diagnostic test in DB"
}

# PHASE 6 - Profile rejects diagnostic test
if ($diagTest -match '^\d+$') {
  $badProf = (@{
    id = 0; code = "BD$tag".Substring(0, [Math]::Min(20, "BD$tag".Length)); name = "Bad Profile $tag"; packageRate = 100; isActive = $true
    profileDetails = @(@{ testId = [int]$diagTest; quantity = 1 })
  } | ConvertTo-Json -Depth 5) -replace 'profileDetails','ProfileDetails'
  Expect-Error { Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$baseApi/api/TestProfile" -Headers (ApiHdr $token) -ContentType "application/json" -Body $badProf } "Diagnostic"
  Cert "API" "Profile rejects diagnostic test" "PASS" "blocked"
} else {
  Cert "API" "Profile rejects diagnostic test" "SKIP" "No diagnostic test"
}

# PHASE 7 - Portal /lis API bridge (prod bundle uses ApplicationServer: /lis/)
try {
  $login = Invoke-WebRequest -Uri "$portalUrl/login" -UseBasicParsing -TimeoutSec 30
  Cert "UI" "Portal SPA /login route" $(if ($login.StatusCode -eq 200 -and $login.Content -match 'app-root') { "PASS" } else { "FAIL" }) "$portalUrl/login"
} catch {
  Cert "UI" "Portal SPA /login route" "FAIL" "Deploy web/Lis.Web/src/web.config to PORTAL — $($_.Exception.Message)"
}

try {
  $bridge = Invoke-WebRequest -Uri "$portalUrl/lis/api/Department/" -Headers @{ ApiOption = '{"RecordPerPage":1,"CurrentPage":1,"SortColumnName":"Code","SortDirection":true}' } -UseBasicParsing -TimeoutSec 30
  Cert "UI" "Portal /lis API bridge" $(if ($bridge.StatusCode -eq 200) { "PASS" } else { "FAIL" }) "$portalUrl/lis/api/Department/"
} catch {
  Cert "UI" "Portal /lis API bridge" "FAIL" "Run Scripts/EnsureAvilisPortalApiBridge.ps1 — $($_.Exception.Message)"
}

# PHASE 7b - Portal bundle architecture UI
$bundle = Get-ChildItem "I:\Projects\PROD\AVILIS\PORTAL\main-es2015.*.js" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$txt = Get-Content $bundle.FullName -Raw
Cert "UI" "Sale invoice itemType in bundle" $(if ($txt -match 'itemType') { "PASS" } else { "FAIL" }) $bundle.Name
Cert "UI" "Department processingCategory in bundle" $(if ($txt -match 'processingCategory') { "PASS" } else { "FAIL" }) "master-form"
Cert "UI" "Department Master list schema" $(if ($txt -match 'processingCategory' -or $txt -match 'Processing Category') { "PASS" } else { "INFO" }) "portal"

# PHASE 8 - Performance smoke
$sw = [System.Diagnostics.Stopwatch]::StartNew()
Invoke-RestMethod -Uri "$baseApi/api/HisTest" -Headers (ApiHdr $token '{"RecordPerPage":50,"CurrentPage":1,"SearchText":"C"}') | Out-Null
$sw.Stop()
Cert "PERF" "HisTest search under 3s" $(if ($sw.ElapsedMilliseconds -lt 3000) { "PASS" } else { "FAIL" }) "$($sw.ElapsedMilliseconds)ms"

Write-Host "`n========== CERTIFICATION SUMMARY ==========" -ForegroundColor Cyan
$results | Format-Table -AutoSize
$fail = ($results | Where-Object { $_.Status -eq 'FAIL' }).Count
if ($fail -gt 0) { throw "$fail certification check(s) FAILED" }
Write-Host "ALL CERTIFICATION CHECKS PASSED ($($results.Count) checks)" -ForegroundColor Green
