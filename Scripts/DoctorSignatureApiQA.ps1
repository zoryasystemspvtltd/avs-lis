# Doctor Designation & Signature - API QA
$ErrorActionPreference = "Stop"
$baseApi = "http://localhost:8081"
$results = @()

function Log([string]$id, [string]$name, [string]$status, [string]$detail) {
  $global:results += [pscustomobject]@{ Id = $id; Scenario = $name; Status = $status; Detail = $detail }
  $color = if ($status -eq "PASS") { "Green" } elseif ($status -eq "FAIL") { "Red" } else { "Yellow" }
  Write-Host "[$status] $id $name - $detail" -ForegroundColor $color
}

function Get-Token([string]$user = "admin%40zorya.co.in", [string]$pwd = "zorKol%401") {
  $body = 'grant_type=password&username=' + $user + '&password=' + $pwd
  $r = Invoke-RestMethod -Method Post -Uri "$baseApi/Token" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
  if (-not $r.access_token) { throw "Token failed for $user" }
  return $r.access_token
}

function HeadersFor([string]$token) {
  return @{ accesskey = "DXI800"; Authorization = ("Bearer " + $token); "Content-Type" = "application/json" }
}

function Expect-Status([scriptblock]$block, [int]$expectedStatus, [string]$needle = $null) {
  try {
    & $block | Out-Null
    throw "Expected HTTP $expectedStatus"
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
    $code = 0
    if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
    if ($code -ne $expectedStatus) { throw "Expected HTTP $expectedStatus but got $code. Body: $msg" }
    if ($needle -and $msg -notlike "*$needle*") { throw "Expected '$needle' in: $msg" }
  }
}

function Upload-SignatureFile([string]$token, [string]$userId, [string]$filePath) {
  Add-Type -AssemblyName System.Net.Http
  $client = New-Object System.Net.Http.HttpClient
  $client.DefaultRequestHeaders.Add('accesskey', 'DXI800')
  $client.DefaultRequestHeaders.Add('Authorization', "Bearer $token")
  $content = New-Object System.Net.Http.MultipartFormDataContent
  $stream = [System.IO.File]::OpenRead($filePath)
  $fileContent = New-Object System.Net.Http.StreamContent($stream)
  $ext = [System.IO.Path]::GetExtension($filePath).ToLowerInvariant()
  $mime = if ($ext -eq '.png') { 'image/png' } else { 'image/jpeg' }
  $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse($mime)
  $content.Add($fileContent, 'file', [System.IO.Path]::GetFileName($filePath))
  $response = $client.PostAsync("$baseApi/api/Users/$userId/DoctorSignature", $content).Result
  $body = $response.Content.ReadAsStringAsync().Result
  $stream.Close()
  $client.Dispose()
  if (-not $response.IsSuccessStatusCode) {
    throw "Upload failed ($([int]$response.StatusCode)): $body"
  }
  return ($body | ConvertFrom-Json)
}

function Get-SignatureStatus([string]$token, [string]$userId, [string]$filePath) {
  try {
    Upload-SignatureFile $token $userId $filePath | Out-Null
    throw 'Expected upload failure'
  } catch {
    if ($_.Exception.Message -notlike '*Upload failed*') { throw }
    return $_.Exception.Message
  }
}

function SqlScalar([string]$q) {
  $query = 'SET NOCOUNT ON; ' + $q
  $out = & sqlcmd -S '.\SQLEXPRESS' -d ZoryaLMS -E -h -1 -W -Q $query 2>&1
  if ($LASTEXITCODE -ne 0) { throw ('SQL failed: ' + $out) }
  $val = ($out | Where-Object { $_ -and $_.ToString().Trim() -ne '' } | Select-Object -First 1)
  if ($null -eq $val) { return '' }
  return $val.ToString().Trim()
}

$token = Get-Token
$h = HeadersFor $token

$doctorRoleId = SqlScalar "SELECT Id FROM AspNetRoles WHERE Name = 'Doctor'"
$techRoleId = SqlScalar "SELECT TOP 1 Id FROM AspNetRoles WHERE Name = 'Technician'"
$stamp = Get-Date -Format "yyyyMMddHHmmss"
$doctorEmail = "doctor.qa.$stamp@zorya.test"
$techEmail = "tech.qa.$stamp@zorya.test"

# POS-01 Create Doctor with designation
$createBody = @{
  email = $doctorEmail
  first_name = "QA"
  last_name = "Doctor"
  phone_number = ""
  doctor_designation = "Consultant Pathologist"
  roles = @(@{ id = $doctorRoleId; name = "Doctor"; isInRole = $true })
  applications = @()
} | ConvertTo-Json -Depth 5

$created = Invoke-RestMethod -Method Post -Uri "$baseApi/api/Users" -Headers $h -Body $createBody
if (-not $created.id) { throw "Create doctor failed" }
$doctorUserId = $created.id
Log "POS-01" "Create Doctor" "PASS" "UserId=$doctorUserId"

# NEG-01 Missing designation
Expect-Status {
  $bad = @{
    email = "bad.$stamp@zorya.test"
    first_name = "Bad"
    last_name = "Doctor"
    doctor_designation = "  "
    roles = @(@{ id = $doctorRoleId; name = "Doctor"; isInRole = $true })
  } | ConvertTo-Json -Depth 5
  Invoke-RestMethod -Method Post -Uri "$baseApi/api/Users" -Headers $h -Body $bad
} 412
Log "NEG-01" "Missing designation" "PASS" "412 returned"

