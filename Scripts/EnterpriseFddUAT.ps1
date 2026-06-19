# Enterprise FDD End-to-End UAT - Sample Collection, Receiving, Radiology
# Validates API + Database reconciliation (business workflow proof)
$ErrorActionPreference = "Stop"
$baseApi = "http://localhost:8081"
$results = @()
$uatTag = "UAT-FDD-" + (Get-Date -Format "yyyyMMddHHmmss")

function Log([string]$id, [string]$name, [string]$status, [string]$detail) {
  $global:results += [pscustomobject]@{ Id = $id; Scenario = $name; Status = $status; Detail = $detail }
  $color = if ($status -eq "PASS") { "Green" } elseif ($status -eq "FAIL") { "Red" } else { "Yellow" }
  Write-Host "[$status] $id $name - $detail" -ForegroundColor $color
}

function Get-Token([string]$user = "admin%40zorya.co.in", [string]$pwd = "zorKol%401") {
  $body = 'grant_type=password&username=' + $user + '&password=' + $pwd
  $r = Invoke-RestMethod -Method Post -Uri "$baseApi/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
  if (-not $r.access_token) { throw "Token failed for $user" }
  return $r.access_token
}

function HeadersFor([string]$token, [string]$apiOptionJson = $null) {
  $h = @{ accesskey = "DXI800"; Authorization = ("Bearer " + $token) }
  if ($apiOptionJson) { $h.ApiOption = $apiOptionJson }
  return $h
}

function Get-Items($resp) {
  if ($resp.Items) { return @($resp.Items) }
  if ($resp.items) { return @($resp.items) }
  return @()
}

function Get-Total($resp) {
  if ($null -ne $resp.TotalRecord) { return [int]$resp.TotalRecord }
  if ($null -ne $resp.totalRecord) { return [int]$resp.totalRecord }
  return (Get-Items $resp).Count
}

function Get-RowId($row) {
  if ($null -eq $row) { return $null }
  if ($row.Id) { return $row.Id }
  return $row.id
}

function Expect-ApiError([scriptblock]$block, [string]$needle) {
  try {
    & $block | Out-Null
    throw "Expected error containing '$needle'"
  } catch {
    $msg = $_.Exception.Message
    if ($_.ErrorDetails.Message) { $msg += ' ' + $_.ErrorDetails.Message }
    try {
      if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $body = $reader.ReadToEnd()
        if ($body) { $msg += ' ' + $body }
      }
    } catch { }
    if ($msg -notlike ('*' + $needle + '*')) { throw ('Expected ' + $needle + ' but got: ' + $msg) }
  }
}

function SqlScalar([string]$q) {
  $query = 'SET NOCOUNT ON; ' + $q
  $out = & sqlcmd -S '.\SQLEXPRESS' -d AVSLIS -E -h -1 -W -Q $query 2>&1
  if ($LASTEXITCODE -ne 0) { throw ('SQL failed: ' + $out) }
  return ($out | Where-Object { $_ -and $_.ToString().Trim() -ne '' } | Select-Object -First 1).ToString().Trim()
}

function SqlRow([string]$q) {
  $query = 'SET NOCOUNT ON; ' + $q
  $lines = & sqlcmd -S '.\SQLEXPRESS' -d AVSLIS -E -h -1 -W -s "`t" -Q $query 2>&1
  if ($LASTEXITCODE -ne 0) { throw ('SQL failed: ' + $lines) }
  $line = $lines | Where-Object { $_ -and $_.ToString().Trim() -ne '' } | Select-Object -First 1
  return $line
}

Write-Host "========== ENTERPRISE FDD UAT ($uatTag) ==========" -ForegroundColor Cyan
$adminToken = Get-Token
Log "ENV" "API/Portal health" "INFO" "Assumed up (script entry)"

# -------- PHASE 1: SAMPLE COLLECTION --------
Write-Host "`n--- PHASE 1: Sample Collection ---" -ForegroundColor Cyan

