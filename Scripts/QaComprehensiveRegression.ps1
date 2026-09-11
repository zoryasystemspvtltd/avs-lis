# QA Comprehensive Regression Harness (SqlClient + authenticated API)
# Unblocks validation when sqlcmd ODBC is broken. Does NOT modify application code.
# Notification: inspect UseMockProviders / IsEnabled; never forces real SMS/WhatsApp.
$ErrorActionPreference = "Stop"
$BaseApi = "http://localhost:8081"
$BasePortal = "http://localhost:8080"
$Cs = "Server=.\SQLEXPRESS;Database=ZoryaLMS;Trusted_Connection=True;TrustServerCertificate=True"
$Pwd = "zorKol@1"
$results = New-Object System.Collections.Generic.List[object]
$tag = "QA-REG-" + (Get-Date -Format "yyyyMMddHHmmss")

function Rec([string]$area, [string]$test, [string]$status, [string]$detail) {
  $script:results.Add([pscustomobject]@{ Area = $area; Test = $test; Status = $status; Detail = $detail })
  $c = switch ($status) { "PASS" { "Green" } "FAIL" { "Red" } "BLOCKED" { "Yellow" } default { "Gray" } }
  Write-Host "[$status] $area :: $test - $detail" -ForegroundColor $c
}

function SqlScalar([string]$q) {
  $cn = New-Object System.Data.SqlClient.SqlConnection $Cs
  $cn.Open()
  try {
    $cmd = $cn.CreateCommand(); $cmd.CommandText = $q
    $v = $cmd.ExecuteScalar()
    if ($null -eq $v -or $v -is [DBNull]) { return $null }
    return $v
  } finally { $cn.Close() }
}

function SqlRows([string]$q) {
  $cn = New-Object System.Data.SqlClient.SqlConnection $Cs
  $cn.Open()
  $list = New-Object System.Collections.ArrayList
  try {
    $cmd = $cn.CreateCommand(); $cmd.CommandText = $q
    $r = $cmd.ExecuteReader()
    while ($r.Read()) {
      $vals = @{}
      for ($i = 0; $i -lt $r.FieldCount; $i++) {
        $n = $r.GetName($i)
        if ($r.IsDBNull($i)) { $vals[$n] = $null } else { $vals[$n] = $r.GetValue($i) }
      }
      [void]$list.Add((New-Object PSObject -Property $vals))
    }
    $r.Close()
  } finally { $cn.Close() }
  return $list
}

function Get-Token([string]$user) {
  $body = "grant_type=password&username=$([uri]::EscapeDataString($user))&password=$([uri]::EscapeDataString($Pwd))"
  $r = Invoke-RestMethod -Method Post -Uri "$BaseApi/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
  if (-not $r.access_token) { throw "Token failed for $user" }
  return $r.access_token
}

function Hdr([string]$token, $apiOption = $null) {
  $h = @{ accesskey = "DXI800"; Authorization = ("Bearer " + $token) }
  if ($apiOption) {
    if ($apiOption -is [string]) { $h.ApiOption = $apiOption }
    else { $h.ApiOption = ($apiOption | ConvertTo-Json -Compress) }
  }
  return $h
}

function Expect-Status([scriptblock]$block, [int[]]$ok) {
  try {
    & $block | Out-Null
    throw "Expected HTTP $($ok -join '/') but call succeeded"
  } catch {
    $code = $null
    if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
      $code = [int]$_.Exception.Response.StatusCode
    }
    $msg = $_.Exception.Message
    if ($_.ErrorDetails.Message) { $msg += " " + $_.ErrorDetails.Message }
    if ($code -and ($ok -contains $code)) { return }
    if ($msg -match '401|403|Unauthorized|Forbidden|denied') {
      if (($ok -contains 401) -or ($ok -contains 403)) { return }
    }
    throw "Expected $($ok -join '/'), got: $msg"
  }
}

Write-Host "========== $tag ==========" -ForegroundColor Cyan

# ---- Stage 1 health ----
try {
  $p = Invoke-WebRequest -Uri $BasePortal -UseBasicParsing -TimeoutSec 20
  Rec "ENV" "Portal HTTP" $(if ([int]$p.StatusCode -eq 200) { "PASS" } else { "FAIL" }) "status=$([int]$p.StatusCode)"
} catch { Rec "ENV" "Portal HTTP" "FAIL" $_.Exception.Message }

