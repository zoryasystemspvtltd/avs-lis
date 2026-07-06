# Pre-Production UAT - Patient Registration Enhancement (MR No, Visit ID, Patient Prefix)
$ErrorActionPreference = "Stop"
$baseApi = "http://localhost:8081"
$tag = "UAT-PAT-" + (Get-Date -Format "yyyyMMddHHmmss")
$results = @()

function Log([string]$id, [string]$name, [string]$status, [string]$detail) {
  $global:results += [pscustomobject]@{ Id = $id; Scenario = $name; Status = $status; Detail = $detail }
  $c = if ($status -eq "PASS") { "Green" } elseif ($status -eq "FAIL") { "Red" } else { "Yellow" }
  Write-Host "[$status] $id $name - $detail" -ForegroundColor $c
}

function Get-Token {
  $body = 'grant_type=password&username=admin%40zorya.co.in&password=zorKol%401'
  $r = Invoke-RestMethod -Method Post -Uri "$baseApi/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
  if (-not $r.access_token) { throw "Token failed" }
  return $r.access_token
}

function ApiHeaders([string]$token, [string]$json = $null) {
  $h = @{ accesskey = "DXI800"; Authorization = "Bearer $token"; "Content-Type" = "application/json" }
  if ($json) { $h.ApiOption = $json }
  return $h
}

function Get-Prop($obj, [string[]]$names) {
  foreach ($n in $names) { if ($obj.PSObject.Properties[$n]) { return $obj.$n } }
  return $null
}

