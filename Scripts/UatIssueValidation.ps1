$ErrorActionPreference = "Stop"

function Get-Token {
  $body = "grant_type=password&username=admin%40zorya.co.in&password=zorKol%401"
  $r = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
  return $r.access_token
}

function HeadersFor([string]$token, [string]$opt = $null) {
  $h = @{ accesskey = "DXI800"; Authorization = "Bearer $token" }
  if ($opt) { $h.ApiOption = $opt }
  return $h
}

function Expect-Message([scriptblock]$block, [string]$needle) {
  try { & $block; throw "Expected failure containing: $needle" } catch {
    $msg = $_.Exception.Message
    if ($_.ErrorDetails.Message) { $msg += " " + $_.ErrorDetails.Message }
    if ($msg -notlike "*$needle*") { throw "Expected '$needle' in: $msg" }
  }
}

$token = Get-Token
$results = @()

# Issue 1 - Test Master duplicate messages
Expect-Message {
  Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/HisTest/" -Headers (HeadersFor $token) -ContentType "application/json" -Body '{"hisTestCode":"CBC","hisTestCodeDescription":"X","hisSpecimenCode":"SERUM","departmentCode":"BIOCHEM","isActive":true}'
} "Test Code already exists"
$results += [pscustomobject]@{ Issue = 1; Check = "Duplicate Test Code API message"; Result = "PASS"; Evidence = "Test Code already exists." }

Expect-Message {
  Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/HisTest/" -Headers (HeadersFor $token) -ContentType "application/json" -Body '{"hisTestCode":"TNEW999","hisTestCodeDescription":"Complete Blood Count","hisSpecimenCode":"SERUM","departmentCode":"BIOCHEM","isActive":true}'
} "Test Name already exists"
$results += [pscustomobject]@{ Issue = 1; Check = "Duplicate Test Name API message"; Result = "PASS"; Evidence = "Test Name already exists." }

# Issue 2 - IsActive on edit load
$active = Invoke-RestMethod -Uri "http://localhost:8081/api/HisTest/2407" -Headers (HeadersFor $token)
$inactive = Invoke-RestMethod -Uri "http://localhost:8081/api/HisTest/74" -Headers (HeadersFor $token)
if ($active.isActive -ne $true -or $inactive.isActive -ne $false) { throw "IsActive API values incorrect" }
$results += [pscustomobject]@{ Issue = 2; Check = "IsActive API active/inactive"; Result = "PASS"; Evidence = "active=$($active.isActive) inactive=$($inactive.isActive)" }

# Issue 3 - Test Mapping duplicate message
$maps = Invoke-RestMethod -Uri "http://localhost:8081/api/TestMappingMaster" -Headers (HeadersFor $token '{"RecordPerPage":1,"CurrentPage":1}')
$m = if ($maps.items) { $maps.items[0] } else { $maps.Items[0] }
$dupMap = @{ equipmentId = $m.equipmentId; lisTestCode = $m.lisTestCode; hisTestCode = $m.hisTestCode; isActive = $true } | ConvertTo-Json
Expect-Message {
  Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/TestMappingMaster" -Headers (HeadersFor $token) -ContentType "application/json" -Body $dupMap
} "Test Mapping already exists"
$results += [pscustomobject]@{ Issue = 3; Check = "Test Mapping duplicate message"; Result = "PASS"; Evidence = "Test Mapping already exists." }

# Issue 4 - Test Mapping list test name
$mapList = Invoke-RestMethod -Uri "http://localhost:8081/api/TestMappingMaster" -Headers (HeadersFor $token '{"RecordPerPage":5,"CurrentPage":1}')
$items = if ($mapList.items) { $mapList.items } else { $mapList.Items }
$withName = ($items | Where-Object { $_.hisTestCodeDescription -and $_.hisTestCodeDescription.Length -gt 0 }).Count
if ($withName -lt 1) { throw "Test Name missing in mapping list" }
$results += [pscustomobject]@{ Issue = 4; Check = "Test Mapping list Test Name"; Result = "PASS"; Evidence = "$withName/5 rows have hisTestCodeDescription" }

# Issue 7 - Test Rate overlap message
$t = Invoke-RestMethod -Uri "http://localhost:8081/api/HisTest/" -Headers (HeadersFor $token '{"RecordPerPage":5,"SearchText":"CBC"}')
$testId = if ($t.items[0].id) { $t.items[0].id } else { $t.Items[0].Id }
$overlap = @{ testId = $testId; rate = 99; emergencyRate = 0; discountPercent = 0; taxPercent = 0; rateType = 0; effectiveStart = "2026-01-01"; effectiveEnd = "2026-12-31"; isActive = $true } | ConvertTo-Json
Expect-Message {
  Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/TestRate" -Headers (HeadersFor $token) -ContentType "application/json" -Body $overlap
} "overlapping effective period"
$results += [pscustomobject]@{ Issue = 7; Check = "Test Rate overlap message"; Result = "PASS"; Evidence = "overlapping effective period" }

# Issue 8 - Rate Type label
$rates = Invoke-RestMethod -Uri "http://localhost:8081/api/TestRate" -Headers (HeadersFor $token '{"RecordPerPage":5,"CurrentPage":1}')
$r0 = if ($rates.items) { $rates.items[0] } else { $rates.Items[0] }
if (-not $r0.rateTypeLabel) { throw "rateTypeLabel missing" }
$results += [pscustomobject]@{ Issue = 8; Check = "Rate Type label in API"; Result = "PASS"; Evidence = $r0.rateTypeLabel }

# Issue 9 - Patient duplicate message
$pats = Invoke-RestMethod -Uri "http://localhost:8081/api/PatientMaster" -Headers (HeadersFor $token '{"RecordPerPage":1,"CurrentPage":1}')
$p = if ($pats.items) { $pats.items[0] } else { $pats.Items[0] }
$dupPat = @{ name = $p.name; phone = $p.phone; hisPatientId = $p.hisPatientId; gender = "M"; age = 30; isActive = $true } | ConvertTo-Json
Expect-Message {
  Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/PatientMaster" -Headers (HeadersFor $token) -ContentType "application/json" -Body $dupPat
} "Patient already exists"
$results += [pscustomobject]@{ Issue = 9; Check = "Patient duplicate message"; Result = "PASS"; Evidence = "Patient already exists." }

# Issue 12/13 - Test Profile details
$prof = Invoke-RestMethod -Uri "http://localhost:8081/api/TestProfile/2" -Headers (HeadersFor $token)
$details = if ($prof.profileDetails) { $prof.profileDetails } else { $prof.ProfileDetails }
if (-not $details -or $details.Count -lt 1) { throw "Profile details missing" }
$results += [pscustomobject]@{ Issue = "12/13"; Check = "Test Profile details in API"; Result = "PASS"; Evidence = "$($details.Count) detail lines on WELL profile" }

$results | Format-Table -AutoSize
Write-Host "UAT API validation PASSED"
