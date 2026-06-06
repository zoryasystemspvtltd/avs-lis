-- =============================================================================
-- Verify CRUD test data — run after SeedAllCrudTestData.sql
-- =============================================================================
USE AVSLIS;
GO

SET NOCOUNT ON;

PRINT '=== Table row counts (CRUD-prefixed rows) ===';

SELECT 'Department' AS [Table], COUNT(*) AS TotalRows,
       SUM(CASE WHEN Code LIKE N'CRUD%' THEN 1 ELSE 0 END) AS CrudRows
FROM Department
UNION ALL SELECT 'HISSpecimenMaster', COUNT(*), SUM(CASE WHEN Code LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM HISSpecimenMaster
UNION ALL SELECT 'UnitMaster', COUNT(*), SUM(CASE WHEN Code LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM UnitMaster
UNION ALL SELECT 'MethodMaster', COUNT(*), SUM(CASE WHEN Code LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM MethodMaster
UNION ALL SELECT 'SampleTypeMaster', COUNT(*), SUM(CASE WHEN Code LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM SampleTypeMaster
UNION ALL SELECT 'ContainerMaster', COUNT(*), SUM(CASE WHEN Code LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM ContainerMaster
UNION ALL SELECT 'TestGroupMaster', COUNT(*), SUM(CASE WHEN Code LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM TestGroupMaster
UNION ALL SELECT 'TestCategoryMaster', COUNT(*), SUM(CASE WHEN Code LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM TestCategoryMaster
UNION ALL SELECT 'ReferralDoctorMaster', COUNT(*), SUM(CASE WHEN Code LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM ReferralDoctorMaster
UNION ALL SELECT 'CorporateMaster', COUNT(*), SUM(CASE WHEN Code LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM CorporateMaster
UNION ALL SELECT 'EquipmentMaster', COUNT(*), SUM(CASE WHEN AccessKey LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM EquipmentMaster
UNION ALL SELECT 'EquipmentHeartBeat', COUNT(*), SUM(CASE WHEN AccessKey LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM EquipmentHeartBeat
UNION ALL SELECT 'HISTestMaster', COUNT(*), SUM(CASE WHEN HISTestCode LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM HISTestMaster
UNION ALL SELECT 'HISParameterMaster', COUNT(*), SUM(CASE WHEN HISTestCode LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM HISParameterMaster
UNION ALL SELECT 'HISParameterRangMaster', COUNT(*),
       (SELECT COUNT(*) FROM HISParameterRangMaster r INNER JOIN HISParameterMaster p ON p.Id = r.HisParameterId WHERE p.HISTestCode LIKE N'CRUD%')
UNION ALL SELECT 'TestMappingMaster', COUNT(*), SUM(CASE WHEN HISTestCode LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM TestMappingMaster
UNION ALL SELECT 'TestProfileMaster', COUNT(*), SUM(CASE WHEN Code LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM TestProfileMaster
UNION ALL SELECT 'TestProfileDetail', COUNT(*),
       (SELECT COUNT(*) FROM TestProfileDetail d INNER JOIN TestProfileMaster p ON p.Id = d.TestProfileId WHERE p.Code LIKE N'CRUD%')
UNION ALL SELECT 'TestRateMaster', COUNT(*),
       (SELECT COUNT(*) FROM TestRateMaster tr INNER JOIN HISTestMaster t ON t.Id = tr.TestId WHERE t.HISTestCode LIKE N'CRUD%')
UNION ALL SELECT 'PatientDetails', COUNT(*), SUM(CASE WHEN HisPatientId LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM PatientDetails
UNION ALL SELECT 'TestRequestDetails', COUNT(*), SUM(CASE WHEN SampleNo LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM TestRequestDetails
UNION ALL SELECT 'TestParameters', COUNT(*), SUM(CASE WHEN HISTestCode LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM TestParameters
UNION ALL SELECT 'SaleInvoice', COUNT(*), SUM(CASE WHEN InvoiceNo LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM SaleInvoice
UNION ALL SELECT 'SaleInvoiceDetail', COUNT(*),
       (SELECT COUNT(*) FROM SaleInvoiceDetail d INNER JOIN SaleInvoice i ON i.Id = d.SaleInvoiceId WHERE i.InvoiceNo LIKE N'CRUD%')
UNION ALL SELECT 'TestResults', COUNT(*), SUM(CASE WHEN SampleNo LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM TestResults
UNION ALL SELECT 'TestResultDetails', COUNT(*),
       (SELECT COUNT(*) FROM TestResultDetails d INNER JOIN TestResults r ON r.Id = d.TestResultId WHERE r.SampleNo LIKE N'CRUD%')
UNION ALL SELECT 'ControlResults', COUNT(*), SUM(CASE WHEN SampleNo LIKE N'CRUD%' THEN 1 ELSE 0 END) FROM ControlResults
UNION ALL SELECT 'ControlResultDetails', COUNT(*),
       (SELECT COUNT(*) FROM ControlResultDetails d INNER JOIN ControlResults c ON c.Id = d.ControlResultId WHERE c.SampleNo LIKE N'CRUD%');

PRINT '';
PRINT '=== CRUD workflow samples (approval status) ===';

SELECT SampleNo, HISTestCode, ReportStatus,
       CASE ReportStatus
           WHEN 0 THEN 'New'
           WHEN 2 THEN 'Report Generated'
           WHEN 3 THEN 'Technician Approved'
           WHEN 5 THEN 'Doctor Approved'
           ELSE CAST(ReportStatus AS NVARCHAR(10))
       END AS StatusLabel,
       CASE WHEN EXISTS (SELECT 1 FROM TestResults tr WHERE tr.TestRequestId = trd.Id) THEN 1 ELSE 0 END AS HasResults
FROM TestRequestDetails trd
WHERE SampleNo LIKE N'CRUD-SMP%'
ORDER BY SampleNo;

PRINT '';
PRINT '=== Expected minimum CRUD rows (CrudRows >= 1) ===';
GO
