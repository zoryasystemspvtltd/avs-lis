-- Populate test results for Lab No / Invoice INV-20260519-0002 (AFP test)
-- Safe to re-run: removes prior results for this test request before insert.
USE AVSLIS;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @InvoiceNo NVARCHAR(30) = N'INV-20260519-0002';
DECLARE @User NVARCHAR(80) = N'admin@zorya.co.in';
DECLARE @Now DATETIME = GETDATE();

DECLARE @RequestId BIGINT;
DECLARE @PatientId BIGINT;
DECLARE @SampleNo NVARCHAR(50);
DECLARE @HISTestCode NVARCHAR(20) = N'T0000001';
DECLARE @LISTestCode NVARCHAR(20) = N'AFP';
DECLARE @EquipmentId INT = 1;
DECLARE @TestResultId BIGINT;

SELECT
    @RequestId = trd.Id,
    @PatientId = trd.PatientId,
    @SampleNo = trd.SampleNo
FROM TestRequestDetails trd
WHERE trd.HISRequestNo = @InvoiceNo
  AND trd.HISTestCode = @HISTestCode;

IF @RequestId IS NULL
BEGIN
    RAISERROR('Test request not found for %s / %s', 16, 1, @InvoiceNo, @HISTestCode);
    RETURN;
END

BEGIN TRANSACTION;

-- Remove existing results for this request (re-run safe)
DELETE trd
FROM TestResultDetails trd
INNER JOIN TestResults tr ON tr.Id = trd.TestResultId
WHERE tr.TestRequestId = @RequestId;

DELETE FROM TestResults WHERE TestRequestId = @RequestId;

INSERT INTO TestResults (
    SampleNo, HISTestCode, LISTestCode, SpecimenCode, SpecimenName,
    ResultDate, SampleCollectionDate, SampleReceivedDate,
    AuthorizationDate, AuthorizedBy, ReviewDate, ReviewedBy,
    CreatedBy, CreatedOn, PatientId, TestRequestId, EquipmentId
)
SELECT
    trd.SampleNo,
    trd.HISTestCode,
    @LISTestCode,
    trd.SpecimenCode,
    trd.SpecimenName,
    @Now,
    trd.SampleCollectionDate,
    trd.SampleReceivedDate,
    @Now,
    @User,
    @Now,
    @User,
    @User,
    @Now,
    trd.PatientId,
    trd.Id,
    @EquipmentId
FROM TestRequestDetails trd
WHERE trd.Id = @RequestId;

SET @TestResultId = SCOPE_IDENTITY();

INSERT INTO TestResultDetails (
    LISParamCode, LISParamValue, LISParamUnit, CreatedBy, CreatedOn, TestResultId
)
VALUES (
    N'AFP',
    N'12.8',
    N'ng/ml',
    @User,
    @Now,
    @TestResultId
);

UPDATE TestRequestDetails
SET ReportStatus = 5  -- DoctorApproved
WHERE Id = @RequestId;

-- Reference range for AFP (male adult) — enables H/L flag on test report
IF NOT EXISTS (
    SELECT 1 FROM HISParameterRangMaster r
    INNER JOIN HISParameterMaster p ON p.Id = r.HisParameterId
    WHERE p.HISTestCode = @HISTestCode AND p.HISParamCode = N'SCMP0001'
)
BEGIN
    INSERT INTO HISParameterRangMaster (
        HISRangeCode, HISRangeValue, Gender, AgeFrom, AgeTo, AgeType,
        MinValue, MaxValue, CreatedOn, HisParameterId
    )
    SELECT
        N'SCMP0001',
        N'0 - 8',
        N'Male',
        18, 120, N'Years',
        0, 8,
        @Now,
        p.Id
    FROM HISParameterMaster p
    WHERE p.HISTestCode = @HISTestCode AND p.HISParamCode = N'SCMP0001';
END

COMMIT TRANSACTION;

SELECT
    tr.Id AS TestResultId,
    tr.TestRequestId,
    tr.SampleNo,
    tr.HISTestCode,
    tr.LISTestCode,
    trd.ReportStatus,
    d.LISParamCode,
    d.LISParamValue,
    d.LISParamUnit
FROM TestResults tr
INNER JOIN TestRequestDetails trd ON trd.Id = tr.TestRequestId
INNER JOIN TestResultDetails d ON d.TestResultId = tr.Id
WHERE tr.TestRequestId = @RequestId;

GO