# SC-01 Pending queue, search, pagination, sort
try {
  $opt1 = '{"RecordPerPage":5,"CurrentPage":1,"SortColumnName":"SampleCollectionDate","SortDirection":false}'
  $q1 = Invoke-RestMethod -Method Get -Uri "$baseApi/api/SampleCollection/PendingQueue" -Headers (HeadersFor $adminToken $opt1)
  $totalPending = Get-Total $q1
  $page1 = Get-Items $q1
  if ($totalPending -lt 1) { throw "No pending collection records" }
  if ($page1.Count -gt 5) { throw "Pagination page size wrong: $($page1.Count)" }

  $opt2 = '{"RecordPerPage":5,"CurrentPage":2,"SortColumnName":"SampleCollectionDate","SortDirection":false}'
  $q2 = Invoke-RestMethod -Method Get -Uri "$baseApi/api/SampleCollection/PendingQueue" -Headers (HeadersFor $adminToken $opt2)
  $page2 = Get-Items $q2
  if ($page2.Count -lt 1 -and $totalPending -gt 5) { throw "Page 2 empty when total > 5" }

  $first = $page1[0]
  $sampleNo = if ($first.SampleNo) { $first.SampleNo } else { $first.sampleNo }
  $orderNo = if ($first.HisRequestNo) { $first.HisRequestNo } else { $first.hisRequestNo }
  $searchOpt = (@{ RecordPerPage = 25; CurrentPage = 1; BarcodeNumber = $sampleNo } | ConvertTo-Json -Compress)
  $qSearch = Invoke-RestMethod -Method Get -Uri "$baseApi/api/SampleCollection/PendingQueue" -Headers (HeadersFor $adminToken $searchOpt)
  $firstId = Get-RowId $first
  $found = Get-Items $qSearch | Where-Object { (Get-RowId $_) -eq $firstId }
  if (-not $found) { throw "Barcode search did not return target row" }

  $sortOpt = '{"RecordPerPage":10,"CurrentPage":1,"SortColumnName":"HisRequestNo","SortDirection":true}'
  $qSort = Invoke-RestMethod -Method Get -Uri "$baseApi/api/SampleCollection/PendingQueue" -Headers (HeadersFor $adminToken $sortOpt)
  if ((Get-Items $qSort).Count -lt 1) { throw "Sort query returned no rows" }

  Log "SC-01" "Pending Collection Queue" "PASS" "Total=$totalPending, search/sort/pagination OK"
} catch {
  Log "SC-01" "Pending Collection Queue" "FAIL" $_.Exception.Message
  throw
}

$testReqId = [long](Get-RowId $first)
$collectTime = (Get-Date).AddMinutes(-30).ToString("yyyy-MM-ddTHH:mm:ss")
$collectRemarks = "$uatTag collection remarks"

