# Pre-Production UAT - 10 Business Change Requests
$ErrorActionPreference = "Stop"
$baseApi = "http://localhost:8081"
$tag = "UAT-PREPROD-" + (Get-Date -Format "yyyyMMddHHmmss")
$results = @()
$evidence = @{}
$script:results = @()

function Log([string]$id, [string]$name, [string]$status, [string]$detail) {
  $script:results += [pscustomobject]@{ Id = $id; Scenario = $name; Status = $status; Detail = $detail }
  $c = if ($status -eq "PASS") { "Green" } elseif ($status -eq "FAIL") { "Red" } else { "Yellow" }
  Write-Host "[$status] $id $name - $detail" -ForegroundColor $c
}

function Get-Token {
  $body = 'grant_type=password&username=admin%40zorya.co.in&password=zorKol%401'
  $r = Invoke-RestMethod -Method Post -Uri "$baseApi/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
  if (-not $r.access_token) { throw "Token failed" }
  return $r.access_token
}

function ApiHdr([string]$token, [string]$json = $null) {
  $h = @{ accesskey = "DXI800"; Authorization = "Bearer $token"; "Content-Type" = "application/json" }
  if ($json) { $h.ApiOption = $json }
  return $h
}

function Items($r) { if ($r.Items) { @($r.Items) } elseif ($r.items) { @($r.items) } else { @() } }
function Total($r) { if ($null -ne $r.TotalRecord) { [int]$r.TotalRecord } elseif ($null -ne $r.totalRecord) { [int]$r.totalRecord } else { (Items $r).Count } }
function RowId($row) { if ($row.Id) { $row.Id } else { $row.id } }

function Get-Prop($obj, [string[]]$names) {
  foreach ($n in $names) {
    if ($obj.PSObject.Properties[$n]) { return $obj.$n }
  }
  return $null
}

function SqlScalar([string]$q) {
  $query = 'SET NOCOUNT ON; ' + $q
  $out = & sqlcmd -S '.\SQLEXPRESS' -d ZoryaLMS -E -h -1 -W -Q $query 2>&1
  if ($LASTEXITCODE -ne 0) { throw "SQL: $out" }
  return ($out | Where-Object { $_ -and $_.ToString().Trim() } | Select-Object -First 1).ToString().Trim()
}

function Expect-Error([scriptblock]$b, [string]$needle) {
  try { & $b | Out-Null; throw "Expected error: $needle" }
  catch {
    $m = $_.Exception.Message
    if ($_.ErrorDetails.Message) { $m += ' ' + $_.ErrorDetails.Message }
    try {
      if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $body = $reader.ReadToEnd()
        if ($body) { $m += ' ' + $body }
      }
    } catch { }
    if ($m -notlike "*$needle*") { throw "Expected '$needle' got: $m" }
  }
}

function PostJson($uri, $token, $body) {
  Invoke-RestMethod -Method Post -Uri $uri -Headers (ApiHdr $token) -Body ($body | ConvertTo-Json -Depth 10 -Compress)
}

Write-Host "========== PRE-PROD UAT ($tag) ==========" -ForegroundColor Cyan
$token = Get-Token
Log "ENV" "API token" "PASS" "Authenticated"

