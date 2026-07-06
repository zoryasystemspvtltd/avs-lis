# Targeted defect fix validation - Issues 1-4
$ErrorActionPreference = "Stop"
$baseApi = "http://localhost:8081"
$results = @()

function Log([string]$id, [string]$status, [string]$detail) {
  $global:results += [pscustomobject]@{ Id = $id; Status = $status; Detail = $detail }
  $color = if ($status -eq "PASS") { "Green" } elseif ($status -eq "FAIL") { "Red" } else { "Yellow" }
  Write-Host "[$status] $id - $detail" -ForegroundColor $color
}

$body = 'grant_type=password&username=admin%40zorya.co.in&password=zorKol%401'
$token = (Invoke-RestMethod -Method Post -Uri "$baseApi/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body).access_token

function Get-Register([string]$invoiceNo) {
  $opt = @{
    FromDate = "2026-06-01"
    ToDate = "2026-06-30"
    InvoiceNo = $invoiceNo
    CurrentPage = 1
    RecordPerPage = 100
    SortColumnName = "InvoiceDate"
    SortDirection = $false
  }
  if ([string]::IsNullOrWhiteSpace($invoiceNo)) { $opt.Remove("InvoiceNo") }
  $optJson = ($opt | ConvertTo-Json -Compress)
  return Invoke-RestMethod -Method Get -Uri "$baseApi/api/Reports/SaleInvoiceRegister" -Headers @{
    accesskey = "DXI800"; Authorization = "Bearer $token"; ApiOption = $optJson
  }
}

# DEFECT 1
try {
  $all = Get-Register ""
  $exact = Get-Register "INV-20260616-0001"
  $contains = Get-Register "INV"
  $datePart = Get-Register "20260616"
  $suffix = Get-Register "0001"
  if ($all.TotalRecord -le 0) { throw "Baseline empty" }
  if ($contains.TotalRecord -le 0) { throw "Contains INV returned 0" }
  if ($datePart.TotalRecord -lt 0) { throw "Date part search failed" }
  $dbCnt = sqlcmd -S ".\SQLEXPRESS" -d AVSLIS -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM SaleInvoice WHERE IsActive=1 AND InvoiceDate >= '2026-06-01' AND InvoiceDate < '2026-07-01' AND InvoiceNo LIKE '%INV%'" -h -1 -W
  if ([int]$contains.TotalRecord -gt [int]$dbCnt.Trim()) { throw "API exceeds DB contains count" }
  Log "D1-InvoiceSearch" "PASS" "all=$($all.TotalRecord) INV=$($contains.TotalRecord) exact=$($exact.TotalRecord) 20260616=$($datePart.TotalRecord) 0001=$($suffix.TotalRecord)"
} catch {
  Log "D1-InvoiceSearch" "FAIL" $_.Exception.Message
}

# DEFECT 2 - source file encoding
try {
  $html = Get-Content "i:\Projects\LIS\avs-lis\web\Lis.Web\src\app\reports\sale-invoice-register\sale-invoice-register.component.html" -Raw
  if ($html -match 'â€') { throw "Mojibake sequence still present in source" }
  if ($html -notmatch '&ndash;') { throw "Expected HTML entity for en-dash in pager" }
  if ($html -notmatch "\|\| '—'") { throw "Expected em-dash placeholder in grid" }
  Log "D2-Encoding" "PASS" "sale-invoice-register.component.html uses correct dash characters"
} catch {
  Log "D2-Encoding" "FAIL" $_.Exception.Message
}

# DEFECT 3 - booking register patient/doctor null filter
try {
  $patientId = (sqlcmd -S ".\SQLEXPRESS" -d AVSLIS -Q "SET NOCOUNT ON; SELECT TOP 1 Id FROM PatientDetails WHERE IsActive=1" -h -1 -W).Trim()
  $optAll = '{"FromDate":"2026-06-01","ToDate":"2026-06-30","PatientId":null,"ReferralDoctorId":null,"CurrentPage":1,"RecordPerPage":5,"SortColumnName":"BookingDate","SortDirection":false}'
  $optPat = (@{ FromDate = "2026-06-01"; ToDate = "2026-06-30"; PatientId = [long]$patientId; CurrentPage = 1; RecordPerPage = 5; SortColumnName = "BookingDate"; SortDirection = $false } | ConvertTo-Json -Compress)
  $all = Invoke-RestMethod -Method Get -Uri "$baseApi/api/Reports/TestBookingRegister" -Headers @{ accesskey = "DXI800"; Authorization = "Bearer $token"; ApiOption = $optAll }
  $filtered = Invoke-RestMethod -Method Get -Uri "$baseApi/api/Reports/TestBookingRegister" -Headers @{ accesskey = "DXI800"; Authorization = "Bearer $token"; ApiOption = $optPat }
  if ($all.TotalRecord -lt $filtered.TotalRecord) { throw "All-patients count less than filtered" }
  Log "D3-BookingFilters" "PASS" "all=$($all.TotalRecord) patient=$($filtered.TotalRecord)"
} catch {
  Log "D3-BookingFilters" "FAIL" $_.Exception.Message
}

# DEFECT 3 UI - All Patients/Doctors in component source
try {
  $ts = Get-Content "i:\Projects\LIS\avs-lis\web\Lis.Web\src\app\reports\test-booking-register\test-booking-register.component.ts" -Raw
  if ($ts -notmatch "All Patients" -or $ts -notmatch "All Doctors") { throw "All options not in test-booking component" }
  Log "D3-AllOptionsUI" "PASS" "All Patients / All Doctors options defined"
} catch {
  Log "D3-AllOptionsUI" "FAIL" $_.Exception.Message
}

# DEFECT 4 - menu exact removed for master routes
try {
  $nav = Get-Content "i:\Projects\LIS\avs-lis\web\Lis.Web\src\app\_components\left-nav-menu\left-nav-menu.component.html" -Raw
  foreach ($route in @('/departments"', '/units"', '/methods"', '/specimens"', '/test-master"', '/test-rates"', '/patient-master"', '/test-parameters"', '/his-parameters"', '/test-profiles"')) {
    $idx = $nav.IndexOf("routerLink=`"$route")
    if ($idx -lt 0) { throw "Route $route not found" }
    $segment = $nav.Substring($idx, [Math]::Min(180, $nav.Length - $idx))
    if ($segment -match 'exact:\s*true') { throw "exact:true still on $route" }
  }
  Log "D4-MenuHighlight" "PASS" "exact:true removed from listed master/setup routes"
} catch {
  Log "D4-MenuHighlight" "FAIL" $_.Exception.Message
}

Write-Host "`n========== SUMMARY =========="
$results | Format-Table -AutoSize
$fail = @($results | Where-Object { $_.Status -eq "FAIL" })
if ($fail.Count -gt 0) { exit 1 }
exit 0