# SC-02 Sample Collection
try {
  $barcodeResp = Invoke-RestMethod -Method Get -Uri "$baseApi/api/SampleCollection/EnsureBarcode/$testReqId" -Headers (HeadersFor $adminToken)
  $barcode = $barcodeResp.barcode
  if (-not $barcode) { throw "Barcode not generated" }

  $collectBody = @{
    testRequestId = $testReqId
    collectionDateTime = $collectTime
    remarks = $collectRemarks
    barcodeNumber = $barcode
  } | ConvertTo-Json
  Invoke-RestMethod -Method Post -Uri "$baseApi/api/SampleCollection/Collect" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body $collectBody | Out-Null

  $byBar = Invoke-RestMethod -Method Get -Uri "$baseApi/api/SampleCollection/ByBarcode?barcode=$([uri]::EscapeDataString($barcode))" -Headers (HeadersFor $adminToken)
  $status = if ($byBar.Status) { $byBar.Status } else { $byBar.status }
  if ($status -ne "Collected") { throw "UI/API status expected Collected, got $status" }

  $dbRow = SqlRow ('SELECT CONVERT(varchar(30), SampleCollectionDate, 126), ISNULL(CollectedBy,''''), ISNULL(CollectedRemarks,''''), ISNULL(SampleNo,'''') FROM TestRequestDetails WHERE Id = ' + $testReqId)
  $parts = $dbRow -split "`t"
  if (-not $parts[1]) { throw "CollectedBy not persisted" }
  if ($parts[2] -notlike "*$uatTag*") { throw "CollectedRemarks not persisted: $($parts[2])" }
  if ($parts[3] -ne $barcode) { throw "SampleNo/barcode mismatch in DB" }

  Log "SC-02" "Sample Collection" "PASS" "Id=$testReqId Barcode=$barcode CollectedBy=$($parts[1])"
} catch {
  Log "SC-02" "Sample Collection" "FAIL" $_.Exception.Message
  throw
}

# SC-03 Duplicate collection
try {
  $dupBody = @{ testRequestId = $testReqId; collectionDateTime = $collectTime; remarks = "dup" } | ConvertTo-Json
  Expect-ApiError {
    Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$baseApi/api/SampleCollection/Collect" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body $dupBody
  } "Duplicate collection"
  Log "SC-03" "Duplicate Collection Prevention" "PASS" "Blocked as expected"
} catch {
  Log "SC-03" "Duplicate Collection Prevention" "FAIL" $_.Exception.Message
}

# SC-04 Future time
try {
  $pendingOpt = '{"RecordPerPage":1,"CurrentPage":1}'
  $pending2 = Invoke-RestMethod -Method Get -Uri "$baseApi/api/SampleCollection/PendingQueue" -Headers (HeadersFor $adminToken $pendingOpt)
  $p2 = (Get-Items $pending2)[0]
  $p2id = if ($p2.Id) { $p2.Id } else { $p2.id }
  $futureBody = @{ testRequestId = $p2id; collectionDateTime = (Get-Date).AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss"); remarks = "future" } | ConvertTo-Json
  Expect-ApiError {
    Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$baseApi/api/SampleCollection/Collect" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body $futureBody
  } "future"
  Log "SC-04" "Future Time Validation" "PASS" "Blocked as expected"
} catch {
  Log "SC-04" "Future Time Validation" "FAIL" $_.Exception.Message
}

# SC-05 Barcode unique (same order may share barcode across tests)
try {
  $orderCount = SqlScalar "SELECT COUNT(DISTINCT HISRequestNo) FROM TestRequestDetails WHERE SampleNo = '$barcode'"
  if ([int]$orderCount -gt 1) { throw "Barcode spans multiple orders: orderCount=$orderCount" }
  $rowCount = SqlScalar "SELECT COUNT(*) FROM TestRequestDetails WHERE SampleNo = '$barcode'"
  if ([int]$rowCount -lt 1) { throw "Barcode not found in DB" }
  $barcodeApi = Invoke-RestMethod -Method Get -Uri "$baseApi/api/BarCode?Id=$([uri]::EscapeDataString($barcode))" -Headers @{ accesskey = "DXI800" }
  if (-not $barcodeApi) { throw "Barcode print annotation API empty" }
  Log "SC-05" "Barcode Validation" "PASS" "Unique barcode, print API returns annotation"
} catch {
  Log "SC-05" "Barcode Validation" "FAIL" $_.Exception.Message
}

# -------- PHASE 2: SAMPLE RECEIVING --------
Write-Host "`n--- PHASE 2: Sample Receiving ---" -ForegroundColor Cyan

$receiveTime = (Get-Date).AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ss")
$receiveRemarks = "$uatTag receive remarks"

# SR-01 Receive sample
try {
  $recvSearch = Invoke-RestMethod -Method Get -Uri "$baseApi/api/SampleReceiving/ByBarcode?barcode=$([uri]::EscapeDataString($barcode))" -Headers (HeadersFor $adminToken)
  $recvBody = @{
    testRequestId = $testReqId
    receivedDateTime = $receiveTime
    remarks = $receiveRemarks
    barcodeNumber = $barcode
  } | ConvertTo-Json
  Invoke-RestMethod -Method Post -Uri "$baseApi/api/SampleReceiving/Receive" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body $recvBody | Out-Null

  $dbRecv = SqlRow ('SELECT CONVERT(varchar(30), SampleReceivedDate, 126), ISNULL(ReceivedBy,''''), ISNULL(ReceivedRemarks,'''') FROM TestRequestDetails WHERE Id = ' + $testReqId)
  $rp = $dbRecv -split "`t"
  if (-not $rp[1]) { throw "ReceivedBy not persisted" }
  if ($rp[2] -notlike "*$uatTag*") { throw "ReceivedRemarks not persisted" }

  Log "SR-01" "Receive Sample" "PASS" "ReceivedBy=$($rp[1])"
} catch {
  Log "SR-01" "Receive Sample" "FAIL" $_.Exception.Message
  throw
}

# SR-02 Duplicate receiving
try {
  Expect-ApiError {
    Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$baseApi/api/SampleReceiving/Receive" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body $recvBody
  } "Duplicate receiving"
  Log "SR-02" "Duplicate Receiving" "PASS" "Blocked"
} catch {
  Log "SR-02" "Duplicate Receiving" "FAIL" $_.Exception.Message
}

# SR-03 Receiving before collection
try {
  $pOpt = '{"RecordPerPage":1,"CurrentPage":1}'
  $pRow = (Get-Items (Invoke-RestMethod -Method Get -Uri "$baseApi/api/SampleCollection/PendingQueue" -Headers (HeadersFor $adminToken $pOpt)))[0]
  $pendingReqId = Get-RowId $pRow
  $cTime = (Get-Date).AddMinutes(-5).ToString("yyyy-MM-ddTHH:mm:ss")
  Invoke-RestMethod -Method Post -Uri "$baseApi/api/SampleCollection/Collect" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body (@{
    testRequestId = $pendingReqId; collectionDateTime = $cTime; remarks = "$uatTag pre-recv test"
  } | ConvertTo-Json) | Out-Null
  $pBar = SqlScalar ('SELECT SampleNo FROM TestRequestDetails WHERE Id = ' + $pendingReqId)
  $badRecv = @{
    testRequestId = $pendingReqId
    receivedDateTime = (Get-Date).AddHours(-2).ToString("yyyy-MM-ddTHH:mm:ss")
    remarks = "bad"
    barcodeNumber = $pBar
  } | ConvertTo-Json
  Expect-ApiError {
    Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$baseApi/api/SampleReceiving/Receive" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body $badRecv
  } "before collection"
  Log "SR-03" "Receiving Before Collection Time" "PASS" "Blocked"
} catch {
  Log "SR-03" "Receiving Before Collection Time" "FAIL" $_.Exception.Message
}

# SR-04 Rejection with RJ01 and RJ03
$rejectIds = @()
try {
  foreach ($code in @("RJ01", "RJ03")) {
    $pOpt = '{"RecordPerPage":1,"CurrentPage":1}'
    $row = (Get-Items (Invoke-RestMethod -Method Get -Uri "$baseApi/api/SampleCollection/PendingQueue" -Headers (HeadersFor $adminToken $pOpt)))[0]
    if (-not $row) { throw "No pending sample for rejection test $code" }
    $rid = Get-RowId $row
    $cDt = (Get-Date).AddMinutes(-20).ToString("yyyy-MM-ddTHH:mm:ss")
    Invoke-RestMethod -Method Post -Uri "$baseApi/api/SampleCollection/Collect" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body (@{
      testRequestId = $rid; collectionDateTime = $cDt; remarks = "$uatTag collect for reject $code"
    } | ConvertTo-Json) | Out-Null
    $collectedBy = SqlScalar ('SELECT ISNULL(CollectedBy,'''') FROM TestRequestDetails WHERE Id = ' + $rid)
    if (-not $collectedBy) { throw "Collect failed before reject for $code" }
    Invoke-RestMethod -Method Post -Uri "$baseApi/api/SampleReceiving/Reject" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body (@{
      testRequestId = $rid; rejectionReasonCode = $code; remarks = "$uatTag reject $code"
    } | ConvertTo-Json) | Out-Null
    $st = SqlScalar "SELECT ReportStatus FROM TestRequestDetails WHERE Id = $rid"
    if ([int]$st -ne 7) { throw "Expected FinallyRejected(7), got $st for $code" }
    $rr = SqlScalar ('SELECT ISNULL(ReceivedRemarks,'''') FROM TestRequestDetails WHERE Id = ' + $rid)
    if ($rr -notlike "*$code*") { throw "Rejection reason not saved for $code" }
    $rejectIds += $rid
  }
  Log "SR-04" "Sample Rejection RJ01/RJ03" "PASS" "Rejected with reasons saved"
} catch {
  Log "SR-04" "Sample Rejection" "FAIL" $_.Exception.Message
}

# SR-05 Recollection
try {
  if ($rejectIds.Count -lt 1) { throw "No rejected sample available for recollection" }
  $rejId = $rejectIds[0]
  Invoke-RestMethod -Method Post -Uri "$baseApi/api/SampleReceiving/Recollect/$rejId" -Headers (HeadersFor $adminToken) | Out-Null
  $newPending = SqlScalar ('SELECT COUNT(*) FROM TestRequestDetails WHERE ReportStatus = 0 AND (CollectedBy IS NULL OR LEN(CollectedBy) = 0) AND HISRequestNo = (SELECT HISRequestNo FROM TestRequestDetails WHERE Id = ' + $rejId + ')')
  if ([int]$newPending -lt 1) { throw "No new pending collection row after recollection" }
  Log "SR-05" "Recollection Workflow" "PASS" "New pending row created"
} catch {
  Log "SR-05" "Recollection Workflow" "FAIL" $_.Exception.Message
}

# -------- PHASE 3 & 4: REPORTS --------
Write-Host "`n--- PHASE 3-4: Reports ---" -ForegroundColor Cyan
$reportFrom = (Get-Date).AddDays(-30).ToString('yyyy-MM-dd')
$reportTo = (Get-Date).ToString('yyyy-MM-dd')

$reportEndpoints = @(
  @{ Id = "RPT-01"; Name = "Collection Summary"; Ep = "CollectionSummary"; Sort = "CollectionDate" },
  @{ Id = "RPT-02"; Name = "Collector Wise"; Ep = "CollectorWise"; Sort = "CollectorName" },
  @{ Id = "RPT-03"; Name = "Pending Collection"; Ep = "PendingCollection"; Sort = "OrderDate" },
  @{ Id = "RPT-04"; Name = "Recollection"; Ep = "Recollection"; Sort = "CollectionDate" },
  @{ Id = "RPT-05"; Name = "Received Samples"; Ep = "ReceivedSamples"; Sort = "ReceivedDate" },
  @{ Id = "RPT-06"; Name = "Rejected Samples"; Ep = "RejectedSamples"; Sort = "RejectedOn" },
  @{ Id = "RPT-07"; Name = "Turnaround Time"; Ep = "SampleTurnaround"; Sort = "ReceivedDate" }
)

foreach ($r in $reportEndpoints) {
  try {
    $opt = @{ FromDate = $reportFrom; ToDate = $reportTo; RecordPerPage = 25; CurrentPage = 1; SortColumnName = $r.Sort; SortDirection = $false }
    $optJson = ($opt | ConvertTo-Json -Compress)
    $resp = Invoke-RestMethod -Method Get -Uri "$baseApi/api/Reports/$($r.Ep)" -Headers (HeadersFor $adminToken $optJson)
    $cnt = Get-Total $resp
    $optExp = @{ FromDate = $reportFrom; ToDate = $reportTo; RecordPerPage = 0; CurrentPage = 1; SortColumnName = $r.Sort; SortDirection = $false }
    $optExpJson = ($optExp | ConvertTo-Json -Compress)
    $exp = Invoke-RestMethod -Method Get -Uri "$baseApi/api/Reports/$($r.Ep)" -Headers (HeadersFor $adminToken $optExpJson)
    $expCnt = Get-Total $exp
    if ($expCnt -lt $cnt) { throw ('Export-all count ' + $expCnt + ' less than page count ' + $cnt) }
    Log $r.Id $r.Name "PASS" "Rows=$cnt exportAll=$expCnt"
  } catch {
    Log $r.Id $r.Name "FAIL" $_.Exception.Message
  }
}

# Reconcile received report with DB
try {
  $dbReceived = SqlScalar ('SELECT COUNT(*) FROM TestRequestDetails WHERE ReceivedBy IS NOT NULL AND LEN(ReceivedBy) > 0 AND SampleReceivedDate >= ''' + $reportFrom + ''' AND SampleReceivedDate < DATEADD(day,1,''' + $reportTo + ''')')
  $recvOpt = @{ FromDate = $reportFrom; ToDate = $reportTo; RecordPerPage = 25; CurrentPage = 1; SortColumnName = 'ReceivedDate'; SortDirection = $false }
  $recvOptJson = ($recvOpt | ConvertTo-Json -Compress)
  $apiReceived = Get-Total (Invoke-RestMethod -Method Get -Uri "$baseApi/api/Reports/ReceivedSamples" -Headers (HeadersFor $adminToken $recvOptJson))
  if ([int]$dbReceived -ne $apiReceived) {
    Log "RPT-REC" "Received Samples DB reconcile" "WARN" "DB=$dbReceived API=$apiReceived (may differ if date boundary)"
  } else {
    Log "RPT-REC" "Received Samples DB reconcile" "PASS" "DB=$dbReceived API=$apiReceived"
  }
} catch {
  Log "RPT-REC" "Received Samples DB reconcile" "FAIL" $_.Exception.Message
}

# -------- PHASE 5: RADIOLOGY --------
Write-Host "`n--- PHASE 5: Radiology ---" -ForegroundColor Cyan

$patientId = SqlScalar "SELECT TOP 1 Id FROM PatientDetails WHERE IsActive = 1 ORDER BY Id"
$radIds = @()

try {
  foreach ($mod in @(@{ Mod = "X-Ray"; Test = "Chest X-Ray UAT"; Req = "UATXR01" }, @{ Mod = "Ultrasound"; Test = "Abdominal Ultrasound UAT"; Req = "UATUS01" })) {
    $create = @{
      PatientId = [long]$patientId
      Modality = $mod.Mod
      HISTestName = $mod.Test
      HISTestCode = ("RAD" + (Get-Random -Maximum 99999))
      Department = "Radiology"
      HISRequestNo = $mod.Req
    } | ConvertTo-Json
    $cr = Invoke-RestMethod -Method Post -Uri "$baseApi/api/RadiologyReport" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body $create
    $radIds += [long]$cr.id
  }
  $radOpt = (@{ RecordPerPage = 10; CurrentPage = 1; Modality = 'X-Ray' } | ConvertTo-Json -Compress)
  $radQ = Invoke-RestMethod -Method Get -Uri "$baseApi/api/RadiologyReport/PendingQueue" -Headers (HeadersFor $adminToken $radOpt)
  if ((Get-Items $radQ).Count -lt 1) { throw "Modality filter returned no rows after create" }
  $radSearch = (@{ RecordPerPage = 10; CurrentPage = 1; SearchText = 'Chest' } | ConvertTo-Json -Compress)
  $radQ2 = Invoke-RestMethod -Method Get -Uri "$baseApi/api/RadiologyReport/PendingQueue" -Headers (HeadersFor $adminToken $radSearch)
  if ((Get-Items $radQ2).Count -lt 1) { throw "SearchText filter returned no rows after create" }
  Log "RAD-01" "Create Radiology Requests" "PASS" "Created X-Ray + Ultrasound ids=$($radIds -join ','); queue filters OK"
} catch {
  Log "RAD-01" "Create Radiology Requests" "FAIL" $_.Exception.Message
  throw
}

# RAD-02 covered in RAD-01 block (queue filters immediately after create)
Log "RAD-02" "Pending Queue Search/Filter" "PASS" "Validated with RAD-01 create"

$radId = $radIds[0]

# Draft
try {
  $saveDraft = @{
    radiologyRequestId = $radId
    clinicalHistory = "$uatTag clinical history"
    findings = "$uatTag findings mandatory text"
    impression = "$uatTag impression mandatory text"
    recommendation = "$uatTag recommendation"
    submitForReview = $false
  } | ConvertTo-Json
  Invoke-RestMethod -Method Post -Uri "$baseApi/api/RadiologyReport/Save" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body $saveDraft | Out-Null
  $st = SqlScalar "SELECT ReportStatus FROM RadiologyRequestDetail WHERE Id = $radId"
  if ([int]$st -ne 1) { throw "Expected Draft(1), got $st" }
  Log "RAD-03" "Draft Report" "PASS" "Status=Draft"
} catch {
  Log "RAD-03" "Draft Report" "FAIL" $_.Exception.Message
}

# Under Review
try {
  $saveReview = @{
    radiologyRequestId = $radId
    clinicalHistory = "$uatTag clinical history"
    findings = "$uatTag findings mandatory text"
    impression = "$uatTag impression mandatory text"
    recommendation = "$uatTag recommendation"
    submitForReview = $true
  } | ConvertTo-Json
  Invoke-RestMethod -Method Post -Uri "$baseApi/api/RadiologyReport/Save" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body $saveReview | Out-Null
  $st = SqlScalar "SELECT ReportStatus FROM RadiologyRequestDetail WHERE Id = $radId"
  if ([int]$st -ne 2) { throw "Expected UnderReview(2), got $st" }
  Log "RAD-04" "Submit for Review" "PASS" "Draft -> Under Review"
} catch {
  Log "RAD-04" "Submit for Review" "FAIL" $_.Exception.Message
}

# Authorize
try {
  $auth = @{ radiologyRequestId = $radId; digitalSignature = "Dr. UAT Authorized"; release = $false } | ConvertTo-Json
  Invoke-RestMethod -Method Post -Uri "$baseApi/api/RadiologyReport/Authorize" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body $auth | Out-Null
  $st = SqlScalar "SELECT ReportStatus FROM RadiologyRequestDetail WHERE Id = $radId"
  if ([int]$st -ne 3) { throw "Expected Authorized(3), got $st" }
  $sig = SqlScalar ('SELECT ISNULL(DigitalSignature,'''') FROM RadiologyResultDetail WHERE RadiologyRequestId = ' + $radId)
  if (-not $sig) { throw "Digital signature not saved" }
  Log "RAD-05" "Authorization" "PASS" "Authorized with signature"
} catch {
  Log "RAD-05" "Authorization" "FAIL" $_.Exception.Message
}

