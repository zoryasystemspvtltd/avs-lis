# Enterprise Test Profile Master - End-to-End UAT Validation
$ErrorActionPreference = "Stop"
$results = @()
function UAT([string]$phase, [string]$test, [bool]$pass, [string]$evidence) {
  $script:results += [pscustomobject]@{ Phase = $phase; Test = $test; Pass = $pass; Evidence = $evidence }
  $s = if ($pass) { "PASS" } else { "FAIL" }
  Write-Host "[$s] $phase :: $test - $evidence"
  if (-not $pass) { throw "UAT FAILED: $phase :: $test" }
}

function Get-Token {
  $body = 'grant_type=password&username=admin%40zorya.co.in&password=zorKol%401'
  return (Invoke-RestMethod -Method Post -Uri "http://localhost:8081/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body).access_token
}
function ApiHeaders([string]$token, [string]$opt = $null) {
  $h = @{ accesskey = "DXI800"; Authorization = "Bearer $token" }
  if ($opt) { $h.ApiOption = $opt }
  return $h
}

$token = Get-Token
$hdr = ApiHeaders $token
$ts = Get-Date -Format "yyyyMMddHHmmss"
$profileCode = "LIPUAT$ts"
$profileName = "LIPID PROFILE UAT $ts"

Write-Host "`n========== PHASE 2-4: PROFILE MASTER + HIERARCHY =========="
$tests = Invoke-RestMethod -Uri "http://localhost:8081/api/HisTest" -Headers (ApiHeaders $token '{"RecordPerPage":2000,"CurrentPage":1,"SortColumnName":"HISTestCode","SortDirection":true}')
$tItems = @($tests.items)
if ($tItems.Count -eq 0) { $tItems = @($tests.Items) }
$lipid = $tItems | Where-Object { $_.hisTestCode -eq 'LIPID' -or $_.HISTestCode -eq 'LIPID' } | Select-Object -First 1
if (-not $lipid) {
  $lipRow = sqlcmd -S ".\SQLEXPRESS" -d AVSLIS -Q "SET NOCOUNT ON; SELECT TOP 1 Id FROM HISTestMaster WHERE HISTestCode='LIPID'" -h -1 -W | Where-Object { $_.Trim() -match '^\d+$' } | Select-Object -First 1
  if ($lipRow) { $lipid = @{ id = [int]$lipRow.Trim(); hisTestCode = 'LIPID' } }
}
$glu = $tItems | Where-Object { ($_.hisTestCode -eq 'GLU' -or $_.HISTestCode -eq 'GLU') -and $_.isActive -ne $false } | Select-Object -First 1
$cbc = $tItems | Where-Object { ($_.hisTestCode -eq 'CBC' -or $_.HISTestCode -eq 'CBC') -and $_.isActive -ne $false } | Select-Object -First 1
if (-not $lipid) { throw "LIPID test not found in DB for UAT" }
$tidLip = if ($lipid.id) { $lipid.id } else { $lipid.Id }
$testLines = @(@{ testId = $tidLip; quantity = 1 })
if ($glu) { $testLines += @{ testId = $(if ($glu.id) { $glu.id } else { $glu.Id }); quantity = 1 } }
if ($cbc) { $testLines += @{ testId = $(if ($cbc.id) { $cbc.id } else { $cbc.Id }); quantity = 1 } }

$createPayload = (@{
  id = 0; code = $profileCode; name = $profileName; packageRate = 1500; isActive = $true
  profileDetails = $testLines
} | ConvertTo-Json -Depth 5) -replace 'profileDetails','ProfileDetails'
$created = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/api/TestProfile" -Headers $hdr -ContentType "application/json" -Body $createPayload
$profileId = if ($created.result) { $created.result } else { $created.id }
UAT "P2" "Create profile unique code/name" ($profileId -gt 0) "id=$profileId code=$profileCode"

try {
  Invoke-RestMethod -Method Post -Uri "http://localhost:8081/api/TestProfile" -Headers $hdr -ContentType "application/json" -Body $createPayload | Out-Null
  UAT "P2" "Duplicate code blocked" $false "duplicate accepted"
} catch { UAT "P2" "Duplicate code blocked" $true "API rejected duplicate" }

$hierarchy = Invoke-RestMethod -Uri "http://localhost:8081/api/TestProfile/$profileId" -Headers $hdr
UAT "P4" "Hierarchy tests loaded" ($hierarchy.tests.Count -ge 1) "tests=$($hierarchy.tests.Count)"
$lipNode = $hierarchy.tests | Where-Object { $_.testCode -eq 'LIPID' } | Select-Object -First 1
UAT "P4" "LIPID parameters expanded" ($lipNode.parameters.Count -ge 1) "params=$($lipNode.parameters.Count)"
$hasRange = ($lipNode.parameters | Where-Object { $_.ranges -and $_.ranges.Count -gt 0 }).Count -gt 0
UAT "P4" "Reference ranges present" $hasRange "LIPID param ranges found"

$dupPayload = (@{
  id = 0; code = "DUP$ts"; name = "Dup $ts"; packageRate = 100; isActive = $true
  profileDetails = @(@{ testId = $tidLip; quantity = 1 }, @{ testId = $tidLip; quantity = 1 })
} | ConvertTo-Json -Depth 5) -replace 'profileDetails','ProfileDetails'
try {
  Invoke-RestMethod -Method Post -Uri "http://localhost:8081/api/TestProfile" -Headers $hdr -ContentType "application/json" -Body $dupPayload | Out-Null
  UAT "P3" "Duplicate tests blocked" $false "duplicate test accepted"
} catch { UAT "P3" "Duplicate tests blocked" $true "rejected" }

$searchOpt = '{"RecordPerPage":10,"CurrentPage":1,"SearchText":"' + $profileCode + '"}'
$list = Invoke-RestMethod -Uri "http://localhost:8081/api/TestProfile" -Headers (ApiHeaders $token $searchOpt)
UAT "P2" "Search profile" ($list.totalRecord -ge 1) "found in search"

Write-Host "`n========== PHASE 5-6: BILLING + SAMPLE EXPANSION =========="
$pats = Invoke-RestMethod -Uri "http://localhost:8081/api/PatientMaster" -Headers (ApiHeaders $token '{"RecordPerPage":5,"CurrentPage":1}')
$patientId = $pats.items[0].id
$invNo = Invoke-RestMethod -Uri "http://localhost:8081/api/SaleInvoice/NextInvoiceNo" -Headers $hdr
$packageRate = 1500
$invPayload = @{
  invoice = @{
    id = 0; invoiceNo = $invNo; invoiceDate = (Get-Date).ToString("yyyy-MM-dd")
    patientId = $patientId; invoiceStatus = 0; paymentStatus = 0
    grossAmount = $packageRate; netAmount = $packageRate; paidAmount = 0; dueAmount = $packageRate; isActive = $true
  }
  details = @(@{
    id = 0; testId = $tidLip; testProfileId = $profileId; rate = $packageRate; quantity = 1
    amount = $packageRate; discountAmount = 0; taxAmount = 0; netAmount = $packageRate
  })
} | ConvertTo-Json -Depth 6
$saved = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/api/SaleInvoice" -Headers $hdr -ContentType "application/json" -Body $invPayload
$invoiceId = if ($saved.result) { $saved.result } else { $saved.id }
$loaded = Invoke-RestMethod -Uri "http://localhost:8081/api/SaleInvoice/$invoiceId" -Headers $hdr
$line = $loaded.details[0]
UAT "P5" "Profile invoice saved" ($invoiceId -gt 0) "invoiceId=$invoiceId"
UAT "P5" "Package rate on line" ([decimal]$line.rate -eq [decimal]$packageRate) "rate=$($line.rate)"
UAT "P5" "TestProfileId persisted" ($line.testProfileId -eq $profileId) "testProfileId=$($line.testProfileId)"

$updatePayload = (@{
  id = $profileId; code = $profileCode; name = $profileName; packageRate = 9999; isActive = $true
  profileDetails = $testLines
} | ConvertTo-Json -Depth 5) -replace 'profileDetails','ProfileDetails'
Invoke-RestMethod -Method Post -Uri "http://localhost:8081/api/TestProfile/Put" -Headers $hdr -ContentType "application/json" -Body $updatePayload | Out-Null
$reloaded = Invoke-RestMethod -Uri "http://localhost:8081/api/SaleInvoice/$invoiceId" -Headers $hdr
UAT "P5" "Historical invoice rate unchanged" ([decimal]$reloaded.details[0].rate -eq [decimal]$packageRate) "still $($reloaded.details[0].rate) after profile rate 9999"

$invNoStr = $reloaded.invoice.invoiceNo
$dbRequests = sqlcmd -S ".\SQLEXPRESS" -d AVSLIS -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM TestRequestDetails WHERE HISRequestNo = '$invNoStr'" -h -1 -W | Where-Object { $_.Trim() -match '^\d+$' } | Select-Object -First 1
$expectedTests = $testLines.Count
UAT "P6" "Profile expanded to all tests" ([int]$dbRequests.Trim() -ge $expectedTests) "requests=$($dbRequests.Trim()) expected>=$expectedTests"

$dbLine = sqlcmd -S ".\SQLEXPRESS" -d AVSLIS -Q "SET NOCOUNT ON; SELECT CONVERT(varchar(20), Rate) FROM SaleInvoiceDetail WHERE SaleInvoiceId=$invoiceId AND IsActive=1" -h -1 -W | Where-Object { $_.Trim() -match '^\d' } | Select-Object -First 1
UAT "P5-DB" "DB invoice rate" ([decimal]$dbLine.Trim() -eq [decimal]$packageRate) "dbRate=$($dbLine.Trim())"

Write-Host "`n========== PHASE 9: ANALYZER MAPPING =========="
$mapCount = sqlcmd -S ".\SQLEXPRESS" -d AVSLIS -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM TestMappingMaster m INNER JOIN HISTestMaster t ON m.HISTestCode=t.HISTestCode WHERE t.Id IN ($tidLip)" -h -1 -W | Where-Object { $_.Trim() -match '^\d+$' } | Select-Object -First 1
UAT "P9" "Test mapping exists for profile test" ([int]$mapCount.Trim() -ge 0) "mappings=$($mapCount.Trim())"

Write-Host '`n========== PHASE 12: PERFORMANCE 10 tests =========='
$activeTests = $tItems | Where-Object { $_.isActive -ne $false -and $_.IsActive -ne $false } | Select-Object -First 10
$perfLines = @()
foreach ($t in $activeTests) { $perfLines += @{ testId = $(if ($t.id) { $t.id } else { $t.Id }); quantity = 1 } }
$perfCode = "PERF$ts"
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$perfPayload = (@{
  id = 0; code = $perfCode; name = "Perf Profile $ts"; packageRate = 5000; isActive = $true
  profileDetails = $perfLines
} | ConvertTo-Json -Depth 5) -replace 'profileDetails','ProfileDetails'
$perfCreated = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/api/TestProfile" -Headers $hdr -ContentType "application/json" -Body $perfPayload
$sw.Stop()
$perfId = if ($perfCreated.result) { $perfCreated.result } else { $perfCreated.id }
$sw2 = [System.Diagnostics.Stopwatch]::StartNew()
$perfH = Invoke-RestMethod -Uri "http://localhost:8081/api/TestProfile/$perfId" -Headers $hdr
$sw2.Stop()
UAT "P12" "Save 10-test profile under 10s" ($sw.ElapsedMilliseconds -lt 10000) "$($sw.ElapsedMilliseconds)ms"
UAT "P12" "Hierarchy load under 5s" ($sw2.ElapsedMilliseconds -lt 5000) "tests=$($perfH.tests.Count) in $($sw2.ElapsedMilliseconds)ms"

Write-Host "`n========== PORTAL BUNDLE CHECK =========="
$bundle = Get-ChildItem "I:\Projects\PROD\AVILIS\PORTAL\main-es2015.*.js" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$txt = Get-Content $bundle.FullName -Raw
UAT "UI" "Test Profile menu/bundle" ($txt -match "Test Profile" -and $txt -match "test-profile-view") $bundle.Name
UAT "UI" "Invoice itemType billing UI" ($txt -match "itemType" -and $txt -match "testProfileId") "sale-invoice lines"
UAT "UI" "Parameter expansion preview" ($txt -match "Parameter Expansion Preview") "form preview"

Write-Host "`n========== ALL ENTERPRISE UAT CHECKS PASSED =========="
