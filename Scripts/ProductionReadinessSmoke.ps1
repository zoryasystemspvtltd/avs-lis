$ErrorActionPreference = "Stop"

function Get-Token {
  $body = "grant_type=password&username=admin%40zorya.co.in&password=zorKol%401"
  $r = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
  if (-not $r -or -not $r.access_token) { throw "Token fetch failed." }
  return $r.access_token
}

function HeadersFor([string]$token, [string]$apiOptionJson = $null) {
  $h = @{ accesskey = "DXI800"; Authorization = ("Bearer " + $token) }
  if ($apiOptionJson) { $h.ApiOption = $apiOptionJson }
  return $h
}

function Assert-True([bool]$cond, [string]$msg) { if (-not $cond) { throw $msg } }
function Assert-Contains([string]$text, [string]$needle, [string]$msg) {
  if ($text -notlike ("*" + $needle + "*")) { throw $msg }
}

function Get-Items($resp) {
  if ($resp.Items) { return @($resp.Items) }
  if ($resp.items) { return @($resp.items) }
  return @()
}

function Expect-Error([scriptblock]$block, [string]$needle) {
  try {
    & $block
    throw "Expected error containing '$needle' but call succeeded."
  } catch {
    $msg = $_.Exception.Message
    if ($_.ErrorDetails.Message) { $msg += " " + $_.ErrorDetails.Message }
    Assert-Contains $msg $needle "Expected error '$needle' but got: $msg"
  }
}

$token = Get-Token
Write-Host "Token OK"

# A. Test Master - SERUM specimen + duplicates
Write-Host "== A. Test Master =="
$spOpt = '{"RecordPerPage":500,"CurrentPage":1,"SortColumnName":"Code","SortDirection":false,"SearchText":"SERUM"}'
$spList = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/Specimens" -Headers (HeadersFor $token $spOpt)
$serum = Get-Items $spList | Where-Object { ($_.Code -eq "SERUM") -or ($_.code -eq "SERUM") } | Select-Object -First 1
Assert-True ($null -ne $serum) "SERUM specimen not found in Specimens API (page 500)"

$deptList = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/Department" -Headers (HeadersFor $token)
$dept = ($deptList | Select-Object -First 1)
$deptCode = if ($dept.Code) { $dept.Code } else { $dept.code }

