# Ensures AVILIS portal can reach the API via same-origin /lis/ (environment.prod ApplicationServer).
# Without this IIS application, master lists fail with "Unable to load records. Check API connection and permissions."

param(
    [string]$PortalSite = 'AVILIS_PORTAL',
    [string]$ApiPhysicalPath = 'I:\Projects\PROD\AVILIS\API',
    [string]$ApiAppPool = 'AVILIS_API_POOL',
    [string]$PortalBaseUrl = 'http://localhost:8080'
)

$ErrorActionPreference = 'Stop'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
if (-not (Test-Path $appcmd)) {
    throw "IIS appcmd not found. Run on a machine with IIS."
}

$lisApp = & $appcmd list app "/site.name:$PortalSite" 2>$null | Select-String 'path:"/lis"'
if (-not $lisApp) {
    Write-Host "Adding /lis application on $PortalSite -> $ApiPhysicalPath"
    & $appcmd add app "/site.name:$PortalSite" /path:/lis /physicalPath:$ApiPhysicalPath | Out-Null
    & $appcmd set app "$PortalSite/lis" "/applicationPool:$ApiAppPool" | Out-Null
} else {
    Write-Host "/lis application already exists on $PortalSite"
    & $appcmd set vdir "$PortalSite/lis/" /physicalPath:$ApiPhysicalPath | Out-Null
    & $appcmd set app "$PortalSite/lis" "/applicationPool:$ApiAppPool" | Out-Null
}

$headers = @{
    ApiOption = '{"RecordPerPage":10,"CurrentPage":1,"SortColumnName":"Code","SortDirection":true}'
}
$testUrl = "$PortalBaseUrl/lis/api/Department/"
try {
    $r = Invoke-WebRequest -Uri $testUrl -Headers $headers -UseBasicParsing -TimeoutSec 30
    Write-Host "OK: $testUrl -> $($r.StatusCode) ($($r.Content.Length) bytes)"
} catch {
    throw "Bridge verification failed for $testUrl : $($_.Exception.Message)"
}

Write-Host 'Portal /lis API bridge is configured.'
