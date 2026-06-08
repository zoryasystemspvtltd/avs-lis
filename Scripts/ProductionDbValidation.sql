-- AVILIS Production DB validation queries
USE AVSLIS;
SET NOCOUNT ON;

PRINT '=== SERUM Specimen ===';
SELECT Id, Code, Name, IsActive FROM HISSpecimenMaster WHERE Code = 'SERUM';

PRINT '=== SaleInvoice IsActive (recent) ===';
SELECT TOP 10 Id, InvoiceNo, IsActive, InvoiceStatus, CreatedOn
FROM SaleInvoice ORDER BY Id DESC;

PRINT '=== SaleInvoice inactive non-cancelled (should be 0) ===';
SELECT COUNT(*) AS InactiveNonCancelled
FROM SaleInvoice WHERE IsActive = 0 AND InvoiceStatus <> 3;

PRINT '=== TestRate overlapping active CBC (legacy check) ===';
SELECT tr.Id, ht.HISTestCode, tr.RateType, tr.EffectiveStart, tr.EffectiveEnd, tr.IsActive
FROM TestRateMaster tr
JOIN HisTestMaster ht ON ht.Id = tr.TestId
WHERE ht.HISTestCode = 'CBC' AND tr.IsActive = 1
ORDER BY tr.EffectiveStart;

PRINT '=== TestMapping duplicates (active) ===';
SELECT EquipmentId, HISTestCode, LISTestCode, COUNT(*) AS Cnt
FROM TestMappingMaster WHERE IsActive = 1
GROUP BY EquipmentId, HISTestCode, LISTestCode
HAVING COUNT(*) > 1;

PRINT '=== Patient duplicates (HisPatientId) ===';
SELECT HisPatientId, COUNT(*) AS Cnt FROM PatientDetails WHERE IsActive = 1 AND HisPatientId IS NOT NULL AND HisPatientId <> ''
GROUP BY HisPatientId HAVING COUNT(*) > 1;

PRINT '=== HisTest duplicate codes (active) ===';
SELECT HISTestCode, COUNT(*) AS Cnt FROM HisTestMaster WHERE IsActive = 1
GROUP BY HISTestCode HAVING COUNT(*) > 1;

PRINT '=== Approval status sample (ReportGenerated+) ===';
SELECT TOP 5 Id, SampleNo, ReportStatus FROM TestRequestDetails WHERE ReportStatus >= 2 ORDER BY Id DESC;

PRINT '=== Test Profile detail orphans ===';
SELECT COUNT(*) AS OrphanProfileDetails
FROM TestProfileDetail d
LEFT JOIN TestProfileMaster p ON p.Id = d.TestProfileId
WHERE p.Id IS NULL;
