$ErrorActionPreference = "Stop"

function Get-Token {
  $body = "grant_type=password&username=admin%40zorya.co.in&password=zorKol%401"
  $r = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
  if (-not $r -or -not $r.access_token) { throw "Token fetch failed." }
  return $r.access_token
}

function ApiHeaders([string]$token) {
  return @{
    accesskey = "DXI800"
    Authorization = ("Bearer " + $token)
  }
}

$token = Get-Token
$headers = ApiHeaders $token

$code = ("MZ" + (Get-Random -Maximum 9999).ToString("0000"))
$create = @{ code = $code; name = ("Method " + $code); isActive = $true } | ConvertTo-Json

Write-Host "Creating Method: $code"
$createResp = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/Method" -Headers $headers -ContentType "application/json" -Body $create
Write-Host ("Create status: " + $createResp.StatusCode)
Write-Host ("Create body: " + $createResp.Content)

Write-Host "List (GetAll) contains code?"
$all = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/Method/GetAll" -Headers $headers
$found = $false
foreach ($m in ($all | ForEach-Object { $_ })) {
  if (($m.Code -eq $code) -or ($m.code -eq $code)) { $found = $true; break }
}
Write-Host ("Found in GetAll: " + $found)
if (-not $found) { throw "Created method not found in GetAll." }

Write-Host "Fetching paged list to find Id..."
$apiOption = ('{"RecordPerPage":50,"CurrentPage":1,"SortColumnName":"Code","SortDirection":false,"SearchText":"' + $code + '"}')
$pagedHeaders = @{
  accesskey = $headers.accesskey
  Authorization = $headers.Authorization
  ApiOption = $apiOption
}
$paged = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/Method/" -Headers $pagedHeaders
$items = @()
if ($paged.Items) { $items = $paged.Items } elseif ($paged.items) { $items = $paged.items } else { $items = @() }
$id = ($items | Select-Object -First 1).Id
if (-not $id) { $id = ($items | Select-Object -First 1).id }
Write-Host ("Found Id: " + $id)
if (-not $id) { throw "Unable to locate created method id." }

Write-Host "Updating Method name..."
$update = @{ id = [int]$id; code = $code; name = ("Method UPDATED " + $code); isActive = $true } | ConvertTo-Json
$updResp = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/Method/Put" -Headers $headers -ContentType "application/json" -Body $update
Write-Host ("Update status: " + $updResp.StatusCode)

Write-Host "Deactivating Method..."
$del = @{ id = [int]$id } | ConvertTo-Json
$delResp = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "http://localhost:8081/api/Method/Delete" -Headers $headers -ContentType "application/json" -Body $del
Write-Host ("Delete status: " + $delResp.StatusCode)

Write-Host "SmokeMethodCrud PASSED"