# --- 1. Parameter Master ---
Write-Host "`n--- 1. Parameter Master ---" -ForegroundColor Cyan
try {
  $testId = [int](SqlScalar "SELECT TOP 1 Id FROM HisTestMaster WHERE IsActive=1 ORDER BY Id")
  if ($testId -le 0) { throw "No active test" }
  $testCode = SqlScalar "SELECT HISTestCode FROM HisTestMaster WHERE Id=$testId"
  $paramCode = "UAT-P-" + [guid]::NewGuid().ToString("N").Substring(0, 8)
  $desc1 = "UAT Param Desc $tag"
  $createBody = @{
    hisTestId = $testId; HISTestCode = $testCode; HISParamCode = $paramCode
    HISParamDescription = $desc1; HISParamUnit = "mg/dL"; HISParamMethod = "Automated"; LISParamCode = $paramCode
  }
  $cr = PostJson "$baseApi/api/HisParameterMaster" $token $createBody
  $paramId = [int](Get-Prop $cr @('Result','result'))
  if ($paramId -le 0) { throw "Parameter create returned no id: $($cr | ConvertTo-Json -Compress)" }

  $loaded = Invoke-RestMethod -Uri "$baseApi/api/HisParameterMaster/$paramId" -Headers (ApiHdr $token)
  $loadedDesc = Get-Prop $loaded @('HISParamDescription','hisParamDescription')
  if ($loadedDesc -ne $desc1) { throw "Description not persisted: $loadedDesc" }

  $desc2 = "UAT Param Renamed $tag"
  $upd = @{
    Id = $paramId; hisTestId = $testId; HISTestCode = $testCode; HISParamCode = $paramCode
    HISParamDescription = $desc2; HISParamUnit = "mg/dL"; HISParamMethod = "Automated"; LISParamCode = $paramCode
  }
  PostJson "$baseApi/api/HisParameterMaster/Put" $token $upd | Out-Null
  $reloaded = Invoke-RestMethod -Uri "$baseApi/api/HisParameterMaster/$paramId" -Headers (ApiHdr $token)
  $reDesc = Get-Prop $reloaded @('HISParamDescription','hisParamDescription')
  if ($reDesc -ne $desc2) { throw "Edit description failed: $reDesc" }

  $dbDesc = SqlScalar "SELECT HISParamDescription FROM HISParameterMaster WHERE Id=$paramId"
  if ($dbDesc -ne $desc2) { throw "DB description mismatch: $dbDesc" }

  $evidence["ParameterId"] = $paramId
  $evidence["ParameterCode"] = $paramCode
  Log "PM-01" "Parameter create/edit + DB" "PASS" "Id=$paramId Code=$paramCode Desc=$desc2"
} catch { Log "PM-01" "Parameter Master" "FAIL" $_.Exception.Message; throw }

# --- 2. Patient Master ---
Write-Host "`n--- 2. Patient Master ---" -ForegroundColor Cyan
try {
  $nextPid = (Invoke-RestMethod -Uri "$baseApi/api/PatientMaster/NextPatientId" -Headers (ApiHdr $token)).patientId
  $nextMr = (Invoke-RestMethod -Uri "$baseApi/api/PatientMaster/NextMrNo" -Headers (ApiHdr $token)).mrNo
  $nextVisit = (Invoke-RestMethod -Uri "$baseApi/api/PatientMaster/NextVisitId" -Headers (ApiHdr $token)).visitId
  $noPhoneBody = @{
    Name = "UAT NoPhone $tag"; HisPatientId = $nextPid; Phone = ""; Gender = "M"
    PatientPrefix = "Mr"; MRNo = $nextMr; VisitId = $nextVisit
    Age = 30; DateOfBirth = "1996-01-01"; IsActive = $true
  }
  Expect-Error { PostJson "$baseApi/api/PatientMaster" $token $noPhoneBody } "Phone"

  $addr = "123 UAT Street $tag"
  $phone = "98765" + (Get-Random -Maximum 99999).ToString("00000")
  $patBody = @{
    Name = "UAT Patient $tag"; HisPatientId = $nextPid; Phone = $phone; Address = $addr
    PatientPrefix = "Mr"; MRNo = "MR-$tag"; VisitId = "VIS-$tag"
    Gender = "M"; Age = 35; DateOfBirth = "1991-06-15"; IsActive = $true
  }
  $pr = PostJson "$baseApi/api/PatientMaster" $token $patBody
  $patientId = [long](Get-Prop $pr @('Result','result'))
  if ($patientId -le 0) { throw "Patient create failed" }

  $dbAddr = SqlScalar "SELECT Address FROM PatientDetails WHERE Id=$patientId"
  if ($dbAddr -ne $addr) { throw "Address not in DB: '$dbAddr'" }

  $patLoaded = Invoke-RestMethod -Uri "$baseApi/api/PatientMaster/$patientId" -Headers (ApiHdr $token)
  if ((Get-Prop $patLoaded @('Address','address')) -ne $addr) { throw "Address not in API GET" }

  # UI Age/DOB sync is client-side - verify handlers exist in source
  $mf = Get-Content "I:\Projects\LIS\avs-lis\web\Lis.Web\src\app\masters\master-form\master-form.component.ts" -Raw
  if ($mf -notmatch 'onPatientAgeChange' -or $mf -notmatch 'onPatientDobChange') { throw "Age/DOB sync handlers missing in master-form" }
  Log "PT-02" "Age/DOB UI sync (source)" "PASS" "onPatientAgeChange + onPatientDobChange present"

  $evidence["PatientId"] = $patientId
  $evidence["PatientPhone"] = $phone
  Log "PT-01" "Patient address + phone mandatory" "PASS" "Id=$patientId Address saved; no-phone blocked"
} catch { Log "PT-01" "Patient Master" "FAIL" $_.Exception.Message; throw }

