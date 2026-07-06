# Lab Result Entry (TestResultEdit) — API certification: Search (OR), Read, Update
$ErrorActionPreference = "Stop"
$baseApi = "http://localhost:8081"
$passed = 0
$failed = 0

function Cert([string]$name, [scriptblock]$block) {
    try {
        & $block
        Write-Host "[PASS] $name" -ForegroundColor Green
        $script:passed++
    } catch {
        Write-Host "[FAIL] $name - $($_.Exception.Message)" -ForegroundColor Red
        $script:failed++
    }
}

function Get-Token {
    $body = 'grant_type=password&username=admin%40zorya.co.in&password=zorKol%401'
    $r = Invoke-RestMethod -Method Post -Uri "$baseApi/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
    if (-not $r.access_token) { throw "Token fetch failed." }
    return $r.access_token
}

function HeadersFor([string]$token, [string]$apiOptionJson = $null) {
    $h = @{ accesskey = "DXI800"; Authorization = ("Bearer " + $token) }
    if ($apiOptionJson) { $h.ApiOption = $apiOptionJson }
    return $h
}

function SqlScalar([string]$q) {
    $query = 'SET NOCOUNT ON; ' + $q
    $out = & sqlcmd -S '.\SQLEXPRESS' -d ZoryaLMS -E -h -1 -W -Q $query 2>&1
    if ($LASTEXITCODE -ne 0) { throw "SQL failed: $out" }
    $line = $out | Where-Object { $_ -and $_.ToString().Trim() -ne '' } | Select-Object -First 1
    if ($null -eq $line) { return '' }
    return [string]$line
}

Write-Host "=== Lab Result Entry API Certification ===" -ForegroundColor Cyan
$token = Get-Token
Write-Host "Token OK"

$sampleNo = SqlScalar "SELECT TOP 1 tr.SampleNo FROM dbo.TestResults tr INNER JOIN dbo.TestRequestDetails req ON req.Id = tr.TestRequestId WHERE tr.SampleNo IS NOT NULL AND req.ReportStatus IN (2,3) ORDER BY tr.Id DESC"
if ([string]::IsNullOrWhiteSpace($sampleNo)) {
    throw "No editable sample found in database for certification."
}
Write-Host "Test sample: $sampleNo"

Cert "Search by date range returns rows" {
    $opt = '{"fromDate":"2020-01-01","toDate":"2030-12-31"}'
    $rows = @(Invoke-RestMethod -Method Get -Uri "$baseApi/api/TestResultEdit/search" -Headers (HeadersFor $token $opt))
    if ($rows.Count -lt 1) { throw "Expected at least one row, got $($rows.Count)" }
}

Cert "Advanced Search OR - Sample No + non-matching Patient Name still returns sample" {
    $bogus = "__no_patient_" + [guid]::NewGuid().ToString("N")
    $opt = (@{ sampleNo = $sampleNo; patientName = $bogus } | ConvertTo-Json -Compress)
    $rows = @(Invoke-RestMethod -Method Get -Uri "$baseApi/api/TestResultEdit/search" -Headers (HeadersFor $token $opt))
    $hit = $rows | Where-Object {
        ($_.sampleNo -and $_.sampleNo -eq $sampleNo) -or ($_.SampleNo -and $_.SampleNo -eq $sampleNo)
    } | Select-Object -First 1
    if (-not $hit) { throw "OR search did not return sample $sampleNo when patient name was non-matching." }
}

Cert "GET by sample loads parameters" {
    $dto = Invoke-RestMethod -Method Get -Uri "$baseApi/api/TestResultEdit/$([uri]::EscapeDataString($sampleNo))" -Headers (HeadersFor $token)
    $tests = @($dto.tests)
    if ($dto.Tests) { $tests = @($dto.Tests) }
    if ($tests.Count -lt 1) { throw "No tests in sample DTO." }
    $test = $tests | Where-Object { $_.canEdit -or $_.CanEdit } | Select-Object -First 1
    if (-not $test) { $test = $tests[0] }
    $params = @($test.parameters)
    if ($test.Parameters) { $params = @($test.Parameters) }
    if ($params.Count -lt 1) { throw "No parameters loaded." }
    $script:certTest = $test
    $script:certParam = $params[0]
}