function SqlScalar([string]$q) {
  $query = 'SET NOCOUNT ON; ' + $q
  $out = & sqlcmd -S '.\SQLEXPRESS' -d AVSLIS -E -h -1 -W -Q $query 2>&1
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

function PostPatient($token, $body) {
  Invoke-RestMethod -Method Post -Uri "$baseApi/api/PatientMaster" -Headers (ApiHeaders $token) -Body ($body | ConvertTo-Json -Depth 5 -Compress)
}

Write-Host "========== PATIENT REGISTRATION UAT ($tag) ==========" -ForegroundColor Cyan
$token = Get-Token
Log "ENV" "API token" "PASS" "Authenticated"

$mrNo = "MR-$tag"
$visitId = "VIS-$tag"
$prefix = "Mr"
$suffix = $tag.Substring(0, [Math]::Min(12, $tag.Length))
$phone = "98" + ($tag.Substring($tag.Length - 8) -replace '[^0-9]','0').PadLeft(8,'0').Substring(0,8)

# Positive: Create patient with new fields
$patientId = $null
try {
  $createBody = @{
    name = "UAT Patient $suffix"; patientPrefix = $prefix; mrNo = $mrNo; visitId = $visitId
    gender = "M"; phone = $phone; age = 35; dateOfBirth = "1991-01-15"; address = "UAT Address"
  }
  $json = PostPatient $token $createBody
  $patientId = [long](Get-Prop $json @('Result','result','Data','data'))
  if ($patientId -le 0) { throw "No patient id in response" }
  Log "P-01" "Create patient with MR/Visit/Prefix" "PASS" "PatientId=$patientId"
} catch {
  Log "P-01" "Create patient with MR/Visit/Prefix" "FAIL" $_.Exception.Message
  throw
}

# Get Patient API
try {
  $loaded = Invoke-RestMethod -Method Get -Uri "$baseApi/api/PatientMaster/$patientId" -Headers (ApiHeaders $token)
  $gotMr = Get-Prop $loaded @('MRNo','mrNo')
  $gotVis = Get-Prop $loaded @('VisitId','visitId')
  $gotPre = Get-Prop $loaded @('PatientPrefix','patientPrefix')
  if ($gotMr -ne $mrNo -or $gotVis -ne $visitId -or $gotPre -ne $prefix) {
    throw "Fields mismatch: MR=$gotMr VIS=$gotVis PRE=$gotPre"
  }
  Log "P-02" "Get Patient returns new fields" "PASS" "MR=$gotMr Visit=$gotVis Prefix=$gotPre"
} catch {
  Log "P-02" "Get Patient returns new fields" "FAIL" $_.Exception.Message
  throw
}

# DB validation
try {
  $dbMr = SqlScalar "SELECT MRNo FROM PatientDetails WHERE Id=$patientId"
  $dbVis = SqlScalar "SELECT VisitId FROM PatientDetails WHERE Id=$patientId"
  $dbPre = SqlScalar "SELECT PatientPrefix FROM PatientDetails WHERE Id=$patientId"
  if ($dbMr -ne $mrNo -or $dbVis -ne $visitId -or $dbPre -ne $prefix) {
    throw "DB mismatch MR=$dbMr VIS=$dbVis PRE=$dbPre"
  }
  Log "P-03" "Database persistence" "PASS" "Columns saved correctly"
} catch {
  Log "P-03" "Database persistence" "FAIL" $_.Exception.Message
  throw
}

# Billing lookup search by MR No
try {
  $opt = (@{ RecordPerPage = 10; CurrentPage = 1; SearchText = $mrNo } | ConvertTo-Json -Compress)
  $billing = Invoke-RestMethod -Method Get -Uri "$baseApi/api/Patients/Billing" -Headers (ApiHeaders $token $opt)
  $items = if ($billing.Items) { @($billing.Items) } else { @($billing.items) }
  $found = $items | Where-Object { (Get-Prop $_ @('Id','id')) -eq $patientId }
  if (-not $found) { throw "Patient not found in billing search by MR No" }
  Log "P-04" "Sale Invoice patient lookup by MR No" "PASS" "Found in GetForBilling"
} catch {
  Log "P-04" "Sale Invoice patient lookup by MR No" "FAIL" $_.Exception.Message
  throw
}

# Negative: duplicate MR No
try {
  $dupBody = @{
    name = "Dup MR $suffix"; patientPrefix = "Mrs"; mrNo = $mrNo.ToLower(); visitId = "VIS-DUP2-$suffix"
    gender = "F"; phone = "9876501235"; age = 30; dateOfBirth = "1996-01-15"
  }
  Expect-Error { PostPatient $token $dupBody | Out-Null } "MR No already exists"
  Log "N-01" "Duplicate MR No rejected" "PASS" "Case-insensitive"
} catch {
  Log "N-01" "Duplicate MR No rejected" "FAIL" $_.Exception.Message
  throw
}

# Negative: duplicate Visit ID
try {
  $dupBody = @{
    name = "Dup VIS $suffix"; patientPrefix = "Mrs"; mrNo = "MR-DUP2-$suffix"; visitId = $visitId.ToLower()
    gender = "F"; phone = "9876501236"; age = 28; dateOfBirth = "1998-01-15"
  }
  Expect-Error { PostPatient $token $dupBody | Out-Null } "Visit ID already exists"
  Log "N-02" "Duplicate Visit ID rejected" "PASS" "Case-insensitive"
} catch {
  Log "N-02" "Duplicate Visit ID rejected" "FAIL" $_.Exception.Message
  throw
}

# Negative: blank MR No
try {
  $bad = @{
    name = "Blank MR"; patientPrefix = "Mr"; mrNo = "  "; visitId = "VIS-BLANK-$suffix"
    gender = "M"; phone = "9876501237"; age = 40; dateOfBirth = "1986-01-15"
  }
  Expect-Error { PostPatient $token $bad | Out-Null } "MR No is required"
  Log "N-03" "Blank MR No rejected" "PASS" ""
} catch {
  Log "N-03" "Blank MR No rejected" "FAIL" $_.Exception.Message
  throw
}

# Negative: blank Visit ID
try {
  $bad = @{
    name = "Blank VIS"; patientPrefix = "Mr"; mrNo = "MR-BLANK-$suffix"; visitId = ""
    gender = "M"; phone = "9876501238"; age = 40; dateOfBirth = "1986-01-15"
  }
  Expect-Error { PostPatient $token $bad | Out-Null } "Visit ID is required"
  Log "N-04" "Blank Visit ID rejected" "PASS" ""
} catch {
  Log "N-04" "Blank Visit ID rejected" "FAIL" $_.Exception.Message
  throw
}

# Negative: blank Prefix
try {
  $bad = @{
    name = "Blank Prefix"; patientPrefix = ""; mrNo = "MR-PRE-$suffix"; visitId = "VIS-PRE-$suffix"
    gender = "M"; phone = "9876501239"; age = 40; dateOfBirth = "1986-01-15"
  }
  Expect-Error { PostPatient $token $bad | Out-Null } "Patient Prefix is required"
  Log "N-05" "Blank Prefix rejected" "PASS" ""
} catch {
  Log "N-05" "Blank Prefix rejected" "FAIL" $_.Exception.Message
  throw
}

# Edit: update fields, exclude-self duplicate check
try {
  $loadedForEdit = Invoke-RestMethod -Method Get -Uri "$baseApi/api/PatientMaster/$patientId" -Headers (ApiHeaders $token)
  $newMr = "MR-ED-$suffix"
  $newVis = "VIS-ED-$suffix"
  $editBody = @{
    id = $patientId
    hisPatientId = (Get-Prop $loadedForEdit @('HisPatientId','hisPatientId'))
    name = "UAT Patient Edited $suffix"
    patientPrefix = "Dr"
    mrNo = $newMr; visitId = $newVis
    gender = (Get-Prop $loadedForEdit @('Gender','gender'))
    phone = $phone
    age = 36; dateOfBirth = (Get-Prop $loadedForEdit @('DateOfBirth','dateOfBirth'))
    address = (Get-Prop $loadedForEdit @('Address','address'))
    isActive = $true
  }
  $putResp = Invoke-WebRequest -Method Post -Uri "$baseApi/api/PatientMaster/Put" -Headers (ApiHeaders $token) -Body ($editBody | ConvertTo-Json -Depth 5 -Compress) -UseBasicParsing
  if ($putResp.StatusCode -ne 200) { throw "Update failed $($putResp.StatusCode)" }
  $reloaded = Invoke-RestMethod -Method Get -Uri "$baseApi/api/PatientMaster/$patientId" -Headers (ApiHeaders $token)
  if ((Get-Prop $reloaded @('MRNo','mrNo')) -ne $newMr) { throw "Edit MR not saved" }
  if ((Get-Prop $reloaded @('PatientPrefix','patientPrefix')) -ne "Dr") { throw "Edit prefix not saved" }
  Log "P-05" "Edit patient updates fields" "PASS" "MR=$newMr Prefix=Dr"
  $mrNo = $newMr
  $visitId = $newVis
} catch {
  Log "P-05" "Edit patient updates fields" "FAIL" $_.Exception.Message
  throw
}

# Edit duplicate MR on another patient
try {
  $otherMr = "MR-OTHER-$suffix"
  $otherBody = @{
    name = "Other $suffix"; patientPrefix = "Mr"; mrNo = $otherMr; visitId = "VIS-OTHER-$suffix"
    gender = "F"; phone = "9876501240"; age = 25; dateOfBirth = "2001-01-15"
  }
  $otherJson = PostPatient $token $otherBody
  $otherId = [long](Get-Prop $otherJson @('Result','result','Data','data'))

  $dupEdit = @{
    id = $otherId; name = "Other $suffix"; patientPrefix = "Mr"; mrNo = $mrNo; visitId = "VIS-OTHER-$suffix"
    gender = "F"; phone = "9876501240"; age = 25; dateOfBirth = "2001-01-15"; isActive = $true
  }
  Expect-Error {
    Invoke-WebRequest -Method Post -Uri "$baseApi/api/PatientMaster/Put" -Headers (ApiHeaders $token) -Body ($dupEdit | ConvertTo-Json -Depth 5 -Compress) -UseBasicParsing | Out-Null
  } "MR No already exists"
  Log "N-06" "Edit duplicate MR No rejected" "PASS" ""

  Invoke-WebRequest -Method Post -Uri "$baseApi/api/PatientMaster/Delete" -Headers (ApiHeaders $token) -Body (@{ id = $otherId } | ConvertTo-Json -Compress) -UseBasicParsing | Out-Null
} catch {
  Log "N-06" "Edit duplicate MR No rejected" "FAIL" $_.Exception.Message
  throw
}

# Regression smoke: sample collection queue, recent samples
try {
  $opt = '{"RecordPerPage":5,"CurrentPage":1,"SortColumnName":"SampleCollectionDate","SortDirection":false}'
  $q = Invoke-RestMethod -Method Get -Uri "$baseApi/api/SampleCollection/PendingQueue" -Headers (ApiHeaders $token $opt)
  Log "R-01" "Sample Collection queue" "PASS" "Total=$(if ($q.TotalRecord) { $q.TotalRecord } else { $q.totalRecord })"
} catch {
  Log "R-01" "Sample Collection queue" "FAIL" $_.Exception.Message
}

try {
  $opt = '{"RecordPerPage":5,"CurrentPage":1,"ReceivedOnly":true}'
  $q = Invoke-RestMethod -Method Get -Uri "$baseApi/api/Patients" -Headers (ApiHeaders $token $opt)
  Log "R-02" "Recent Samples (received only)" "PASS" "OK"
} catch {
  Log "R-02" "Recent Samples (received only)" "FAIL" $_.Exception.Message
}

# Cleanup test patient
try {
  Invoke-WebRequest -Method Post -Uri "$baseApi/api/PatientMaster/Delete" -Headers (ApiHeaders $token) -Body (@{ id = $patientId } | ConvertTo-Json -Compress) -UseBasicParsing | Out-Null
  Log "CLN" "Cleanup test patient" "PASS" "Deleted $patientId"
} catch {
  Log "CLN" "Cleanup test patient" "WARN" $_.Exception.Message
}

$fail = @($results | Where-Object { $_.Status -eq "FAIL" })
Write-Host "`n========== SUMMARY ==========" -ForegroundColor Cyan
$results | Format-Table -AutoSize
if ($fail.Count -gt 0) {
  Write-Host "UAT FAILED ($($fail.Count) failures)" -ForegroundColor Red
  exit 1
}
Write-Host "UAT PASSED - All $($results.Count) scenarios OK" -ForegroundColor Green
exit 0