$dupCode = "CBC"
$dupPayload = @{ hisTestCode = $dupCode; hisTestCodeDescription = "Dup Name"; hisSpecimenCode = "SERUM"; departmentCode = $deptCode; isActive = $true } | ConvertTo-Json
Expect-Error { Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/HisTest" -Headers (HeadersFor $token) -ContentType "application/json" -Body $dupPayload } "Test Code already exists"

$uniq = ("PRA" + (Get-Random -Maximum 99999))
$dupNamePayload = @{ hisTestCode = $uniq; hisTestCodeDescription = "Complete Blood Count"; hisSpecimenCode = "SERUM"; departmentCode = $deptCode; isActive = $true } | ConvertTo-Json
Expect-Error { Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/HisTest" -Headers (HeadersFor $token) -ContentType "application/json" -Body $dupNamePayload } "Test Name already exists"
Write-Host "Test Master SERUM + duplicate blocks OK"

# B. Test Rate overlap + rate type label
Write-Host "== B. Test Rate =="
$tOpt = '{"RecordPerPage":5,"CurrentPage":1,"SortColumnName":"HISTestCode","SortDirection":false,"SearchText":"CBC"}'
$tList = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/HisTest" -Headers (HeadersFor $token $tOpt)
$cbc = Get-Items $tList | Where-Object { ($_.HISTestCode -eq "CBC") -or ($_.hisTestCode -eq "CBC") } | Select-Object -First 1
Assert-True ($null -ne $cbc) "CBC test not found"
$testId = if ($cbc.Id) { $cbc.Id } else { $cbc.id }

$rateOpt = '{"RecordPerPage":10,"CurrentPage":1,"SortColumnName":"EffectiveStart","SortDirection":false}'
$rateList = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/TestRate" -Headers (HeadersFor $token $rateOpt)
$rates = Get-Items $rateList
Assert-True ($rates.Count -gt 0) "No test rates in list"
$firstRate = $rates[0]
$rtLabel = if ($firstRate.RateTypeLabel) { $firstRate.RateTypeLabel } else { $firstRate.rateTypeLabel }
Assert-True ($rtLabel -and $rtLabel.Length -gt 0) "RateTypeLabel missing on Test Rate list item"

$overlapPayload = @{
  testId = $testId; rate = 100; emergencyRate = 0; discountPercent = 0; taxPercent = 0
  rateType = 0; effectiveStart = "2026-01-01"; effectiveEnd = "2026-12-31"; isActive = $true
} | ConvertTo-Json
Expect-Error { Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/TestRate" -Headers (HeadersFor $token) -ContentType "application/json" -Body $overlapPayload } "overlapping effective period"
Write-Host "Test Rate overlap block + RateTypeLabel OK"

# C. Test Mapping duplicate
Write-Host "== C. Test Mapping =="
$mapListOpt = '{"RecordPerPage":5,"CurrentPage":1,"SortColumnName":"HISTestCode","SortDirection":false}'
$mapList = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/TestMappingMaster" -Headers (HeadersFor $token $mapListOpt)
$maps = Get-Items $mapList
Assert-True ($maps.Count -gt 0) "No test mappings"
$m0 = $maps[0]
$tn = if ($m0.HISTestCodeDescription) { $m0.HISTestCodeDescription } else { $m0.hisTestCodeDescription }
Assert-True ($tn -and $tn.Length -gt 0) "Test Name (HISTestCodeDescription) missing in mapping list"

$dupMap = @{
  equipmentId = if ($m0.EquipmentId) { $m0.EquipmentId } else { $m0.equipmentId }
  lisTestCode = if ($m0.LISTestCode) { $m0.LISTestCode } else { $m0.lisTestCode }
  hisTestCode = if ($m0.HISTestCode) { $m0.HISTestCode } else { $m0.hisTestCode }
  isActive = $true
} | ConvertTo-Json
Expect-Error { Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/TestMappingMaster" -Headers (HeadersFor $token) -ContentType "application/json" -Body $dupMap } "Test Mapping already exists"
Write-Host "Test Mapping duplicate block + Test Name OK"

# D. Patient duplicate
Write-Host "== D. Patient =="
$patOpt = '{"RecordPerPage":1,"CurrentPage":1,"SortColumnName":"Name","SortDirection":false}'
$patList = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/PatientMaster" -Headers (HeadersFor $token $patOpt)
$pats = Get-Items $patList
if ($pats.Count -gt 0) {
  $p = $pats[0]
  $dupPat = @{
    name = if ($p.Name) { $p.Name } else { $p.name }
    phone = if ($p.Phone) { $p.Phone } else { $p.phone }
    hisPatientId = if ($p.HisPatientId) { $p.HisPatientId } else { $p.hisPatientId }
    gender = "M"; age = 30; isActive = $true
  } | ConvertTo-Json
  Expect-Error { Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/PatientMaster" -Headers (HeadersFor $token) -ContentType "application/json" -Body $dupPat } "Patient already exists"
}
Write-Host "Patient duplicate block OK"

# H. Sale Invoice IsActive
Write-Host "== H. Sale Invoice =="
$invOpt = '{"RecordPerPage":5,"CurrentPage":1,"SortColumnName":"InvoiceDate","SortDirection":false}'
$invList = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/SaleInvoice" -Headers (HeadersFor $token $invOpt)
$invs = Get-Items $invList
Assert-True ($invs.Count -gt 0) "No sale invoices visible (IsActive filter may be broken)"
Write-Host "Sale Invoice list visibility OK ($($invs.Count) rows)"

# I. Edit Test Results API
Write-Host "== I. Edit Test Results =="
$etrOpt = '{"fromDate":"2020-01-01","toDate":"2030-12-31"}'
try {
  $etr = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/TestResultEdit/search" -Headers (HeadersFor $token $etrOpt)
  Assert-True ($null -ne $etr) "TestResultEdit search returned null"
  Write-Host "TestResultEdit search OK"
} catch {
  throw "TestResultEdit API failed: $($_.Exception.Message)"
}

# L. Reports API
Write-Host "== L. Reports =="
$repOpt = '{"RecordPerPage":5,"CurrentPage":1,"SortColumnName":"InvoiceDate","SortDirection":false,"FromDate":"2020-01-01","ToDate":"2030-12-31"}'
$sir = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/Reports/SaleInvoiceRegister" -Headers (HeadersFor $token $repOpt)
$tbr = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/Reports/TestBookingRegister" -Headers (HeadersFor $token $repOpt)
Assert-True ($null -ne $sir) "SaleInvoiceRegister returned null"
Assert-True ($null -ne $tbr) "TestBookingRegister returned null"
Write-Host "Reports API OK"

Write-Host ""
Write-Host "ProductionReadinessSmoke PASSED"