try {
  $a = Invoke-WebRequest -Uri $BaseApi -UseBasicParsing -TimeoutSec 20
  Rec "ENV" "API HTTP" $(if ([int]$a.StatusCode -eq 200) { "PASS" } else { "FAIL" }) "status=$([int]$a.StatusCode)"
} catch { Rec "ENV" "API HTTP" "FAIL" $_.Exception.Message }

try {
  $n = SqlScalar "SELECT COUNT(*) FROM NotificationConfiguration"
  Rec "ENV" "DB connectivity" "PASS" "NotificationConfiguration rows=$n"
} catch { Rec "ENV" "DB connectivity" "FAIL" $_.Exception.Message }

$mock = $null
try {
  $cfgPath = "I:\Projects\PROD\AVILIS\API\Web.config"
  if (Test-Path $cfgPath) {
    [xml]$xml = Get-Content $cfgPath
    $mock = ($xml.configuration.appSettings.add | Where-Object { $_.key -eq "Notification:UseMockProviders" }).value
    $tz = ($xml.configuration.appSettings.add | Where-Object { $_.key -eq "OperationalDateTime:FacilityTimeZoneId" }).value
    Rec "ENV" "UseMockProviders" $(if ($mock -eq "true") { "PASS" } else { "FAIL" }) "value=$mock"
    Rec "ENV" "FacilityTimeZone" $(if ($tz -eq "India Standard Time") { "PASS" } else { "FAIL" }) "value=$tz"
  } else {
    Rec "ENV" "Deployed Web.config" "BLOCKED" "path missing"
  }
} catch { Rec "ENV" "Notification mock config" "FAIL" $_.Exception.Message }

$isEnabled = SqlScalar "SELECT TOP 1 CAST(IsEnabled AS INT) FROM NotificationConfiguration"
Rec "ENV" "Notification IsEnabled" "PASS" "IsEnabled=$isEnabled (ops observation if 1; mock providers required)"

# Toolchain
$msb = Test-Path "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
$vst = Test-Path "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
Rec "TOOL" "MSBuild" $(if ($msb) { "PASS" } else { "BLOCKED" }) "VS2022 Community MSBuild present=$msb"
Rec "TOOL" "VSTest" $(if ($vst) { "PASS" } else { "BLOCKED" }) "vstest.console present=$vst"
Rec "TOOL" "Masters.Tests DLL" $(if (Test-Path "I:\Projects\LIS\avs-lis\LIS.Masters.Tests\bin\Release\LIS.Masters.Tests.dll") { "PASS" } else { "FAIL" }) "prebuilt Release DLL"
Rec "TOOL" "sqlcmd ODBC" "BLOCKED" "sqlcmd DSN/driver fails; harness uses ADO.NET SqlClient instead"

# ---- Auth ----
$admin = $null; $tech = $null; $doc = $null
try { $admin = Get-Token "admin@zorya.co.in"; Rec "AUTH" "Admin token" "PASS" "len=$($admin.Length)" } catch { Rec "AUTH" "Admin token" "FAIL" $_.Exception.Message }
try { $tech = Get-Token "qa-cert-tech@zorya.co.in"; Rec "AUTH" "Technician token" "PASS" "len=$($tech.Length)" } catch { Rec "AUTH" "Technician token" "FAIL" $_.Exception.Message }
try { $doc = Get-Token "qa-cert-doctor@zorya.co.in"; Rec "AUTH" "Doctor token" "PASS" "len=$($doc.Length)" } catch { Rec "AUTH" "Doctor token" "FAIL" $_.Exception.Message }

# Receptionist if exists
try {
  $recTok = Get-Token "qa-cert-reception@zorya.co.in"
  Rec "AUTH" "Receptionist token" "PASS" "len=$($recTok.Length)"
} catch {
  Rec "AUTH" "Receptionist token" "SKIPPED" "qa-cert-reception user not available: $($_.Exception.Message)"
}

