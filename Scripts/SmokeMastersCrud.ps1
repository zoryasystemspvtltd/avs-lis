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

$token = Get-Token

function Smoke-LookupMaster([string]$name, [string]$api, [string]$codePrefix) {
  $code = ($codePrefix + (Get-Random -Maximum 9999).ToString("0000"))
  $payload = @{ code = $code; name = ($name + " " + $code); isActive = $true } | ConvertTo-Json

  Write-Host "== $name =="
  (Invoke-WebRequest -UseBasicParsing -Method Post -Uri ("http://localhost:8081/api/" + $api) -Headers (HeadersFor $token) -ContentType "application/json" -Body $payload) | Out-Null

  $all = Invoke-RestMethod -Method Get -Uri ("http://localhost:8081/api/" + $api + "/GetAll") -Headers (HeadersFor $token)
  $found = $false
  foreach ($m in ($all | ForEach-Object { $_ })) {
    if (($m.Code -eq $code) -or ($m.code -eq $code)) { $found = $true; break }
  }
  Assert-True $found ("$name not found in GetAll after create: " + $code)
  Write-Host "Create+GetAll OK: $code"
}

Smoke-LookupMaster "Unit" "Unit" "UQA"
Smoke-LookupMaster "Method" "Method" "MQA"

Write-Host "== Department =="
$deptCode = ("DQA" + (Get-Random -Maximum 9999).ToString("0000"))
$deptPayload = @{ code = $deptCode; name = ("Dept " + $deptCode) } | ConvertTo-Json
(Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/Department" -Headers (HeadersFor $token) -ContentType "application/json" -Body $deptPayload) | Out-Null
$deptList = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/Department" -Headers (HeadersFor $token)
Assert-True ($deptList | Where-Object { $_.Code -eq $deptCode -or $_.code -eq $deptCode } | Measure-Object).Count -gt 0 ("Department not found after create: " + $deptCode)
Write-Host "Department create+list OK: $deptCode"

Write-Host "== Specimen =="
$spCode = ("SQA" + (Get-Random -Maximum 9999).ToString("0000"))
$spPayload = @{ code = $spCode; name = ("Specimen " + $spCode); isActive = $true } | ConvertTo-Json
(Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/Specimens" -Headers (HeadersFor $token) -ContentType "application/json" -Body $spPayload) | Out-Null
$spOpt = ('{"RecordPerPage":50,"CurrentPage":1,"SortColumnName":"Code","SortDirection":false,"SearchText":"' + $spCode + '"}')
$spList = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/Specimens" -Headers (HeadersFor $token $spOpt)
$spItems = @()
if ($spList.Items) { $spItems = $spList.Items } elseif ($spList.items) { $spItems = $spList.items }
Assert-True ($spItems | Where-Object { $_.Code -eq $spCode -or $_.code -eq $spCode } | Measure-Object).Count -gt 0 ("Specimen not found after create: " + $spCode)
Write-Host "Specimen create+list OK: $spCode"

Write-Host "== HisTest (Test Master) =="
$tCode = ("TQA" + (Get-Random -Maximum 9999).ToString("0000"))
$tPayload = @{ hisTestCode = $tCode; hisTestCodeDescription = ("Test " + $tCode); hisSpecimenCode = $spCode; hisSpecimenName = ("Specimen " + $spCode); departmentCode = $deptCode; isActive = $true } | ConvertTo-Json
(Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/HisTest" -Headers (HeadersFor $token) -ContentType "application/json" -Body $tPayload) | Out-Null
$tOpt = ('{"RecordPerPage":200,"CurrentPage":1,"SortColumnName":"HISTestCode","SortDirection":false,"SearchText":"' + $tCode + '"}')
$tList = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/HisTest" -Headers (HeadersFor $token $tOpt)
$tItems = @()
if ($tList.Items) { $tItems = $tList.Items } elseif ($tList.items) { $tItems = $tList.items }
Assert-True ($tItems | Where-Object { $_.HISTestCode -eq $tCode -or $_.hisTestCode -eq $tCode } | Measure-Object).Count -gt 0 ("HisTest not found after create: " + $tCode)
Write-Host "HisTest create+list OK: $tCode"

Write-Host "== Test Mapping =="
$mapPayload = @{ equipmentId = 1; lisTestCode = ("LIS-" + $tCode); lisTestCodeDescription = ("LIS " + $tCode); hisTestCode = $tCode; hisTestCodeDescription=("Test "+$tCode); isActive = $true } | ConvertTo-Json
(Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/TestMappingMaster" -Headers (HeadersFor $token) -ContentType "application/json" -Body $mapPayload) | Out-Null
Write-Host "TestMapping create OK"

Write-Host "SmokeMastersCrud PASSED"

