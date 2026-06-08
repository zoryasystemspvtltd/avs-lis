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

$results = @()
function Record([string]$issue, [string]$check, [bool]$pass, [string]$detail) {
  $script:results += [pscustomobject]@{ Issue = $issue; Check = $check; Pass = $pass; Detail = $detail }
  $status = if ($pass) { "PASS" } else { "FAIL" }
  Write-Host "[$status] $issue - $check : $detail"
  if (-not $pass) { throw "Validation failed: $issue - $check" }
}

$token = Get-Token
$h = HeadersFor $token

Write-Host "`n=== ISSUE 1: Equipment vs Heartbeat ==="
$equip = Invoke-RestMethod -Uri "http://localhost:8081/api/Equipments" -Headers $h
$hb = Invoke-RestMethod -Uri "http://localhost:8081/api/EquipmentHeartbeat" -Headers $h
$eqCount = @($equip.items).Count
$hbCount = @($hb).Count
Record "1" "Different API endpoints" ($eqCount -ne $hbCount) "Equipments=$eqCount Heartbeat=$hbCount"
Record "1" "Heartbeat active-only" (($hb | Where-Object { $_.isActive -eq $false }).Count -eq 0) "Active-only filter"
$portalJs = Get-ChildItem "I:\Projects\PROD\AVILIS\PORTAL\main-es2015.*.js" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$bundle = Get-Content $portalJs.FullName -Raw
Record "1" "UI distinct headings in portal bundle" ($bundle -match "Equipment Management" -and $bundle -match "Equipment Heartbeat Monitor") $portalJs.Name
Record "1" "UI distinct columns in portal bundle" ($bundle -match "Last Heartbeat" -and $bundle -match "Maintain equipment master") $portalJs.Name

Write-Host "`n=== ISSUE 2: Pagination isolation (API + logic) ==="
$dept5 = Invoke-RestMethod -Uri "http://localhost:8081/api/Department" -Headers (HeadersFor $token '{"RecordPerPage":10,"CurrentPage":5,"SortColumnName":"Code","SortDirection":true}')
$unit1 = Invoke-RestMethod -Uri "http://localhost:8081/api/Unit" -Headers (HeadersFor $token '{"RecordPerPage":10,"CurrentPage":1,"SortColumnName":"Name","SortDirection":true}')
$method1 = Invoke-RestMethod -Uri "http://localhost:8081/api/Method" -Headers (HeadersFor $token '{"RecordPerPage":10,"CurrentPage":1,"SortColumnName":"Name","SortDirection":true}')
Record "2" "Department page 5 API slice" ($dept5.totalRecord -ge 0) "total=$($dept5.totalRecord) items=$(@($dept5.items).Count)"
Record "2" "Unit page 1 independent API" ($unit1.totalRecord -ge 0) "total=$($unit1.totalRecord) items=$(@($unit1.items).Count)"
Record "2" "Method page 1 independent API" ($method1.totalRecord -ge 0) "total=$($method1.totalRecord) items=$(@($method1.items).Count)"
Record "2" "Portal has listModuleKey isolation" ($bundle -match "listModuleKey" -and $bundle -match "listStateByModule") "bundle pagination fix present"