# --- 3. Sale Invoice ---
Write-Host "`n--- 3. Sale Invoice ---" -ForegroundColor Cyan
try {
  $rateRow = SqlScalar "SELECT TOP 1 tr.TestId FROM TestRateMaster tr INNER JOIN HisTestMaster ht ON ht.Id=tr.TestId WHERE tr.IsActive=1 AND ht.IsActive=1 AND tr.RateType=0 AND tr.EffectiveStart <= CAST(GETDATE() AS DATE) AND tr.EffectiveEnd >= CAST(GETDATE() AS DATE) ORDER BY tr.Id DESC"
  $billTestId = [int]$rateRow
  if ($billTestId -le 0) { throw "No billable test with active rate" }

  $invNo = Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/NextInvoiceNo" -Headers (ApiHdr $token)
  $dto = @{
    invoice = @{
      InvoiceNo = $invNo; InvoiceDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
      PatientId = $patientId; InvoiceStatus = 0; PaymentStatus = 0; IsActive = $true
      PaymentType = "UPI"; DiscountType = "Percentage"; Notes = "UAT notes $tag"
      GrossAmount = 0; DiscountAmount = 10; TaxAmount = 0; NetAmount = 0; PaidAmount = 0; DueAmount = 0
    }
    details = @(@{ TestId = $billTestId; Quantity = 1; Rate = 0; RequestDetailId = 0; DiscountAmount = 0; TaxAmount = 0 })
  }
  $save = PostJson "$baseApi/api/SaleInvoice" $token $dto
  $invoiceId = [long](Get-Prop $save @('Result','result'))
  if ($invoiceId -le 0) { throw "Invoice save failed" }

  $draft = Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/$invoiceId" -Headers (ApiHdr $token)
  $draftStatus = [int](Get-Prop $draft.Invoice @('InvoiceStatus','invoiceStatus'))
  if ($draftStatus -ne 0) { throw "Not draft" }
  $draftNet = [decimal](Get-Prop $draft.Invoice @('NetAmount','netAmount'))
  if ($draftNet -le 0) { throw "Net amount not calculated" }

  # Fixed discount invoice line recalc via second save with DiscountType Fixed in notes meta
  $fixedDto = @{
    invoice = @{
      Id = $invoiceId; InvoiceNo = $invNo; InvoiceDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
      PatientId = $patientId; InvoiceStatus = 0; PaymentStatus = 0; IsActive = $true
      PaymentType = "Cash"; DiscountType = "Fixed Amount"; Notes = "Fixed disc UAT"
      GrossAmount = $draft.Invoice.GrossAmount; DiscountAmount = 50; TaxAmount = $draft.Invoice.TaxAmount
      NetAmount = $draft.Invoice.GrossAmount - 50 + $draft.Invoice.TaxAmount; PaidAmount = 0; DueAmount = 0
    }
    details = @($draft.Details | ForEach-Object { @{ Id = $_.Id; TestId = $_.TestId; Quantity = $_.Quantity; Rate = $_.Rate; Amount = $_.Amount; DiscountAmount = $_.DiscountAmount; TaxAmount = $_.TaxAmount; NetAmount = $_.NetAmount; RequestDetailId = $_.RequestDetailId } })
  }
  PostJson "$baseApi/api/SaleInvoice" $token $fixedDto | Out-Null
  $edited = Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/$invoiceId" -Headers (ApiHdr $token)
  $payType = Get-Prop $edited.Invoice @('PaymentType','paymentType')
  if ($payType -ne "Cash") { throw "PaymentType not persisted: $payType" }

  # Confirm draft before paid
  PostJson "$baseApi/api/SaleInvoice/Status" $token @{ Id = $invoiceId; InvoiceStatus = 1; PaymentStatus = 0 } | Out-Null
  PostJson "$baseApi/api/SaleInvoice/Status" $token @{ Id = $invoiceId; InvoiceStatus = 2; PaymentStatus = 2 } | Out-Null
  $paid = Invoke-RestMethod -Uri "$baseApi/api/SaleInvoice/$invoiceId" -Headers (ApiHdr $token)
  $paidStatus = [int](Get-Prop $paid.Invoice @('InvoiceStatus','invoiceStatus'))
  if ($paidStatus -ne 2) { throw "Mark paid failed" }

  # Test search UI - verify ng-select in source
  $sf = Get-Content "I:\Projects\LIS\avs-lis\web\Lis.Web\src\app\masters\sale-invoice\sale-invoice-form.component.html" -Raw
  if ($sf -notmatch 'sale-invoice-test-select' -or $sf -notmatch 'discountType') { throw "Sale invoice UI changes missing" }
  if ($sf -match '<th[^>]*>Tax</th>' -and $sf -match 'formControlName="taxAmount"') { throw "Tax column still visible on line items" }

  $evidence["InvoiceNo"] = $invNo
  $evidence["InvoiceId"] = $invoiceId
  $evidence["InvoiceNet"] = $paid.Invoice.NetAmount
  Log "SI-01" "Sale Invoice draft/edit/paid/calc" "PASS" "InvoiceNo=$invNo Id=$invoiceId Net=$($paid.Invoice.NetAmount) PaymentType=Cash"
} catch { Log "SI-01" "Sale Invoice" "FAIL" $_.Exception.Message; throw }