# POS-02 Upload signature
$pngPath = Join-Path $env:TEMP "doctor-sig-$stamp.png"
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap 120, 40
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::White)
$g.DrawString("Dr QA", (New-Object System.Drawing.Font("Arial", 12)), [System.Drawing.Brushes]::Black, 10, 10)
$g.Dispose()
$bmp.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

$upload = Upload-SignatureFile $token $doctorUserId $pngPath
if (-not $upload.doctor_signature_path) { throw "Upload failed" }
$sigPath = $upload.doctor_signature_path
Log "POS-02" "Upload Signature" "PASS" $sigPath

# POS-03 Retrieve Doctor
$detail = Invoke-RestMethod -Method Get -Uri "$baseApi/api/Users/$doctorUserId" -Headers $h
if ($detail.doctor_designation -ne "Consultant Pathologist") { throw "Designation mismatch" }
if (-not $detail.doctor_signature_path) { throw "Signature path missing on GET" }
Log "POS-03" "Retrieve Doctor" "PASS" "designation and signature path present"

# POS-04 Get signature image
$img = Invoke-WebRequest -Uri "$baseApi/api/Users/$doctorUserId/DoctorSignature" -Headers @{ accesskey = "DXI800"; Authorization = ("Bearer " + $token) } -UseBasicParsing
if ($img.StatusCode -ne 200 -or $img.Headers["Content-Type"] -notlike "image/*") { throw "Signature image GET failed" }
Log "POS-04" "Get Signature Image" "PASS" $img.Headers["Content-Type"]

# POS-05 Replace signature
Start-Sleep -Milliseconds 200
$upload2 = Upload-SignatureFile $token $doctorUserId $pngPath
if ($upload2.doctor_signature_path -eq $sigPath) { throw "Expected new signature path after replace" }
Log "POS-05" "Replace Signature" "PASS" $upload2.doctor_signature_path

# POS-06 Edit Doctor designation
$editBody = @{
  id = $doctorUserId
  email = $doctorEmail
  first_name = "QA"
  last_name = "Doctor"
  phone_number = ""
  doctor_designation = "Senior Radiologist"
  locked = $false
  email_confirmed = $true
  is_blocked = $false
  roles = @(@{ id = $doctorRoleId; name = "Doctor"; isInRole = $true })
  applications = $detail.applications
} | ConvertTo-Json -Depth 6
Invoke-RestMethod -Method Put -Uri "$baseApi/api/Users/$doctorUserId" -Headers $h -Body $editBody | Out-Null
$dbDesignation = SqlScalar "SELECT DoctorDesignation FROM AspNetUsers WHERE Id = '$doctorUserId'"
if ($dbDesignation -ne "Senior Radiologist") { throw "DB designation not updated" }
Log "POS-06" "Edit Doctor" "PASS" "Senior Radiologist"

# POS-07 Create non-doctor unaffected
$techBody = @{
  email = $techEmail
  first_name = "QA"
  last_name = "Tech"
  roles = @(@{ id = $techRoleId; name = "Technician"; isInRole = $true })
} | ConvertTo-Json -Depth 5
$tech = Invoke-RestMethod -Method Post -Uri "$baseApi/api/Users" -Headers $h -Body $techBody
$dbTechDesig = SqlScalar "SELECT ISNULL(DoctorDesignation,'') FROM AspNetUsers WHERE Id = '$($tech.id)'"
if ($dbTechDesig) { throw "Technician should not have designation" }
Log "POS-07" "Non-doctor unaffected" "PASS" "Technician created"

# NEG-02 Invalid extension
$txtPath = Join-Path $env:TEMP "bad-$stamp.txt"
"not an image" | Set-Content $txtPath
$msg = Get-SignatureStatus $token $doctorUserId $txtPath
if ($msg -notlike '*PNG or JPG*') { throw "Expected invalid extension message: $msg" }
Log "NEG-02" "Invalid extension" "PASS" "Rejected"

# NEG-03 Large file
$bigPath = Join-Path $env:TEMP "big-$stamp.png"
$fs = [System.IO.File]::Create($bigPath)
$fs.SetLength(3MB)
$fs.Close()
$msg = Get-SignatureStatus $token $doctorUserId $bigPath
if ($msg -notlike '*2 MB*') { throw "Expected large file message: $msg" }
Log "NEG-03" "Large file" "PASS" "Rejected"

# NEG-04 Unauthorized upload
try {
  Upload-SignatureFile '' $doctorUserId $pngPath | Out-Null
  throw 'Expected unauthorized upload failure'
} catch {
  if ($_.Exception.Message -notlike '*401*' -and $_.Exception.Message -notlike '*Upload failed (401)*') {
    throw "Expected 401 unauthorized: $($_.Exception.Message)"
  }
}
Log "NEG-04" "Unauthorized upload" "PASS" "401 returned"

# NEG-05 Upload for non-doctor
$msg = Get-SignatureStatus $token $tech.id $pngPath
if ($msg -notlike '*Doctor signature can only be uploaded*') { throw "Expected non-doctor rejection: $msg" }
Log "NEG-05" "Upload for non-doctor" "PASS" "Rejected"

# POS-08 Delete doctor cleans up
Invoke-RestMethod -Method Delete -Uri "$baseApi/api/Users/$doctorUserId" -Headers $h | Out-Null
Invoke-RestMethod -Method Delete -Uri "$baseApi/api/Users/$($tech.id)" -Headers $h | Out-Null
Log "POS-08" "Delete Doctor" "PASS" "Deleted QA users"

$pass = ($results | Where-Object Status -eq 'PASS').Count
$fail = ($results | Where-Object Status -eq 'FAIL').Count
Write-Host ""
Write-Host "API QA Summary: PASS=$pass FAIL=$fail" -ForegroundColor Cyan
if ($fail -gt 0) { exit 1 }