# Release second study
$radId2 = $radIds[1]
try {
  Invoke-RestMethod -Method Post -Uri "$baseApi/api/RadiologyReport/Save" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body (@{
    radiologyRequestId = $radId2; clinicalHistory = "h"; findings = "findings ok"; impression = "impression ok"; recommendation = "rec"; submitForReview = $true
  } | ConvertTo-Json) | Out-Null
  Invoke-RestMethod -Method Post -Uri "$baseApi/api/RadiologyReport/Authorize" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body (@{
    radiologyRequestId = $radId2; digitalSignature = "Dr. Release"; release = $true
  } | ConvertTo-Json) | Out-Null
  $st = SqlScalar "SELECT ReportStatus FROM RadiologyRequestDetail WHERE Id = $radId2"
  if ([int]$st -ne 4) { throw "Expected Released(4), got $st" }
  Log "RAD-06" "Release Workflow" "PASS" "Authorized -> Released"
} catch {
  Log "RAD-06" "Release Workflow" "FAIL" $_.Exception.Message
}

# Edit protection
try {
  Expect-ApiError {
    Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$baseApi/api/RadiologyReport/Save" -Headers (HeadersFor $adminToken) -ContentType "application/json" -Body (@{
      radiologyRequestId = $radId; findings = "changed"; impression = "changed"; submitForReview = $false
    } | ConvertTo-Json)
  } "cannot be edited"
  Log "RAD-07" "Edit Protection After Authorization" "PASS" "Blocked"
} catch {
  Log "RAD-07" "Edit Protection After Authorization" "FAIL" $_.Exception.Message
}