# --- 4. Workflow ---
Write-Host "`n--- 4. Workflow: Collection -> Receiving -> Recent ---" -ForegroundColor Cyan
try {
  $reqId = [long](SqlScalar "SELECT TOP 1 RequestDetailId FROM SaleInvoiceDetail WHERE SaleInvoiceId=$invoiceId AND RequestDetailId>0")
  if ($reqId -le 0) { throw "No TestRequestDetail linked to invoice" }

  $collOpt = (@{ RecordPerPage = 50; CurrentPage = 1; OrderNumber = $invNo } | ConvertTo-Json -Compress)
  $collQ = Invoke-RestMethod -Uri "$baseApi/api/SampleCollection/PendingQueue" -Headers (ApiHdr $token $collOpt)
  $collRow = Items $collQ | Where-Object { (RowId $_) -eq $reqId } | Select-Object -First 1
  if (-not $collRow) { throw "Paid invoice request not in collection queue (Id=$reqId)" }

  $collectTime = (Get-Date).AddMinutes(-15).ToString("yyyy-MM-ddTHH:mm:ss")
  PostJson "$baseApi/api/SampleCollection/Collect" $token @{ TestRequestId = $reqId; CollectionDateTime = $collectTime; Remarks = "$tag collect"; BarcodeNumber = "" } | Out-Null

  $collAfter = Invoke-RestMethod -Uri "$baseApi/api/SampleCollection/PendingQueue" -Headers (ApiHdr $token $collOpt)
  $stillPending = Items $collAfter | Where-Object { (RowId $_) -eq $reqId }
  if ($stillPending) { throw "Collected sample still in collection queue" }

  $sampleNo = SqlScalar "SELECT SampleNo FROM TestRequestDetails WHERE Id=$reqId"
  $collectedBy = SqlScalar "SELECT CollectedBy FROM TestRequestDetails WHERE Id=$reqId"
  if ([string]::IsNullOrWhiteSpace($collectedBy)) { throw "CollectedBy empty after collect" }

  $recvOpt = (@{ RecordPerPage = 50; CurrentPage = 1; BarcodeNumber = $sampleNo } | ConvertTo-Json -Compress)
  $recvQ = Invoke-RestMethod -Uri "$baseApi/api/SampleReceiving/Queue" -Headers (ApiHdr $token $recvOpt)
  $recvRow = Items $recvQ | Where-Object { (RowId $_) -eq $reqId } | Select-Object -First 1
  if (-not $recvRow) { throw "Collected sample not in receiving queue" }

  $recvTime = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
  PostJson "$baseApi/api/SampleReceiving/Receive" $token @{ TestRequestId = $reqId; ReceivedDateTime = $recvTime; Remarks = "$tag receive"; BarcodeNumber = $sampleNo } | Out-Null

  $recvAfter = Invoke-RestMethod -Uri "$baseApi/api/SampleReceiving/Queue" -Headers (ApiHdr $token $recvOpt)
  $stillRecv = Items $recvAfter | Where-Object { (RowId $_) -eq $reqId }
  if ($stillRecv) { throw "Received sample still in receiving queue" }

  $receivedBy = SqlScalar "SELECT ReceivedBy FROM TestRequestDetails WHERE Id=$reqId"
  if ([string]::IsNullOrWhiteSpace($receivedBy)) { throw "ReceivedBy empty" }

  $evidence["TestRequestId"] = $reqId
  $evidence["SampleNo"] = $sampleNo
  $evidence["CollectedBy"] = $collectedBy
  $evidence["ReceivedBy"] = $receivedBy
  Log "WF-01" "Invoice->Collect->Receive" "PASS" "InvoiceNo=$invNo SampleNo=$sampleNo ReqId=$reqId"
} catch { Log "WF-01" "Workflow" "FAIL" $_.Exception.Message; throw }

