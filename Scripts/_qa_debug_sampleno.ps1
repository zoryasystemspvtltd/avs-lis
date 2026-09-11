$Cs = "Server=.\SQLEXPRESS;Database=ZoryaLMS;Trusted_Connection=True;TrustServerCertificate=True"
$cn = New-Object System.Data.SqlClient.SqlConnection $Cs
$cn.Open()
$cmd = $cn.CreateCommand()
$cmd.CommandText = @"
SELECT TOP 20 Id, HISRequestNo, HISRequestId, HISTestCode, SpecimenCode, SampleNo
FROM TestRequestDetails
WHERE HISRequestNo LIKE N'INV-20260912%'
ORDER BY Id DESC
"@
$r = $cmd.ExecuteReader()
$i = 0
while ($r.Read()) {
  $i++
  Write-Output ("ROW $i | Id=$($r['Id']) | No=$($r['HISRequestNo']) | Test=$($r['HISTestCode']) | Spec=$($r['SpecimenCode']) | Sample=$($r['SampleNo'])")
}
$r.Close()
Write-Output "TOTAL_READ=$i"
$cmd.CommandText = "SELECT COUNT(*) FROM SaleInvoiceDetail WHERE SaleInvoiceId IN (1953,1955)"
Write-Output ("SaleInvoiceDetailCount=" + $cmd.ExecuteScalar())
$cn.Close()