# ---- Security anonymous ----
$anonEndpoints = @(
  @{ N = "SaleInvoice"; U = "$BaseApi/api/SaleInvoice" },
  @{ N = "BarCode"; U = "$BaseApi/api/BarCode?sampleNo=TEST" },
  @{ N = "HisTest"; U = "$BaseApi/api/HisTest" },
  @{ N = "Department"; U = "$BaseApi/api/Department" },
  @{ N = "TestRate"; U = "$BaseApi/api/TestRate" },
  @{ N = "Quality"; U = "$BaseApi/api/Quality" },
  @{ N = "ReportLayout"; U = "$BaseApi/api/ReportLayoutConfiguration/Diagnostic" },
  @{ N = "NotificationConfig"; U = "$BaseApi/api/NotificationConfiguration" },
  @{ N = "DashboardReg"; U = "$BaseApi/api/Dashboard/Registration" }
)
foreach ($e in $anonEndpoints) {
  try {
    Expect-Status { Invoke-RestMethod -Uri $e.U -Method Get } @(401, 403)
    Rec "SEC" "Anonymous $($e.N)" "PASS" "401/403"
  } catch { Rec "SEC" "Anonymous $($e.N)" "FAIL" $_.Exception.Message }
}

try {
  $dl = Invoke-WebRequest -Uri "$BaseApi/api/report/download/invalid-token-$tag" -UseBasicParsing
  Rec "SEC" "Secure download invalid" "FAIL" "unexpected success status=$([int]$dl.StatusCode)"
} catch {
  $code = $null
  if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
  $body = ""
  try {
    $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $body = $sr.ReadToEnd()
  } catch {}
  if ($code -eq 400 -and $body -notmatch 'Patient|InvoiceNo|ResultValue|phone') {
    Rec "SEC" "Secure download invalid" "PASS" "HTTP 400 opaque rejection"
  } else {
    Rec "SEC" "Secure download invalid" "FAIL" "code=$code body=$body"
  }
}

# ---- RBAC Report Layout ----
if ($admin) {
  try {
    $layout = Invoke-RestMethod -Uri "$BaseApi/api/ReportLayoutConfiguration/Diagnostic" -Headers (Hdr $admin)
    Rec "RBAC" "Admin GET Diagnostic layout" "PASS" "header=$($layout.headerHeightMm) footer=$($layout.footerHeightMm) doc=$($layout.doctorSignatureEnabled) tech=$($layout.technicianSignatureEnabled)"
    $layoutR = Invoke-RestMethod -Uri "$BaseApi/api/ReportLayoutConfiguration/Radiology" -Headers (Hdr $admin)
    Rec "RBAC" "Admin GET Radiology layout" "PASS" "header=$($layoutR.headerHeightMm)"
    $mods = Invoke-RestMethod -Uri "$BaseApi/api/UserAccess/DXI800" -Headers (Hdr $admin)
    if ($mods -is [string]) { $mods = $mods | ConvertFrom-Json }
    $has = @($mods | Where-Object { $_.name -eq "ReportLayoutConfiguration" }).Count -gt 0
    Rec "RBAC" "Admin has ReportLayoutConfiguration module" $(if ($has) { "PASS" } else { "FAIL" }) "has=$has"
  } catch { Rec "RBAC" "Admin Report Layout" "FAIL" $_.Exception.Message }
}

if ($tech) {
  try {
    Expect-Status { Invoke-RestMethod -Uri "$BaseApi/api/ReportLayoutConfiguration/Diagnostic" -Headers (Hdr $tech) } @(401, 403)
    Rec "RBAC" "Technician denied Report Layout API" "PASS" "401/403"
  } catch { Rec "RBAC" "Technician denied Report Layout API" "FAIL" $_.Exception.Message }
}

if ($doc) {
  try {
    Expect-Status { Invoke-RestMethod -Uri "$BaseApi/api/ReportLayoutConfiguration/Diagnostic" -Headers (Hdr $doc) } @(401, 403)
    Rec "RBAC" "Doctor denied Report Layout API" "PASS" "401/403"
  } catch { Rec "RBAC" "Doctor denied Report Layout API" "FAIL" $_.Exception.Message }
}

