/*
  Migrate legacy RoleMenuPermission.MenuKey values to enterprise keys (MASTER_*, etc.)
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NULL
BEGIN
    RAISERROR('RoleMenuPermission missing.', 16, 1);
    RETURN;
END

;WITH Map(OldKey, NewKey) AS (
    SELECT * FROM (VALUES
        (N'workingboard.recentSamples', N'WORKING_BOARD_SAMPLES'),
        (N'workingboard.sampleCollection', N'WORKING_BOARD_COLLECTION'),
        (N'workingboard.sampleReceiving', N'WORKING_BOARD_RECEIVING'),
        (N'workingboard.radiologyReportEntry', N'WORKING_BOARD_RADIOLOGY_ENTRY'),
        (N'workingboard.radiologyDoctorApproval', N'WORKING_BOARD_RADIOLOGY_APPROVAL'),
        (N'workingboard.radiologyApproved', N'WORKING_BOARD_RADIOLOGY_APPROVED'),
        (N'workingboard.technicianApproval', N'WORKING_BOARD_TECHNICIAN_APPROVAL'),
        (N'workingboard.testResultEdit', N'WORKING_BOARD_LAB_RESULT_EDIT'),
        (N'workingboard.doctorApproval', N'WORKING_BOARD_DOCTOR_APPROVAL'),
        (N'workingboard.approvedSamples', N'WORKING_BOARD_APPROVED_SAMPLES'),
        (N'workingboard.rejectedSamples', N'WORKING_BOARD_REJECTED_SAMPLES'),
        (N'workingboard.qualityControls', N'WORKING_BOARD_QUALITY_CONTROLS'),
        (N'setup.department', N'MASTER_DEPARTMENT'),
        (N'setup.unit', N'MASTER_UNIT'),
        (N'setup.method', N'MASTER_METHOD'),
        (N'setup.equipment', N'SETUP_EQUIPMENT'),
        (N'setup.equipmentHeartbeat', N'SETUP_EQUIPMENT_HEARTBEAT'),
        (N'setup.notificationConfiguration', N'SETUP_NOTIFICATION_CONFIGURATION'),
        (N'setup.reportLayoutConfiguration', N'SETUP_REPORT_LAYOUT_CONFIGURATION'),
        (N'masters.testMaster', N'MASTER_TESTMASTER'),
        (N'masters.testProfile', N'MASTER_TESTPROFILE'),
        (N'masters.specimen', N'MASTER_SPECIMEN'),
        (N'masters.testRate', N'MASTER_TESTRATE'),
        (N'masters.referralDoctor', N'MASTER_REFERRAL_DOCTOR'),
        (N'masters.corporate', N'MASTER_CORPORATE'),
        (N'masters.parameter', N'MASTER_PARAMETER'),
        (N'masters.testParamMapping', N'MASTER_TEST_PARAM_MAPPING'),
        (N'masters.analyzerParamMapping', N'MASTER_ANALYZER_PARAM_MAPPING'),
        (N'masters.parameterRange', N'MASTER_PARAMETER_RANGE'),
        (N'transaction.patientDetails', N'TRANSACTION_PATIENT'),
        (N'transaction.saleInvoice', N'TRANSACTION_SALEINVOICE'),
        (N'reports.saleInvoiceRegister', N'REPORT_INVOICE_REGISTER'),
        (N'reports.testBookingRegister', N'REPORT_TEST_BOOKING'),
        (N'reports.diagnosticReport', N'REPORT_DIAGNOSTIC'),
        (N'reports.radiologyReportPrint', N'REPORT_RADIOLOGY'),
        (N'reports.collectionSummary', N'REPORT_COLLECTION_SUMMARY'),
        (N'reports.collectorWise', N'REPORT_COLLECTOR_WISE'),
        (N'reports.pendingCollection', N'REPORT_PENDING_COLLECTION'),
        (N'reports.recollection', N'REPORT_RECOLLECTION'),
        (N'reports.receivedSamples', N'REPORT_RECEIVED_SAMPLES'),
        (N'reports.rejectedSamples', N'REPORT_REJECTED_SAMPLES'),
        (N'reports.turnaround', N'REPORT_TAT'),
        (N'reports.radiologyPending', N'REPORT_RADIOLOGY_PENDING'),
        (N'reports.radiologyAuthorized', N'REPORT_RADIOLOGY_AUTHORIZED'),
        (N'reports.radiologyModality', N'REPORT_RADIOLOGY_MODALITY'),
        (N'reports.radiologyProductivity', N'REPORT_RADIOLOGY_PRODUCTIVITY'),
        (N'account.users', N'ACCOUNT_USERS'),
        (N'account.roles', N'ACCOUNT_ROLES')
    ) v(OldKey, NewKey)
)
UPDATE p
SET MenuKey = m.NewKey,
    ModifiedBy = N'migrate-keys',
    ModifiedOn = GETUTCDATE()
FROM dbo.RoleMenuPermission p
INNER JOIN Map m ON p.MenuKey = m.OldKey;

SELECT MenuKey, COUNT(*) AS Cnt FROM dbo.RoleMenuPermission GROUP BY MenuKey ORDER BY MenuKey;
GO
