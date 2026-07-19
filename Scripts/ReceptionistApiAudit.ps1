# API authorization audit for the Receptionist role.
# Positive: granted module APIs return 200. Negative: ungranted module APIs return 401/403.
$ErrorActionPreference = "Continue"
$baseApi = "http://localhost:8080/lis"

$body = 'grant_type=password&username=recep1%40zorya.co.in&password=zorKol%401'
$tok = (Invoke-RestMethod -Method Post -Uri "$baseApi/TOKEN" -Headers @{ accesskey = "DXI800" } -ContentType "application/x-www-form-urlencoded" -Body $body).access_token
$h = @{ accesskey = "DXI800"; Authorization = "Bearer $tok" }
$opt = '{"CurrentPage":1,"RecordPerPage":5,"SortColumnName":"Name","SortDirection":false}'

function Probe([string]$name, [string]$method, [string]$url, $payload) {
  try {
    if ($method -eq "GET") {
      $null = Invoke-WebRequest -Method Get -Uri $url -Headers ($h + @{ ApiOption = $opt }) -UseBasicParsing
      $code = 200
    } else {
      $null = Invoke-WebRequest -Method $method -Uri $url -Headers $h -ContentType "application/json" -Body $payload -UseBasicParsing
      $code = 200
    }
  } catch {
    $code = 0
    if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
  }
  "{0,-55} {1,-6} HTTP {2}" -f $name, $method, $code
}

"== POSITIVE (granted) =="
Probe "GET /api/HisTest (Test Master)"            GET  "$baseApi/api/HisTest"
Probe "GET /api/TestRate (Test Rate)"             GET  "$baseApi/api/TestRate"
Probe "GET /api/ReferralDoctor"                   GET  "$baseApi/api/ReferralDoctor"
Probe "GET /api/Corporate"                        GET  "$baseApi/api/Corporate"
Probe "GET /api/PatientMaster"                    GET  "$baseApi/api/PatientMaster"
Probe "GET /api/SaleInvoice"                      GET  "$baseApi/api/SaleInvoice"
""
"== NEGATIVE (not granted; expect 401/403) =="
Probe "POST /api/Department (menu-enforced)"      POST "$baseApi/api/Department" '{"code":"QAX1","name":"QA Denied Dept"}'
Probe "POST /api/Specimens (menu-enforced)"       POST "$baseApi/api/Specimens" '{"code":"QAX1","name":"QA Denied Spec"}'
Probe "GET /api/Users"                            GET  "$baseApi/api/Users"
Probe "GET /api/Roles/e41f02d9-e6dc-4848-b182-985a912b49e6 (role detail)" GET "$baseApi/api/Roles/e41f02d9-e6dc-4848-b182-985a912b49e6"
Probe "GET /api/Equipment"                        GET  "$baseApi/api/Equipment"
Probe "GET /api/RawSamples (Samples)"             GET  "$baseApi/api/RawSamples"