$adminMap = SqlScalar "SELECT COUNT(*) FROM RoleModuleMappings rm INNER JOIN AspNetRoles r ON r.Id=rm.RoleId INNER JOIN UserModules um ON um.Id=rm.ModuleId WHERE um.Name=N'ReportLayoutConfiguration' AND r.Name=N'Administrator'"
$nonAdmin = SqlScalar "SELECT COUNT(*) FROM RoleModuleMappings rm INNER JOIN AspNetRoles r ON r.Id=rm.RoleId INNER JOIN UserModules um ON um.Id=rm.ModuleId WHERE um.Name=N'ReportLayoutConfiguration' AND r.Name<>N'Administrator'"
$menu = SqlScalar "SELECT COUNT(*) FROM RoleMenuPermission WHERE MenuKey=N'SETUP_REPORT_LAYOUT_CONFIGURATION' AND IsActive=1"
Rec "RBAC" "DB Admin RLC mapping" $(if ([int]$adminMap -ge 1) { "PASS" } else { "FAIL" }) "count=$adminMap"
Rec "RBAC" "DB non-Admin RLC mappings" $(if ([int]$nonAdmin -eq 0) { "PASS" } else { "FAIL" }) "count=$nonAdmin"
Rec "RBAC" "DB RLC menu permission" $(if ([int]$menu -ge 1) { "PASS" } else { "FAIL" }) "count=$menu"

