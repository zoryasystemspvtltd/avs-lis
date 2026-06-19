$ErrorActionPreference = "Stop"

function Get-Token {
  $body = "grant_type=password&username=admin%40zorya.co.in&password=zorKol%401"
  $r = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body
  return $r.access_token
}

function HeadersFor([string]$token, [string]$apiOptionJson = $null) {
  $h = @{ accesskey = "DXI800"; Authorization = ("Bearer " + $token) }
  if ($apiOptionJson) { $h.ApiOption = $apiOptionJson }
  return $h
}

$token = Get-Token
Write-Host "FDD Smoke: Token OK"

$opt = '{"RecordPerPage":10,"CurrentPage":1,"SortColumnName":"SampleCollectionDate","SortDirection":false}'
$queue = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/SampleCollection/PendingQueue" -Headers (HeadersFor $token $opt)
Write-Host "Sample Collection queue: $($queue.TotalRecord) rows"

$recv = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/SampleReceiving/Queue" -Headers (HeadersFor $token $opt)
Write-Host "Sample Receiving queue: $($recv.TotalRecord) rows"

$reasons = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/SampleReceiving/RejectionReasons" -Headers (HeadersFor $token)
if ($reasons.Count -lt 6) { throw "Expected 6 rejection reasons, got $($reasons.Count)" }
Write-Host "Rejection reasons: $($reasons.Count) seeded"

$rad = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/RadiologyReport/PendingQueue" -Headers (HeadersFor $token $opt)
Write-Host "Radiology queue: $($rad.TotalRecord) rows"

$reportOpt = '{"FromDate":"2026-01-01","ToDate":"2026-12-31","RecordPerPage":5,"CurrentPage":1,"SortColumnName":"CollectionDate","SortDirection":false}'
$summary = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/Reports/CollectionSummary" -Headers (HeadersFor $token $reportOpt)
Write-Host "Collection Summary report: $($summary.TotalRecord) rows"

$pending = Invoke-RestMethod -Method Get -Uri "http://localhost:8081/api/Reports/PendingCollection" -Headers (HeadersFor $token $reportOpt)
Write-Host "Pending Collection report: $($pending.TotalRecord) rows"

Write-Host "FDD SMOKE: PASS"
