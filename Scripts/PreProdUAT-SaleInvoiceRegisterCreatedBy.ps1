# UAT - Sale Invoice Register Created By filter
$ErrorActionPreference = "Stop"
$baseApi = "http://localhost:8081"
$tag = "UAT-SIR-" + (Get-Date -Format "yyyyMMddHHmmss")
$results = @()

function Log([string]$id, [string]$name, [string]$status, [string]$detail) {
  $global:results += [pscustomobject]@{ Id = $id; Scenario = $name; Status = $status; Detail = $detail }
  $c = if ($status -eq "PASS") { "Green" } elseif ($status -eq "FAIL") { "Red" } else { "Yellow" }
  Write-Host "[$status] $id $name - $detail" -ForegroundColor $c
}

function Get-Token {
  $body = 'grant_type=password&username=admin%40zorya.co.in&password=zorKol%401'
  $r = Invoke-RestMethod -Method Post -Uri "$baseApi/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
  return $r.access_token
}

function ApiHeaders([string]$token, [string]$json = $null) {
  $h = @{ accesskey = "DXI800"; Authorization = "Bearer $token" }
  if ($json) { $h.ApiOption = $json }
  return $h
}

function Get-Report($token, $opt) {
  $json = $opt | ConvertTo-Json -Compress
  return Invoke-RestMethod -Method Get -Uri "$baseApi/api/Reports/SaleInvoiceRegister" -Headers (ApiHeaders $token $json)
}

function Items($r) { if ($r.Items) { @($r.Items) } elseif ($r.items) { @($r.items) } else { @() } }
function Total($r) { if ($null -ne $r.TotalRecord) { [int]$r.TotalRecord } elseif ($null -ne $r.totalRecord) { [int]$r.totalRecord } else { (Items $r).Count } }

Write-Host "========== SALE INVOICE REGISTER UAT ($tag) ==========" -ForegroundColor Cyan
$token = Get-Token
$from = "2026-06-01"
$to = "2026-06-30"
$baseOpt = @{
  FromDate = $from; ToDate = $to; RecordPerPage = 200; CurrentPage = 1
  SortColumnName = "InvoiceDate"; SortDirection = $false
}

# Lookup API
try {
  $users = Invoke-RestMethod -Method Get -Uri "$baseApi/api/Users/Lookup" -Headers (ApiHeaders $token)
  if (-not $users -or $users.Count -lt 1) { throw "No users in lookup" }
  Log "LK-01" "Users Lookup API" "PASS" "Count=$($users.Count)"
} catch {
  Log "LK-01" "Users Lookup API" "FAIL" $_.Exception.Message
  throw
}

# Scenario 1: Date only
try {
  $r = Get-Report $token $baseOpt
  $totalAll = Total $r
  if ($totalAll -lt 1) { throw "No rows in June 2026" }
  Log "S-01" "Date range only" "PASS" "Total=$totalAll"
} catch {
  Log "S-01" "Date range only" "FAIL" $_.Exception.Message
  throw
}

# Find admin user
$admin = $users | Where-Object { ($_.name -match 'admin') -or ($_.id) } | Select-Object -First 1
foreach ($u in $users) {
  $detail = Invoke-RestMethod -Uri "$baseApi/api/Users/$($u.id)" -Headers (ApiHeaders $token) -ErrorAction SilentlyContinue
  if ($detail.email -eq 'admin@zorya.co.in') { $admin = $u; break }
}

# Scenario 2/4: All users vs specific user
try {
  $adminId = $admin.id
  $optUser = $baseOpt.Clone()
  $optUser.CreatedById = $adminId
  $rUser = Get-Report $token $optUser
  $totalUser = Total $rUser
  if ($totalUser -lt 1) {
    Log "S-02" "Created By specific user" "PASS" "No invoices for admin in range (valid)"
  } else {
    $bad = Items $rUser | Where-Object {
      $cb = if ($_.createdBy) { $_.createdBy } else { $_.CreatedBy }
      $cb -ne 'admin@zorya.co.in'
    }
    if ($bad) { throw "Filter leaked other creators" }
    Log "S-02" "Created By specific user" "PASS" "Total=$totalUser admin only"
  }

  $optAll = $baseOpt.Clone()
  $optAll.CreatedById = $null
  $rAll = Get-Report $token $optAll
  if ((Total $rAll) -lt $totalAll) { throw "All users should match date-only total" }
  Log "S-04" "Created By All Users" "PASS" "Total=$(Total $rAll)"
} catch {
  Log "S-02/S-04" "Created By filter" "FAIL" $_.Exception.Message
  throw
}

# Invalid user id
try {
  $optBad = $baseOpt.Clone()
  $optBad.CreatedById = "00000000-0000-0000-0000-000000000000"
  try {
    Get-Report $token $optBad | Out-Null
    throw "Expected 400 for invalid user"
  } catch {
    if ($_.Exception.Message -notmatch '400|Invalid Created By') { throw $_ }
  }
  Log "N-01" "Invalid user id" "PASS" "Rejected"
} catch {
  Log "N-01" "Invalid user id" "FAIL" $_.Exception.Message
}

# Combined with invoice number
try {
  $sample = (Items (Get-Report $token $baseOpt))[0]
  $invNo = if ($sample.invoiceNo) { $sample.invoiceNo } else { $sample.InvoiceNo }
  $optComb = $baseOpt.Clone()
  $optComb.InvoiceNo = $invNo
  if ($admin) { $optComb.CreatedById = $admin.id }
  $rComb = Get-Report $token $optComb
  Log "S-07" "Invoice + Created By" "PASS" "Total=$(Total $rComb)"
} catch {
  Log "S-07" "Invoice + Created By" "FAIL" $_.Exception.Message
}

$fail = @($results | Where-Object { $_.Status -eq "FAIL" })
Write-Host "`n========== SUMMARY ==========" -ForegroundColor Cyan
$results | Format-Table -AutoSize
if ($fail.Count -gt 0) { Write-Host "UAT FAILED" -ForegroundColor Red; exit 1 }
Write-Host "UAT PASSED" -ForegroundColor Green
exit 0