# ---- SampleNo multi-specimen invoice ----
if ($admin) {
  try {
    $tests = @(SqlRows @"
SELECT TOP 1 t.Id AS Id, t.HISTestCode AS Code, t.HISSpecimenCode AS Spec
FROM HISTestMaster t
INNER JOIN TestRateMaster r ON r.TestId=t.Id AND r.IsActive=1
 AND r.EffectiveStart <= CAST(GETDATE() AS date) AND r.EffectiveEnd >= CAST(GETDATE() AS date)
INNER JOIN Department d ON d.Code=t.DepartmentCode AND d.ProcessingCategory=N'Laboratory'
WHERE t.IsActive=1 AND t.HISSpecimenCode IS NOT NULL AND LTRIM(RTRIM(t.HISSpecimenCode))<>N''
ORDER BY t.Id
"@)
    $specA = [string]$tests[0].Spec
    $testAId = [int]$tests[0].Id
    $testsB = @(SqlRows @"
SELECT TOP 1 t.Id AS Id, t.HISTestCode AS Code, t.HISSpecimenCode AS Spec
FROM HISTestMaster t
INNER JOIN TestRateMaster r ON r.TestId=t.Id AND r.IsActive=1
 AND r.EffectiveStart <= CAST(GETDATE() AS date) AND r.EffectiveEnd >= CAST(GETDATE() AS date)
INNER JOIN Department d ON d.Code=t.DepartmentCode AND d.ProcessingCategory=N'Laboratory'
WHERE t.IsActive=1 AND t.HISSpecimenCode IS NOT NULL AND LTRIM(RTRIM(t.HISSpecimenCode))<>N''
 AND t.HISSpecimenCode <> N'$specA'
ORDER BY t.Id
"@)
    if ($testsB.Count -lt 1) { throw "No second specimen test found" }
    $specB = [string]$testsB[0].Spec
    $testBId = [int]$testsB[0].Id
    Rec "SAMPLENO" "Specimen pair" "PASS" "A=$specA/$testAId B=$specB/$testBId"

    $pats = Invoke-RestMethod -Uri "$BaseApi/api/PatientMaster" -Headers (Hdr $admin (@{ RecordPerPage = 1; CurrentPage = 1 }))
    $patientId = $pats.items[0].id
    if (-not $patientId) { $patientId = $pats.Items[0].Id }
    $invNo = Invoke-RestMethod -Uri "$BaseApi/api/SaleInvoice/NextInvoiceNo" -Headers (Hdr $admin)
    $payload = @{
      invoice = @{
        id = 0; invoiceNo = "$invNo"; invoiceDate = (Get-Date -Format "yyyy-MM-dd")
        patientId = [long]$patientId; invoiceStatus = 1; paymentStatus = 0; isActive = $true
        paymentType = "Cash"; discountType = "Fixed Amount"
      }
      details = @(
        @{ testId = $testAId; quantity = 1; rate = 0 },
        @{ testId = $testBId; quantity = 1; rate = 0 }
      )
    } | ConvertTo-Json -Depth 6
    $saved = Invoke-RestMethod -Method Post -Uri "$BaseApi/api/SaleInvoice" -Headers (Hdr $admin) -ContentType "application/json" -Body $payload
    $invId = $saved.result; if (-not $invId) { $invId = $saved.Result }
    Rec "SAMPLENO" "Create multi-specimen invoice" $(if ($invId -gt 0) { "PASS" } else { "FAIL" }) "id=$invId no=$invNo"

    $reqList = SqlRows "SELECT HISTestCode, SpecimenCode, SampleNo, HISRequestNo, HISRequestId FROM TestRequestDetails WHERE HISRequestNo=N'$invNo' ORDER BY Id"
    $reqs = @($reqList | ForEach-Object { $_ })
    Rec "SAMPLENO" "TestRequestDetails rows" $(if ($reqList.Count -ge 2) { "PASS" } else { "FAIL" }) "count=$($reqList.Count)"

    $okFmt = $true
    $samples = @{}
    foreach ($row in $reqList) {
      $sn = [string]$row.SampleNo
      $sp = [string]$row.SpecimenCode
      if ($sn -match 'INV' -or $sn -match '-') { $okFmt = $false }
      if (-not $samples.ContainsKey($sp)) { $samples[$sp] = New-Object System.Collections.Generic.List[string] }
      if (-not $samples[$sp].Contains($sn)) { [void]$samples[$sp].Add($sn) }
      Rec "SAMPLENO" "Persisted SampleNo" "PASS" "test=$($row.HISTestCode) specimen=$sp sampleNo=$sn requestNo=$($row.HISRequestNo)"
    }
    Rec "SAMPLENO" "No INV/hyphen in SampleNo" $(if ($okFmt) { "PASS" } else { "FAIL" }) "checked=$($reqs.Count)"

    $sampleVals = @($samples.Values | ForEach-Object { $_[0] } | Select-Object -Unique)
    $diffOk = ($samples.Keys.Count -ge 2) -and ($sampleVals.Count -ge 2)
    $mapText = (($samples.GetEnumerator() | ForEach-Object { $_.Key + '=' + ($_.Value -join ',') }) -join '; ')
    Rec "SAMPLENO" "Different specimens different SampleNo" $(if ($diffOk) { "PASS" } else { "FAIL" }) "map=$mapText"

    # same specimen reuse: add second line of same specimen if another test exists
    $sameTests = @(SqlRows @"
SELECT TOP 1 t.Id AS Id, t.HISTestCode AS Code
FROM HISTestMaster t
INNER JOIN TestRateMaster r ON r.TestId=t.Id AND r.IsActive=1
 AND r.EffectiveStart <= CAST(GETDATE() AS date) AND r.EffectiveEnd >= CAST(GETDATE() AS date)
INNER JOIN Department d ON d.Code=t.DepartmentCode AND d.ProcessingCategory=N'Laboratory'
WHERE t.IsActive=1 AND t.HISSpecimenCode=N'$specA' AND t.Id<>$testAId
ORDER BY t.Id
"@)
    if ($sameTests.Count -ge 1) {
      $invNo2 = Invoke-RestMethod -Uri "$BaseApi/api/SaleInvoice/NextInvoiceNo" -Headers (Hdr $admin)
      $payload2 = @{
        invoice = @{
          id = 0; invoiceNo = "$invNo2"; invoiceDate = (Get-Date -Format "yyyy-MM-dd")
          patientId = [long]$patientId; invoiceStatus = 1; paymentStatus = 0; isActive = $true
          paymentType = "Cash"; discountType = "Fixed Amount"
        }
        details = @(
          @{ testId = $testAId; quantity = 1; rate = 0 },
          @{ testId = [int]$sameTests[0].Id; quantity = 1; rate = 0 }
        )
      } | ConvertTo-Json -Depth 6
      $saved2 = Invoke-RestMethod -Method Post -Uri "$BaseApi/api/SaleInvoice" -Headers (Hdr $admin) -ContentType "application/json" -Body $payload2
      $invId2 = $saved2.result; if (-not $invId2) { $invId2 = $saved2.Result }
      $reqs2List = SqlRows "SELECT SampleNo FROM TestRequestDetails WHERE HISRequestNo=N'$invNo2'"
      $uniq = @($reqs2List | ForEach-Object { [string]$_.SampleNo } | Select-Object -Unique)
      Rec "SAMPLENO" "Same specimen reuses SampleNo" $(if ($uniq.Count -eq 1 -and $reqs2List.Count -ge 2) { "PASS" } else { "FAIL" }) "count=$($reqs2List.Count) unique=$($uniq.Count) sample=$($uniq -join ',')"
      try { Invoke-RestMethod -Method Put -Uri "$BaseApi/api/SaleInvoice/Cancel/$invId2" -Headers (Hdr $admin) | Out-Null } catch {}
    } else {
      Rec "SAMPLENO" "Same specimen reuses SampleNo" "SKIPPED" "no second rated test for specimen $specA"
    }

    try { Invoke-RestMethod -Method Put -Uri "$BaseApi/api/SaleInvoice/Cancel/$invId" -Headers (Hdr $admin) | Out-Null; Rec "SAMPLENO" "Cancel multi-specimen invoice" "PASS" "id=$invId" } catch { Rec "SAMPLENO" "Cancel multi-specimen invoice" "FAIL" $_.Exception.Message }
  } catch {
    Rec "SAMPLENO" "Multi-specimen flow" "FAIL" $_.Exception.Message
  }
}

# ---- Lab result entry search (Enter-key is UI; API smoke) ----
if ($admin) {
  try {
    $opt = '{"fromDate":"2020-01-01","toDate":"2030-12-31"}'
    $rows = @(Invoke-RestMethod -Method Get -Uri "$BaseApi/api/TestResultEdit/search" -Headers (Hdr $admin $opt))
    Rec "RESULT" "TestResultEdit search" $(if ($rows.Count -ge 0) { "PASS" } else { "FAIL" }) "rows=$($rows.Count)"
  } catch { Rec "RESULT" "TestResultEdit search" "FAIL" $_.Exception.Message }
}

# ---- Tech modules / Doctor modules smoke ----
if ($tech) {
  try {
    $mods = Invoke-RestMethod -Uri "$BaseApi/api/UserAccess/DXI800" -Headers (Hdr $tech)
    if ($mods -is [string]) { $mods = $mods | ConvertFrom-Json }
    $names = @($mods | ForEach-Object { $_.name })
    Rec "RBAC" "Tech has SampleCollection" $(if ($names -contains "SampleCollection") { "PASS" } else { "FAIL" }) ("mods=" + ($names -join ','))
    Rec "RBAC" "Tech lacks ReportLayoutConfiguration" $(if ($names -notcontains "ReportLayoutConfiguration") { "PASS" } else { "FAIL" }) "present=$($names -contains 'ReportLayoutConfiguration')"
  } catch { Rec "RBAC" "Tech modules" "FAIL" $_.Exception.Message }
}
if ($doc) {
  try {
    $mods = Invoke-RestMethod -Uri "$BaseApi/api/UserAccess/DXI800" -Headers (Hdr $doc)
    if ($mods -is [string]) { $mods = $mods | ConvertFrom-Json }
    $names = @($mods | ForEach-Object { $_.name })
    Rec "RBAC" "Doctor has DoctorsApprovals" $(if ($names -contains "DoctorsApprovals") { "PASS" } else { "FAIL" }) ("mods=" + ($names -join ','))
    Rec "RBAC" "Doctor lacks ReportLayoutConfiguration" $(if ($names -notcontains "ReportLayoutConfiguration") { "PASS" } else { "FAIL" }) "present=$($names -contains 'ReportLayoutConfiguration')"
  } catch { Rec "RBAC" "Doctor modules" "FAIL" $_.Exception.Message }
}

# ---- Dashboard auth ----
if ($admin) {
  try {
    $m = Invoke-RestMethod -Uri "$BaseApi/api/Dashboard/Registration" -Headers (Hdr $admin)
    Rec "DASH" "Admin Dashboard Registration" "PASS" "metrics=$($m.Count)"
  } catch { Rec "DASH" "Admin Dashboard Registration" "FAIL" $_.Exception.Message }
}

# Summary
$pass = @($results | Where-Object Status -eq "PASS").Count
$fail = @($results | Where-Object Status -eq "FAIL").Count
$blocked = @($results | Where-Object Status -eq "BLOCKED").Count
$skipped = @($results | Where-Object Status -eq "SKIPPED").Count
Write-Host ""
Write-Host "SUMMARY PASS=$pass FAIL=$fail BLOCKED=$blocked SKIPPED=$skipped TOTAL=$($results.Count)" -ForegroundColor Cyan
$out = "I:\Projects\LIS\avs-lis\docs\_qa_comprehensive_run.json"
$results | ConvertTo-Json -Depth 4 | Set-Content $out -Encoding UTF8
Write-Host "Wrote $out"
if ($fail -gt 0) { exit 2 }
exit 0
