$cs = 'Server=.\SQLEXPRESS;Database=ZoryaLMS;Trusted_Connection=True;TrustServerCertificate=True'
$cn = New-Object System.Data.SqlClient.SqlConnection $cs
$cn.Open()
$cmd = $cn.CreateCommand()
$cmd.CommandText = "SELECT name FROM sys.tables WHERE name LIKE '%Sample%' OR name LIKE '%Request%' OR name LIKE '%Invoice%' ORDER BY name"
$r = $cmd.ExecuteReader()
while ($r.Read()) { Write-Output $r.GetString(0) }
$r.Close()
$cmd.CommandText = "SELECT IsEnabled, SmsEnabled, WhatsAppEnabled FROM NotificationConfiguration"
$r2 = $cmd.ExecuteReader()
while ($r2.Read()) { Write-Output ("Notif IsEnabled=" + $r2.GetValue(0) + " Sms=" + $r2.GetValue(1) + " Wa=" + $r2.GetValue(2)) }
$r2.Close()
$cn.Close()
