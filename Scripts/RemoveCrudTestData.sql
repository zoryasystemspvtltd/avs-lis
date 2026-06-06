-- =============================================================================
-- Remove CRUD-prefixed test data (reverse dependency order)
-- Safe: only deletes rows matching CRUD-* identifiers
-- =============================================================================
USE AVSLIS;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DELETE d FROM TestResultDetails d
INNER JOIN TestResults r ON r.Id = d.TestResultId
WHERE r.SampleNo LIKE N'CRUD%';

DELETE FROM TestResults WHERE SampleNo LIKE N'CRUD%';

DELETE d FROM ControlResultDetails d
INNER JOIN ControlResults c ON c.Id = d.ControlResultId
WHERE c.SampleNo LIKE N'CRUD%';

DELETE FROM ControlResults WHERE SampleNo LIKE N'CRUD%';

DELETE FROM TestParameters WHERE HISTestCode LIKE N'CRUD%';

DELETE FROM SaleInvoiceDetail
WHERE SaleInvoiceId IN (SELECT Id FROM SaleInvoice WHERE InvoiceNo LIKE N'CRUD%');

DELETE FROM SaleInvoice WHERE InvoiceNo LIKE N'CRUD%';

DELETE FROM TestRequestDetails WHERE SampleNo LIKE N'CRUD%';

DELETE FROM PatientDetails WHERE HisPatientId LIKE N'CRUD%';

DELETE FROM TestRateMaster WHERE TestId IN (SELECT Id FROM HISTestMaster WHERE HISTestCode LIKE N'CRUD%');

DELETE d FROM TestProfileDetail d
INNER JOIN TestProfileMaster p ON p.Id = d.TestProfileId
WHERE p.Code LIKE N'CRUD%';

DELETE FROM TestProfileMaster WHERE Code LIKE N'CRUD%';

DELETE FROM TestMappingMaster WHERE HISTestCode LIKE N'CRUD%';

DELETE FROM HISParameterRangMaster WHERE HisParameterId IN (
    SELECT Id FROM HISParameterMaster WHERE HISTestCode LIKE N'CRUD%'
);

DELETE FROM HISParameterMaster WHERE HISTestCode LIKE N'CRUD%';

DELETE FROM HISTestMaster WHERE HISTestCode LIKE N'CRUD%';

DELETE FROM EquipmentHeartBeat WHERE AccessKey LIKE N'CRUD%';

DELETE FROM EquipmentMaster WHERE AccessKey LIKE N'CRUD%';

DELETE FROM CorporateMaster WHERE Code LIKE N'CRUD%';
DELETE FROM ReferralDoctorMaster WHERE Code LIKE N'CRUD%';
DELETE FROM TestCategoryMaster WHERE Code LIKE N'CRUD%';
DELETE FROM TestGroupMaster WHERE Code LIKE N'CRUD%';
DELETE FROM ContainerMaster WHERE Code LIKE N'CRUD%';
DELETE FROM SampleTypeMaster WHERE Code LIKE N'CRUD%';
DELETE FROM MethodMaster WHERE Code LIKE N'CRUD%';
DELETE FROM UnitMaster WHERE Code LIKE N'CRUD%';
DELETE FROM HISSpecimenMaster WHERE Code LIKE N'CRUD%';
DELETE FROM Department WHERE Code LIKE N'CRUD%';

COMMIT TRANSACTION;

PRINT 'CRUD test data removed.';
GO
