# Approval workflow runtime validation script
param(
    [string]$ApiBase = "http://localhost:8081",
    [string]$User = "admin@zorya.co.in",
    [string]$Password = "zorKol@1",
    [string]$AccessKey = "DXI800",
    [string]$TestSample = "CRUD-SMP-RPT"
)

$ErrorActionPreference = "Stop"
$results = @()

function Add-Result($Step, $Pass, $Detail) {
    $script:results += [PSCustomObject]@{ Step = $Step; Pass = $Pass; Detail = $Detail }
    $icon = if ($Pass) { "PASS" } else { "FAIL" }
    Write-Host "[$icon] $Step - $Detail"
}

function Get-Token {
    $body = @{ grant_type = "password"; username = $User; password = $Password }
    $r = Invoke-RestMethod -Uri "$ApiBase/Token" -Method Post -Headers @{ accesskey = $AccessKey } -Body $body -ContentType "application/x-www-form-urlencoded"
    return $r.access_token
}

function Get-Headers($token) {
    return @{ Authorization = "Bearer $token"; accesskey = $AccessKey }
}

function Get-RequestId($token, $sampleNo, $status) {
    $opt = @{ RecordPerPage = 50; CurrentPage = 1; SortColumnName = "SampleNo"; SortDirection = $true; Status = $status; SearchText = $sampleNo } | ConvertTo-Json -Compress
    $h = Get-Headers $token
    $h["ApiOption"] = $opt
    $list = Invoke-RestMethod -Uri "$ApiBase/api/Patients/" -Headers $h
    $row = $list.items | Where-Object { $_.sampleNo -eq $sampleNo } | Select-Object -First 1
    return $row
}

$token = Get-Token
Add-Result "Auth" $true "Token acquired"

# Reset sample to ReportGenerated (2) for clean test
$req = Get-RequestId $token $TestSample 2
if (-not $req) { $req = Get-RequestId $token $TestSample 3 }
if (-not $req) { $req = Get-RequestId $token $TestSample 5 }
if (-not $req) {
    Add-Result "Find sample" $false "Sample $TestSample not found"
    exit 1
}
$requestId = $req.id
Add-Result "Find sample" $true "RequestId=$requestId Status=$($req.reportStatus)"

# GET review payload
$review = Invoke-RestMethod -Uri "$ApiBase/api/sample/$requestId" -Headers (Get-Headers $token)
$hasRuns = ($review.testRuns.Count -gt 0)
$hasParams = ($review.testRuns[0].parameters.Count -gt 0)
Add-Result "Technician review load" ($null -ne $review.test) "Runs=$hasRuns Params=$hasParams"

# Technician approve (if status 2)
if ([int]$req.reportStatus -eq 2) {
    $body = @{ id = $requestId; status = 3; note = "QA tech approve"; runIndex = $review.testRuns[0].runIndex } | ConvertTo-Json
    try {
        Invoke-RestMethod -Uri "$ApiBase/api/sample/" -Method Post -Headers (Get-Headers $token) -Body $body -ContentType "application/json"
        $after = Get-RequestId $token $TestSample 3
        Add-Result "Technician approve" ($after.reportStatus -eq 3) "Status=$($after.reportStatus)"
    } catch {
        Add-Result "Technician approve" $false $_.Exception.Message
    }
}

# Duplicate technician approve (should fail)
$bodyDup = @{ id = $requestId; status = 3; note = "dup"; runIndex = $review.testRuns[0].runIndex } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$ApiBase/api/sample/" -Method Post -Headers (Get-Headers $token) -Body $bodyDup -ContentType "application/json"
    Add-Result "Duplicate tech approve blocked" $false "Should have been rejected"
} catch {
    Add-Result "Duplicate tech approve blocked" $true "Rejected as expected"
}

# Doctor approve from wrong status (reset to 2 via SQL not done - try from 3)
$req3 = Get-RequestId $token $TestSample 3
if ($req3) {
    $bodyDoc = @{ id = $requestId; status = 5; note = "QA doctor approve"; runIndex = $review.testRuns[0].runIndex } | ConvertTo-Json
    try {
        Invoke-RestMethod -Uri "$ApiBase/api/sample/" -Method Put -Headers (Get-Headers $token) -Body $bodyDoc -ContentType "application/json"
        $after5 = Get-RequestId $token $TestSample 5
        Add-Result "Doctor approve" ($after5.reportStatus -eq 5) "Status=$($after5.reportStatus)"
    } catch {
        Add-Result "Doctor approve" $false $_.Exception.Message
    }
}

# Doctor approve without tech (use SMP-NEW if exists)
$newReq = Get-RequestId $token "CRUD-SMP-NEW" 0
if ($newReq) {
    $bodyBad = @{ id = $newReq.id; status = 5; note = "skip tech"; runIndex = 0 } | ConvertTo-Json
    try {
        Invoke-RestMethod -Uri "$ApiBase/api/sample/" -Method Put -Headers (Get-Headers $token) -Body $bodyBad -ContentType "application/json"
        Add-Result "Doctor skip-tech blocked" $false "Should fail"
    } catch {
        Add-Result "Doctor skip-tech blocked" $true "Rejected"
    }
}

# Report: unpaid should fail, paid should work
$invoiceNo = "CRUD-INV-001"
try {
    Invoke-RestMethod -Uri "$ApiBase/api/Reports/TestReport?labNo=$invoiceNo" -Headers (Get-Headers $token) | Out-Null
    Add-Result "Report print (paid)" $true "Report returned"
} catch {
    Add-Result "Report print (paid)" $false $_.ErrorDetails.Message
}

Write-Host "`n=== SUMMARY ==="
$results | Format-Table -AutoSize
$fail = ($results | Where-Object { -not $_.Pass }).Count
Write-Host ("Total: {0}  Failed: {1}" -f $results.Count, $fail)
exit $(if ($fail -gt 0) { 1 } else { 0 })