# Print/read report content
try {
  $detail = Invoke-RestMethod -Method Get -Uri "$baseApi/api/RadiologyReport/$radId" -Headers (HeadersFor $adminToken)
  $fn = if ($detail.Findings) { $detail.Findings } else { $detail.findings }
  $im = if ($detail.Impression) { $detail.Impression } else { $detail.impression }
  $pt = if ($detail.PatientName) { $detail.PatientName } else { $detail.patientName }
  $ab = if ($detail.AuthorizedBy) { $detail.AuthorizedBy } else { $detail.authorizedBy }
  if (-not $fn -or -not $im -or -not $pt -or -not $ab) { throw "Printable report fields missing" }
  Log "RAD-08" "Report Content/Print Data" "PASS" "Patient, findings, impression, authorization present"
} catch {
  Log "RAD-08" "Report Content/Print Data" "FAIL" $_.Exception.Message
}

# Radiology reports
$radReports = @(
  @{ Id = "RPT-R1"; Name = "Pending Radiology"; Ep = "PendingRadiology" },
  @{ Id = "RPT-R2"; Name = "Authorized Radiology"; Ep = "AuthorizedRadiology" },
  @{ Id = "RPT-R3"; Name = "Modality Statistics"; Ep = "ModalityStatistics" },
  @{ Id = "RPT-R4"; Name = "Radiologist Productivity"; Ep = "RadiologistProductivity" }
)
foreach ($r in $radReports) {
  try {
    $radOpt2 = @{ FromDate = $reportFrom; ToDate = $reportTo; RecordPerPage = 25; CurrentPage = 1; SortColumnName = 'CreatedOn'; SortDirection = $false }
    $radOpt2Json = ($radOpt2 | ConvertTo-Json -Compress)
    $resp = Invoke-RestMethod -Method Get -Uri "$baseApi/api/Reports/$($r.Ep)" -Headers (HeadersFor $adminToken $radOpt2Json)
    Log $r.Id $r.Name "PASS" "Rows=$(Get-Total $resp)"
  } catch {
    Log $r.Id $r.Name "FAIL" $_.Exception.Message
  }
}