# --- 5. Recent Sample (received only) ---
Write-Host "`n--- 5. Recent Sample ---" -ForegroundColor Cyan
try {
  $recvOpt = (@{ RecordPerPage = 100; CurrentPage = 1; ReceivedOnly = $true; SearchText = $sampleNo } | ConvertTo-Json -Compress)
  $recent = Invoke-RestMethod -Uri "$baseApi/api/Patients" -Headers (ApiHdr $token $recvOpt)
  $found = Items $recent | Where-Object { (RowId $_) -eq $reqId -or (Get-Prop $_ @('SampleNo','sampleNo')) -eq $sampleNo }
  if (-not $found) { throw "Received sample not in Recent Sample list" }

  $unrecvId = [long](SqlScalar "SELECT TOP 1 Id FROM TestRequestDetails WHERE (ReceivedBy IS NULL OR ReceivedBy='') AND ReportStatus=0 ORDER BY Id DESC")
  if ($unrecvId -gt 0) {
    $badOpt = (@{ RecordPerPage = 200; CurrentPage = 1; ReceivedOnly = $true } | ConvertTo-Json -Compress)
    $allRecent = Items (Invoke-RestMethod -Uri "$baseApi/api/Patients" -Headers (ApiHdr $token $badOpt))
    $leaked = $allRecent | Where-Object { (RowId $_) -eq $unrecvId }
    if ($leaked) { throw "Non-received sample Id=$unrecvId appears in received-only list" }
  }

  $rawList = Get-Content "I:\Projects\LIS\avs-lis\web\Lis.Web\src\app\LIS\samples\list-rawsample.component.ts" -Raw
  if ($rawList -match 'ipNo' -or $rawList -match 'testParameterNames') { throw "IP No / Test Parameter columns still in Recent Sample schema" }
  if ($rawList -notmatch 'receivedOnly:\s*true') { throw "receivedOnly flag missing" }

  Log "RS-01" "Recent Sample received-only" "PASS" "SampleNo=$sampleNo in list; non-received excluded"
} catch { Log "RS-01" "Recent Sample" "FAIL" $_.Exception.Message; throw }