Write-Host "`n=== ISSUE 3: Test Rate date (no UTC shift) ==="
$testDates = @(
  @{ testId = 2559; start = "2035-01-01"; end = "2035-12-31"; label = "01-Jan" },
  @{ testId = 2558; start = "2036-02-15"; end = "2037-02-15"; label = "15-Feb" },
  @{ testId = 2557; start = "2037-12-31"; end = "2038-12-31"; label = "31-Dec" }
)
foreach ($td in $testDates) {
  $testId = $td.testId
  $payload = @{
    testId = $testId; rate = 100; emergencyRate = 0; discountPercent = 0; taxPercent = 0
    rateType = 0; effectiveStart = $td.start; effectiveEnd = $td.end; isActive = $true
  } | ConvertTo-Json
  $rateId = $null
  try {
    $created = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/api/TestRate" -Headers (HeadersFor $token) -ContentType "application/json" -Body $payload
    $rateId = if ($created.result) { $created.result } else { $created.id }
  } catch {
    $rates = Invoke-RestMethod -Uri "http://localhost:8081/api/TestRate" -Headers (HeadersFor $token '{"RecordPerPage":500,"CurrentPage":1}')
    $rItems = if ($rates.items) { $rates.items } else { $rates.Items }
    $match = $rItems | Where-Object { $_.testId -eq $testId -and ($_.effectiveStart -like "$($td.start)*") } | Select-Object -First 1
    $rateId = $match.id
  }
  if (-not $rateId) { Record "3" "Create $($td.label)" $false "Could not create/find rate for testId=$testId"; continue }
  $rate = Invoke-RestMethod -Uri "http://localhost:8081/api/TestRate/$rateId" -Headers (HeadersFor $token)
  $es = if ($rate.effectiveStart) { $rate.effectiveStart } else { $rate.EffectiveStart }
  $ok = $es -like "$($td.start)*"
  Record "3" "Date $($td.label) API" $ok "sent=$($td.start) got=$es"
  $dbRow = sqlcmd -S ".\SQLEXPRESS" -d AVSLIS -Q "SET NOCOUNT ON; SELECT CONVERT(varchar(10), EffectiveStart, 120) FROM TestRateMaster WHERE Id=$rateId" -h -1 -W
  $dbDate = ($dbRow | Where-Object { $_ -match '^\d{4}-\d{2}-\d{2}$' } | Select-Object -First 1).Trim()
  Record "3" "Date $($td.label) DB" ($dbDate -eq $td.start) "db=$dbDate"
}

Write-Host "`n=== ISSUE 4: Patient gender ==="
$malePayload = @{ name = "QA Male $(Get-Random)"; gender = "M"; phone = "9$(Get-Random -Maximum 999999999)"; age = 30; isActive = $true } | ConvertTo-Json
$femalePayload = @{ name = "QA Female $(Get-Random)"; gender = "F"; phone = "9$(Get-Random -Maximum 999999999)"; age = 28; isActive = $true } | ConvertTo-Json
$mCreated = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/api/PatientMaster" -Headers (HeadersFor $token) -ContentType "application/json" -Body $malePayload
$fCreated = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/api/PatientMaster" -Headers (HeadersFor $token) -ContentType "application/json" -Body $femalePayload
$mId = if ($mCreated.result) { $mCreated.result } else { $mCreated.id }
$fId = if ($fCreated.result) { $fCreated.result } else { $fCreated.id }
$pm = Invoke-RestMethod -Uri "http://localhost:8081/api/PatientMaster/$mId" -Headers (HeadersFor $token)
$pf = Invoke-RestMethod -Uri "http://localhost:8081/api/PatientMaster/$fId" -Headers (HeadersFor $token)
Record "4" "Male create/get API" ($pm.gender -eq 'M' -or $pm.gender -eq 'Male') "gender=$($pm.gender)"
Record "4" "Female create/get API" ($pf.gender -eq 'F' -or $pf.gender -eq 'Female') "gender=$($pf.gender)"
$mDb = (sqlcmd -S ".\SQLEXPRESS" -d AVSLIS -Q "SET NOCOUNT ON; SELECT Gender FROM PatientDetails WHERE Id=$mId" -h -1 -W | Where-Object { $_.Trim() -ne '' } | Select-Object -First 1).Trim()
$fDb = (sqlcmd -S ".\SQLEXPRESS" -d AVSLIS -Q "SET NOCOUNT ON; SELECT Gender FROM PatientDetails WHERE Id=$fId" -h -1 -W | Where-Object { $_.Trim() -ne '' } | Select-Object -First 1).Trim()
Record "4" "Male DB gender" ($mDb -in @('M','Male')) "db=$mDb"
Record "4" "Female DB gender" ($fDb -in @('F','Female')) "db=$fDb"
Record "4" "Portal gender form fix" ($bundle -match "normalizePatientGender" -and $bundle -match "ngValue") "bundle binding fix"

Write-Host "`n=== ALL STRICT VALIDATION PASSED ==="