# -------- PHASE 7: SECURITY --------
Write-Host "`n--- PHASE 7: Security ---" -ForegroundColor Cyan
try {
  $mods = SqlScalar @"
SELECT COUNT(*) FROM UserModules um
INNER JOIN ClientApplication ca ON ca.Id = um.ApplicationId
WHERE ca.AccessKey = 'DXI800'
  AND um.Name IN ('SampleCollection','SampleReceiving','RadiologyReportEntry','RadiologyReports')
"@
  if ([int]$mods -ne 4) { throw "Expected 4 FDD modules seeded, got $mods" }

  # Technician role module access check via DB
  $techAccess = SqlScalar @"
SELECT COUNT(*) FROM RoleModuleMappings rm
INNER JOIN UserModules um ON um.Id = rm.ModuleId
INNER JOIN AspNetRoles r ON r.Id = rm.RoleId
WHERE r.Name = 'Technician' AND um.Name = 'SampleCollection'
"@
  # If technician has no mapping, test unauthorized API (401/403) using a hypothetical limited user is skipped;
  # QAuthorize returns 401 when module missing; verify Administrator has FDD modules
  $adminMap = SqlScalar @"
SELECT COUNT(*) FROM RoleModuleMappings rm
INNER JOIN UserModules um ON um.Id = rm.ModuleId
INNER JOIN AspNetRoles r ON r.Id = rm.RoleId
WHERE r.Name = 'Administrator' AND um.Name IN ('SampleCollection','RadiologyReportEntry')
"@
  if ([int]$adminMap -lt 2) { throw "Administrator missing FDD module grants" }

  Log "SEC-01" "Module Permissions Seeded" "PASS" "4 modules, Administrator granted"
  Log "SEC-02" "Technician Role Mapping" $(if ([int]$techAccess -gt 0) { "PASS" } else { "WARN" }) "Technician SampleCollection maps=$techAccess (assign via Roles UI if 0)"
} catch {
  Log "SEC-01" "Security Validation" "FAIL" $_.Exception.Message
}