Cert "PUT save updates parameter value" {
    if (-not $script:certTest -or -not $script:certParam) { throw "Prior GET step did not set cert context." }
    $canEdit = $script:certTest.canEdit
    if ($null -eq $canEdit) { $canEdit = $script:certTest.CanEdit }
    if (-not $canEdit) { throw "Sample is read-only; cannot certify save." }

    $detailId = $script:certParam.detailId
    if (-not $detailId) { $detailId = $script:certParam.DetailId }
    $oldVal = $script:certParam.resultValue
    if (-not $oldVal) { $oldVal = $script:certParam.ResultValue }
    $newVal = if ($oldVal -match '^\d+$') { ([int]$oldVal + 1).ToString() } else { "9.99" }

    $testResultId = $script:certTest.testResultId
    if (-not $testResultId) { $testResultId = $script:certTest.TestResultId }
    $testRequestId = $script:certTest.testRequestId
    if (-not $testRequestId) { $testRequestId = $script:certTest.TestRequestId }

    $payload = @{
        testResultId = $testResultId
        testRequestId = $testRequestId
        parameters = @(@{ detailId = $detailId; resultValue = $newVal; remark = '' })
    } | ConvertTo-Json -Depth 5

    $save = Invoke-RestMethod -Method Put -Uri "$baseApi/api/TestResultEdit" -Headers (HeadersFor $token) -ContentType "application/json" -Body $payload
    if (-not ($save.success -or $save.Success)) { throw "Save returned unsuccessful." }

    $reload = Invoke-RestMethod -Method Get -Uri "$baseApi/api/TestResultEdit/$([uri]::EscapeDataString($sampleNo))" -Headers (HeadersFor $token)
    $tests = @($reload.tests)
    if ($reload.Tests) { $tests = @($reload.Tests) }
    $test = $tests | Where-Object { ($_.testResultId -eq $testResultId) -or ($_.TestResultId -eq $testResultId) } | Select-Object -First 1
    $params = @($test.parameters)
    if ($test.Parameters) { $params = @($test.Parameters) }
    $updated = $params | Where-Object { ($_.detailId -eq $detailId) -or ($_.DetailId -eq $detailId) } | Select-Object -First 1
    $actual = $updated.resultValue
    if (-not $actual) { $actual = $updated.ResultValue }
    if ($actual -ne $newVal) { throw "Expected '$newVal' after save, got '$actual'." }

    # Restore original value
    $restore = @{
        testResultId = $testResultId
        testRequestId = $testRequestId
        parameters = @(@{ detailId = $detailId; resultValue = $oldVal; remark = '' })
    } | ConvertTo-Json -Depth 5
    Invoke-RestMethod -Method Put -Uri "$baseApi/api/TestResultEdit" -Headers (HeadersFor $token) -ContentType "application/json" -Body $restore | Out-Null
}

Cert "Portal bundle contains Lab Result Entry route" {
    $bundle = Get-ChildItem "I:\Projects\LIS\avs-lis\web\Lis.Web\dist\DxI800\main-es2015.*.js" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $bundle) { throw "Angular dist bundle not found - run ng build first." }
    $text = Get-Content $bundle.FullName -Raw
    if ($text -notlike '*lab-result-entry*') { throw "Built bundle missing lab-result-entry route." }
    if ($text -notlike '*glyphicon-list-alt*' -and $text -notlike '*list-alt*') {
        Write-Host "  (warn) glyphicon-list-alt not found in bundle - icon may be in template only)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Summary: $passed passed, $failed failed ===" -ForegroundColor Cyan
if ($failed -gt 0) { exit 1 }
Write-Host "LabResultEntryCert PASSED" -ForegroundColor Green
