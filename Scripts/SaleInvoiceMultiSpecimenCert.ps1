# Sale Invoice - multi test + profile with different specimen types (API + DB certification)
$ErrorActionPreference = "Stop"
$baseApi = "http://localhost:8081"
$results = @()
$tag = "SI-MULTI-SPEC-" + (Get-Date -Format "yyyyMMddHHmmss")

function Cert([string]$area, [string]$test, [string]$status, [string]$detail) {
  $script:results += [pscustomobject]@{ Area = $area; Test = $test; Status = $status; Detail = $detail }
  $c = switch ($status) { "PASS" { "Green" } "FAIL" { "Red" } default { "Yellow" } }
  Write-Host "[$status] $area :: $test - $detail" -ForegroundColor $c
  if ($status -eq "FAIL") { throw "CERTIFICATION FAILED: $area :: $test - $detail" }
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

function Get-Items($r) {
  if ($null -eq $r) { return @() }
  $val = $r.items; if ($null -eq $val) { $val = $r.Items }
  if ($null -eq $val) { return @() }
  if ($val -is [System.Array]) { return ,$val }
  return @($val)
}

function SqlRows([string]$query) {
  $raw = & sqlcmd -S '.\SQLEXPRESS' -d ZoryaLMS -E -h -1 -W -s "`t" -Q "SET NOCOUNT ON; $query" 2>&1
  $lines = @($raw | Where-Object { $_ -and $_.ToString().Trim() -ne "" -and $_ -notmatch '^\(\d+ rows affected\)' })
  return $lines
}

function Parse-SqlRow([string]$line) {
  $parts = if ($line -match "`t") {
    @($line -split "`t" | ForEach-Object { $_.Trim() })
  } elseif ($line -match '\|') {
    @($line -split '\|' | ForEach-Object { $_.Trim() })
  } else {
    @($line.Trim() -split '\s+')
  }
  return ,$parts
}

Write-Host "========== SALE INVOICE MULTI-SPECIMEN CERT ($tag) ==========" -ForegroundColor Cyan

$apiCode = (Invoke-WebRequest -Uri "$baseApi/api/Department" -UseBasicParsing -TimeoutSec 30).StatusCode
Cert "ENV" "API reachable" $(if ($apiCode -eq 200) { "PASS" } else { "FAIL" }) "status=$apiCode"

$token = Get-Token
Cert "ENV" "API auth token" "PASS" "acquired"

# Find two tests with different specimen codes (rated for today)
$specimenRows = @(SqlRows "SELECT TOP 5 Code FROM HISSpecimenMaster WHERE IsActive=1 ORDER BY Code")
Cert "DB" "At least 2 active specimens" $(if ($specimenRows.Count -ge 2) { "PASS" } else { "FAIL" }) "count=$($specimenRows.Count)"

$specA = 'SER'
$specB = 'EDT'
if ($specimenRows -notcontains $specA) { $specA = [string]($specimenRows | Where-Object { $_ -ne 'CRUD-SP' } | Select-Object -First 1) }
if ($specimenRows -notcontains $specB) { $specB = [string]($specimenRows | Where-Object { $_ -ne $specA -and $_ -ne 'CRUD-SP' } | Select-Object -First 1) }
Cert "DB" "Specimen pair selected" "PASS" "specA=$specA specB=$specB"

$testARow = @(SqlRows "SELECT TOP 1 t.Id, t.HISTestCode, t.HISSpecimenCode FROM HISTestMaster t INNER JOIN TestRateMaster r ON r.TestId = t.Id AND r.IsActive = 1 AND r.EffectiveStart <= CAST(GETDATE() AS date) AND r.EffectiveEnd >= CAST(GETDATE() AS date) INNER JOIN Department d ON d.Code = t.DepartmentCode AND d.ProcessingCategory = 'Laboratory' WHERE t.IsActive = 1 AND t.HISSpecimenCode = '$specA' ORDER BY t.Id")
$testBRow = @(SqlRows "SELECT TOP 1 t.Id, t.HISTestCode, t.HISSpecimenCode FROM HISTestMaster t INNER JOIN TestRateMaster r ON r.TestId = t.Id AND r.IsActive = 1 AND r.EffectiveStart <= CAST(GETDATE() AS date) AND r.EffectiveEnd >= CAST(GETDATE() AS date) INNER JOIN Department d ON d.Code = t.DepartmentCode AND d.ProcessingCategory = 'Laboratory' WHERE t.IsActive = 1 AND t.HISSpecimenCode = '$specB' ORDER BY t.Id")

if ($testARow.Count -lt 1 -or $testBRow.Count -lt 1) {
  Cert "DB" "Rated lab tests per specimen" "FAIL" "specA=$specA specB=$specB rowsA=$($testARow.Count) rowsB=$($testBRow.Count)"
}

function Split-SqlRow([string]$line) {
  if ([string]::IsNullOrWhiteSpace($line)) { return @() }
  if ($line.Contains("`t")) { return @($line.Split("`t") | ForEach-Object { $_.Trim() }) }
  return @($line.Trim())
}

$testA = Split-SqlRow ([string]$testARow[0])
$testB = Split-SqlRow ([string]$testBRow[0])
$testAId = [int]$testA[0]
$testBId = [int]$testB[0]
$testACode = [string]$testA[1]
$testBCode = [string]$testB[1]
Cert "DB" "Lab test A ($specA)" "PASS" "id=$testAId code=$testACode"
Cert "DB" "Lab test B ($specB)" "PASS" "id=$testBId code=$testBCode"

# Find or create profile with both specimen types
$profileRow = @(SqlRows "SELECT TOP 1 p.Id, p.Code FROM TestProfileMaster p WHERE p.IsActive = 1 AND EXISTS (SELECT 1 FROM TestProfileDetail pd INNER JOIN HISTestMaster t ON t.Id = pd.TestId WHERE pd.TestProfileId = p.Id AND t.HISSpecimenCode = '$specA' AND t.IsActive = 1) AND EXISTS (SELECT 1 FROM TestProfileDetail pd INNER JOIN HISTestMaster t ON t.Id = pd.TestId WHERE pd.TestProfileId = p.Id AND t.HISSpecimenCode = '$specB' AND t.IsActive = 1) ORDER BY p.Id DESC")

$profileId = 0
if ($profileRow.Count -gt 0) {
  $profileId = [int](Split-SqlRow ([string]$profileRow[0])[0])
  Cert "DB" "Multi-specimen profile exists" "PASS" "profileId=$profileId"
} else {
  # Use billable items API to find any profile
  $opt = (@{ RecordPerPage = 50; CurrentPage = 1; BillableItemType = "profile"; SearchText = "" } | ConvertTo-Json -Compress)
  $profiles = Get-Items (Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/BillableItems" -Headers (ApiHdr $token $opt))
  foreach ($p in $profiles) {
    $pid = $p.testProfileId; if (-not $pid) { $pid = $p.TestProfileId }
    if (-not $pid) { continue }
    $hier = Invoke-RestMethod -Uri "$baseApi/api/TestProfile/$pid/Hierarchy" -Headers (ApiHdr $token) -ErrorAction SilentlyContinue
    $details = @($hier.profileDetails); if ($details.Count -eq 0 -and $hier.ProfileDetails) { $details = @($hier.ProfileDetails) }
    $specs = @($details | ForEach-Object {
      $tid = $_.testId; if (-not $tid) { $tid = $_.TestId }
      if ($tid -eq $testAId) { $specA }
      elseif ($tid -eq $testBId) { $specB }
      else { $null }
    } | Where-Object { $_ } | Select-Object -Unique)
    if ($specs.Count -ge 2) { $profileId = [int]$pid; break }
  }
  if ($profileId -gt 0) {
    Cert "DB" "Multi-specimen profile via API" "PASS" "profileId=$profileId"
  } else {
    Cert "DB" "Multi-specimen profile" "WARN" "no multi-specimen profile found; testing standalone tests only"
  }
}

# Patient
$pats = Invoke-RestMethod -Uri "$baseApi/api/PatientMaster" -Headers (ApiHdr $token '{"RecordPerPage":1,"CurrentPage":1}')
$patientId = $pats.items[0].id
if (-not $patientId) { $patientId = $pats.Items[0].Id }
Cert "API" "Patient for invoice" "PASS" "patientId=$patientId"

$invNo = Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/NextInvoiceNo" -Headers (ApiHdr $token)
$details = @(
  @{ testId = $testAId; quantity = 1; rate = 0 }
  @{ testId = $testBId; quantity = 1; rate = 0 }
)
if ($profileId -gt 0) {
  $details += @{ testProfileId = $profileId; quantity = 1; rate = 0 }
}

$payload = @{
  invoice = @{
    id = 0; invoiceNo = $invNo; invoiceDate = (Get-Date -Format "yyyy-MM-dd")
    patientId = $patientId; invoiceStatus = 1; paymentStatus = 0; isActive = $true
    paymentType = "Cash"; discountType = "Fixed Amount"
  }
  details = $details
} | ConvertTo-Json -Depth 6

$saved = Invoke-RestMethod -Method Post -Uri "$baseApi/api/SaleInvoice" -Headers (ApiHdr $token) -ContentType "application/json" -Body $payload
$invId = $saved.result
if (-not $invId) { $invId = $saved.Result }
Cert "API" "Save multi-specimen invoice" $(if ($invId -gt 0) { "PASS" } else { "FAIL" }) "id=$invId no=$invNo"

$loaded = Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/$invId" -Headers (ApiHdr $token)
$detailRows = Get-Items $loaded.details; if ($detailRows.Count -eq 0) { $detailRows = Get-Items $loaded.Details }
$expectedLines = if ($profileId -gt 0) { 3 } else { 2 }
Cert "API" "Invoice line count" $(if ($detailRows.Count -eq $expectedLines) { "PASS" } else { "FAIL" }) "lines=$($detailRows.Count) expected=$expectedLines"

# DB: TestRequestDetails for this invoice
$reqRows = @(SqlRows "SELECT HISTestCode, SpecimenCode, SampleNo, ReportStatus FROM TestRequestDetails WHERE HISRequestNo = '$invNo' ORDER BY HISTestCode")
Cert "DB" "TestRequestDetails created" $(if ($reqRows.Count -ge 2) { "PASS" } else { "FAIL" }) "count=$($reqRows.Count)"

$sampleBySpec = @{}
$parsed = @()
foreach ($row in $reqRows) {
  $parts = Split-SqlRow ([string]$row)
  if ($parts.Count -lt 3) { continue }
  $parsed += [pscustomobject]@{
    TestCode = $parts[0].Trim()
    Specimen = $parts[1].Trim()
    SampleNo = $parts[2].Trim()
  }
  if ($parts[1].Trim()) {
    $sampleBySpec[$parts[1].Trim()] = $parts[2].Trim()
  }
}

$distinctSamples = @($parsed | Select-Object -ExpandProperty SampleNo -Unique)
Cert "DB" "Each request has SampleNo" $(if ($parsed.Count -gt 0 -and ($parsed | Where-Object { -not $_.SampleNo }).Count -eq 0) { "PASS" } else { "FAIL" }) "rows=$($parsed.Count)"

# Same specimen => same barcode
$sameSpecOk = $true
foreach ($spec in $sampleBySpec.Keys) {
  $samples = @($parsed | Where-Object { $_.Specimen -eq $spec } | Select-Object -ExpandProperty SampleNo -Unique)
  if ($samples.Count -gt 1) { $sameSpecOk = $false }
}
Cert "DB" "Same specimen reuses barcode" $(if ($sameSpecOk) { "PASS" } else { "FAIL" }) "specimens=$($sampleBySpec.Keys -join ',')"

# Different specimens => different barcodes (when both have specimen codes)
if ($sampleBySpec.Count -ge 2) {
  $vals = @($sampleBySpec.Values | Select-Object -Unique)
  Cert "DB" "Different specimens different barcodes" $(if ($vals.Count -ge 2) { "PASS" } else { "FAIL" }) "samples=$($vals -join ';')"
}

# Technician queue visibility
$opt = (@{ Status = 0; CurrentPage = 1; RecordPerPage = 200; SearchText = $invNo } | ConvertTo-Json -Compress)
$tech = Invoke-RestMethod -Uri "$baseApi/api/Patients" -Headers (ApiHdr $token $opt)
$techItems = Get-Items $tech
Cert "WF" "New requests in technician queue" $(if ($techItems.Count -ge 2) { "PASS" } else { "FAIL" }) "count=$($techItems.Count)"

# Sample collection pending queue
$optSc = (@{ RecordPerPage = 200; CurrentPage = 1; SearchText = $invNo } | ConvertTo-Json -Compress)
$sc = Invoke-RestMethod -Uri "$baseApi/api/SampleCollection/PendingQueue" -Headers (ApiHdr $token $optSc)
$scItems = Get-Items $sc
Cert "WF" "Sample collection queue" $(if ($scItems.Count -ge 1) { "PASS" } else { "WARN" }) "count=$($scItems.Count) (requires confirmed invoice + uncollected sample)"

# Cleanup — cancel invoice
Invoke-RestMethod -Method Put -Uri "$baseApi/api/SaleInvoice/Cancel/$invId" -Headers (ApiHdr $token) | Out-Null
Cert "API" "Cancel test invoice" "PASS" "id=$invId"

Write-Host ""
Write-Host "ALL MULTI-SPECIMEN CERTIFICATION CHECKS PASSED ($($results.Count) checks)" -ForegroundColor Green
$results | Format-Table -AutoSize