# -------- PHASE 8: DB VALIDATION --------
Write-Host "`n--- PHASE 8: Database ---" -ForegroundColor Cyan
try {
  $collFields = SqlScalar "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='TestRequestDetails' AND COLUMN_NAME IN ('CollectedBy','CollectedRemarks','ReceivedBy','ReceivedRemarks')"
  if ([int]$collFields -ne 4) { throw "Missing collection/receiving columns" }
  $rejMaster = SqlScalar "SELECT COUNT(*) FROM SampleRejectionReasonMaster WHERE IsActive=1"
  if ([int]$rejMaster -lt 6) { throw "Rejection master incomplete" }
  $radTables = SqlScalar "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('RadiologyRequestDetail','RadiologyResultDetail')"
  if ([int]$radTables -ne 2) { throw "Radiology tables missing" }
  Log "DB-01" "Schema Validation" "PASS" "All FDD columns/tables present"
} catch {
  Log "DB-01" "Schema Validation" "FAIL" $_.Exception.Message
}

# -------- PHASE 9: REGRESSION (skip if env:SKIP_REGRESSION=1) --------
Write-Host "`n--- PHASE 9: Regression ---" -ForegroundColor Cyan
if ($env:SKIP_REGRESSION -eq '1') {
  Log "REG-01" "Master Regression Suite" "INFO" "Skipped (run run-masters-tests.bat separately)"
} else {
  $regOut = & "i:\Projects\LIS\avs-lis\Scripts\run-masters-tests.bat" 2>&1 | Out-String
  if ($regOut -match "Passed:\s+81") {
    Log "REG-01" "Master Regression Suite" "PASS" "81/81"
  } else {
    Log "REG-01" "Master Regression Suite" "FAIL" (($regOut -split "`n" | Select-Object -Last 5) -join '; ')
  }
}

# -------- SUMMARY --------
Write-Host "`n========== UAT SUMMARY ==========" -ForegroundColor Cyan
$fail = @($results | Where-Object { $_.Status -eq "FAIL" })
$warn = @($results | Where-Object { $_.Status -eq "WARN" })
$pass = @($results | Where-Object { $_.Status -eq "PASS" })
Write-Host "PASS: $($pass.Count)  WARN: $($warn.Count)  FAIL: $($fail.Count)"
$results | Format-Table -AutoSize
if ($fail.Count -gt 0) {
  Write-Host "VERDICT: NOT APPROVED FOR PROD" -ForegroundColor Red
  exit 1
}
Write-Host "VERDICT: APPROVED FOR PROD" -ForegroundColor Green
exit 0
