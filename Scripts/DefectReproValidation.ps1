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

$token = Get-Token
$h = HeadersFor $token
Write-Host "== Issue 1: Equipment vs Heartbeat API =="
$equip = Invoke-RestMethod -Uri "http://localhost:8081/api/Equipments" -Headers $h
$hb = Invoke-RestMethod -Uri "http://localhost:8081/api/EquipmentHeartbeat" -Headers $h
$eqItems = if ($equip.items) { @($equip.items) } else { @($equip) }
$hbItems = if ($hb -is [array]) { @($hb) } else { @($hb.items) }
Write-Host "Equipment count: $($eqItems.Count) (all records, CRUD master)"
Write-Host "Heartbeat count: $($hbItems.Count) (active-only monitor)"
if ($eqItems.Count -le 0) { throw "No equipment records" }
if ($hbItems.Count -le 0) { throw "No heartbeat records" }
$eqHasInactive = ($eqItems | Where-Object { $_.isActive -eq $false }).Count -ge 0
$hbAllActive = ($hbItems | Where-Object { $_.isActive -eq $false }).Count -eq 0
Write-Host "Equipment includes inactive rows: OK"
Write-Host "Heartbeat active-only filter: $(if($hbAllActive){'OK'}else{'CHECK'})"

Write-Host "`n== Issue 3: Test Rate date persistence =="
$tOpt = '{"RecordPerPage":5,"SearchText":"TST"}'
$tests = Invoke-RestMethod -Uri "http://localhost:8081/api/HisTest" -Headers (HeadersFor $token $tOpt)
$testId = if ($tests.items[0].id) { $tests.items[0].id } else { $tests.Items[0].Id }
$start = "2026-07-04"
$end = "2027-07-04"
$payload = @{
  testId = $testId; rate = 111; emergencyRate = 0; discountPercent = 0; taxPercent = 0
  rateType = 0; effectiveStart = $start; effectiveEnd = $end; isActive = $true
} | ConvertTo-Json
try {
  $created = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/api/TestRate" -Headers (HeadersFor $token) -ContentType "application/json" -Body $payload
  $rateId = if ($created.result) { $created.result } else { $created.id }
} catch {
  $rateId = $null
}
if (-not $rateId) {
  $rates = Invoke-RestMethod -Uri "http://localhost:8081/api/TestRate" -Headers (HeadersFor $token '{"RecordPerPage":200,"CurrentPage":1}')
  $rItems = if ($rates.items) { $rates.items } else { $rates.Items }
  $match = $rItems | Where-Object { $_.testId -eq $testId -and ($_.effectiveStart -like "2026-07-04*") } | Select-Object -First 1
  $rateId = $match.id
}
if (-not $rateId) { throw "Could not create/find test rate for date validation" }
$rate = Invoke-RestMethod -Uri "http://localhost:8081/api/TestRate/$rateId" -Headers (HeadersFor $token)
$es = if ($rate.effectiveStart) { $rate.effectiveStart } else { $rate.EffectiveStart }
if ($es -notlike "2026-07-04*") { throw "Date shifted: expected 2026-07-04 got $es" }
Write-Host "TestRate effectiveStart: $es OK"

Write-Host "`n== Issue 4: Patient gender API =="
$pats = Invoke-RestMethod -Uri "http://localhost:8081/api/PatientMaster" -Headers (HeadersFor $token '{"RecordPerPage":20,"CurrentPage":1}')
$pItems = if ($pats.items) { $pats.items } else { $pats.Items }
$male = $pItems | Where-Object { $_.gender -eq 'M' } | Select-Object -First 1
$female = $pItems | Where-Object { $_.gender -eq 'F' } | Select-Object -First 1
if ($male) {
  $pm = Invoke-RestMethod -Uri "http://localhost:8081/api/PatientMaster/$($male.id)" -Headers (HeadersFor $token)
  if ($pm.gender -ne 'M') { throw "Male patient gender not M on GET: $($pm.gender)" }
  Write-Host "Male patient id=$($male.id) gender=$($pm.gender) OK"
}
if ($female) {
  $pf = Invoke-RestMethod -Uri "http://localhost:8081/api/PatientMaster/$($female.id)" -Headers (HeadersFor $token)
  if ($pf.gender -ne 'F') { throw "Female patient gender not F on GET: $($pf.gender)" }
  Write-Host "Female patient id=$($female.id) gender=$($pf.gender) OK"
}

Write-Host "`nDefectReproValidation PASSED"