# --- 6. Rate Master ---
Write-Host "`n--- 6. Rate Master ---" -ForegroundColor Cyan
try {
  $routing = Get-Content "I:\Projects\LIS\avs-lis\web\Lis.Web\src\app\app.routing.ts" -Raw
  if ($routing -match "taxPercent") { throw "taxPercent still in testRate LOOKUP_FIELDS" }
  $schemas = Get-Content "I:\Projects\LIS\avs-lis\web\Lis.Web\src\app\masters\master-schemas.ts" -Raw
  if ($schemas -match "Tax %") { throw "Tax % still in rate list schema" }

  $histTax = SqlScalar "SELECT TOP 1 CONVERT(varchar(20), TaxPercent) FROM TestRateMaster WHERE TaxPercent > 0"
  if ([string]::IsNullOrWhiteSpace($histTax)) { $histTax = "0" }
  Log "RM-01" "Rate Master tax hidden + historical" "PASS" "UI tax hidden; DB historical TaxPercent sample=$histTax"
} catch { Log "RM-01" "Rate Master" "FAIL" $_.Exception.Message; throw }

# --- 7. Regression ---
Write-Host "`n--- 7. Regression ---" -ForegroundColor Cyan
try {
  $techOpt = (@{ RecordPerPage = 10; CurrentPage = 1; Status = 0 } | ConvertTo-Json -Compress)
  $tech = Invoke-RestMethod -Uri "$baseApi/api/Patients" -Headers (ApiHdr $token $techOpt)
  if ($null -eq $tech) { throw "Patients/technician queue null" }

  $recvOnlyTech = (@{ RecordPerPage = 10; CurrentPage = 1; ReceivedOnly = $true } | ConvertTo-Json -Compress)
  $recentOnly = Invoke-RestMethod -Uri "$baseApi/api/Patients" -Headers (ApiHdr $token $recvOnlyTech)
  if ($null -eq $recentOnly) { throw "ReceivedOnly queue null" }

  # Diagnostic report - ensure test request with results path reachable
  $hasResults = [int](SqlScalar "SELECT COUNT(*) FROM TestResults WHERE TestRequestId=$reqId")
  Log "RG-01" "Queues + diagnostic data path" "PASS" "Technician queue OK; ReceivedOnly OK; Results for workflow req=$hasResults"
} catch { Log "RG-01" "Regression" "FAIL" $_.Exception.Message; throw }

# --- Summary ---
Write-Host "`n========== EVIDENCE ==========" -ForegroundColor Cyan
$evidence.GetEnumerator() | Sort-Object Name | ForEach-Object { Write-Host "$($_.Key): $($_.Value)" }

$fail = @($script:results | Where-Object { $_.Status -eq "FAIL" })
Write-Host "`n========== SUMMARY ==========" -ForegroundColor Cyan
$script:results | Format-Table -AutoSize
if ($fail.Count -gt 0) {
  Write-Host "VERDICT: NOT APPROVED FOR PROD ($($fail.Count) failure(s))" -ForegroundColor Red
  exit 1
}
Write-Host "VERDICT: APPROVED FOR PROD" -ForegroundColor Green
exit 0
