/*
  Seed RoleMenuPermission from existing RoleModuleMappings.
  For every role+module mapping with any permission bit set, insert all catalog
  menus under that module with the same Can* flags.

  Safe to re-run: deletes prior seeded rows for matching RoleId+ApplicationId+MenuKey then inserts.
  Does not change RoleModuleMappings / UserModules.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NULL
BEGIN
    RAISERROR('RoleMenuPermission table missing. Run AddRoleMenuPermission.sql first.', 16, 1);
    RETURN;
END

IF OBJECT_ID(N'tempdb..#MenuCatalog') IS NOT NULL DROP TABLE #MenuCatalog;
CREATE TABLE #MenuCatalog
(
    MenuKey     NVARCHAR(100) NOT NULL,
    ModuleName  NVARCHAR(100) NOT NULL
);

INSERT INTO #MenuCatalog (MenuKey, ModuleName) VALUES
(N'workingboard.recentSamples', N'Samples'),
(N'workingboard.sampleCollection', N'SampleCollection'),
(N'workingboard.sampleReceiving', N'SampleReceiving'),
(N'workingboard.radiologyReportEntry', N'RadiologyReportEntry'),
(N'workingboard.radiologyDoctorApproval', N'RadiologyDoctorApprovals'),
(N'workingboard.radiologyApproved', N'RadiologyDoctorApprovals'),
(N'workingboard.technicianApproval', N'Reports'),
(N'workingboard.testResultEdit', N'Reports'),
(N'workingboard.doctorApproval', N'DoctorsApprovals'),
(N'workingboard.approvedSamples', N'Reports'),
(N'workingboard.rejectedSamples', N'Reports'),
(N'workingboard.qualityControls', N'Reports'),
(N'setup.department', N'Masters'),
(N'setup.unit', N'Masters'),
(N'setup.method', N'Masters'),
(N'setup.equipment', N'Equipments'),
(N'setup.equipmentHeartbeat', N'Equipments'),
(N'SETUP_NOTIFICATION_CONFIGURATION', N'NotificationConfiguration'),
(N'setup.notificationConfiguration', N'NotificationConfiguration'),
(N'SETUP_REPORT_LAYOUT_CONFIGURATION', N'ReportLayoutConfiguration'),
(N'setup.reportLayoutConfiguration', N'ReportLayoutConfiguration'),
(N'masters.testMaster', N'HisTest'),
(N'masters.testProfile', N'Masters'),
(N'masters.specimen', N'Masters'),
(N'masters.testRate', N'TestRates'),
(N'masters.referralDoctor', N'Masters'),
(N'masters.corporate', N'Masters'),
(N'masters.parameter', N'Masters'),
(N'masters.testParamMapping', N'Masters'),
(N'masters.analyzerParamMapping', N'Masters'),
(N'masters.parameterRange', N'Masters'),
(N'transaction.patientDetails', N'PatientDetails'),
(N'transaction.saleInvoice', N'SaleInvoices'),
(N'reports.saleInvoiceRegister', N'Reports'),
(N'reports.testBookingRegister', N'Reports'),
(N'reports.diagnosticReport', N'Reports'),
(N'reports.radiologyReportPrint', N'RadiologyReports'),
(N'reports.collectionSummary', N'Reports'),
(N'reports.collectorWise', N'Reports'),
(N'reports.pendingCollection', N'Reports'),
(N'reports.recollection', N'Reports'),
(N'reports.receivedSamples', N'Reports'),
(N'reports.rejectedSamples', N'Reports'),
(N'reports.turnaround', N'Reports'),
(N'reports.radiologyPending', N'RadiologyReports'),
(N'reports.radiologyAuthorized', N'RadiologyReports'),
(N'reports.radiologyModality', N'RadiologyReports'),
(N'reports.radiologyProductivity', N'RadiologyReports'),
(N'account.users', N'Users'),
(N'account.roles', N'Roles');

DECLARE @Now DATETIME = GETUTCDATE();

;WITH SourceRows AS
(
    SELECT
        rmm.RoleId,
        rmm.ModuleId,
        rmm.ApplicationId,
        c.MenuKey,
        rmm.CanView,
        rmm.CanAdd,
        rmm.CanEdit,
        rmm.CanDelete,
        rmm.CanAuthorize,
        rmm.CanReject
    FROM dbo.RoleModuleMappings rmm
    INNER JOIN dbo.UserModules um ON um.Id = rmm.ModuleId
    INNER JOIN #MenuCatalog c ON c.ModuleName = um.Name
    INNER JOIN dbo.AspNetRoles r ON r.Id = rmm.RoleId
    WHERE r.Name <> N'Administrator'
      AND (
            rmm.CanView = 1 OR rmm.CanAdd = 1 OR rmm.CanEdit = 1
         OR rmm.CanDelete = 1 OR rmm.CanAuthorize = 1 OR rmm.CanReject = 1
          )
)
MERGE dbo.RoleMenuPermission AS tgt
USING SourceRows AS src
    ON tgt.RoleId = src.RoleId
   AND tgt.ModuleId = src.ModuleId
   AND tgt.MenuKey = src.MenuKey
   AND ((tgt.ApplicationId = src.ApplicationId) OR (tgt.ApplicationId IS NULL AND src.ApplicationId IS NULL))
WHEN MATCHED THEN
    UPDATE SET
        CanView = src.CanView,
        CanAdd = src.CanAdd,
        CanEdit = src.CanEdit,
        CanDelete = src.CanDelete,
        CanAuthorize = src.CanAuthorize,
        CanReject = src.CanReject,
        IsActive = 1,
        ModifiedBy = N'seed',
        ModifiedOn = @Now
WHEN NOT MATCHED THEN
    INSERT (RoleId, ModuleId, MenuKey, CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject,
            IsActive, ApplicationId, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn)
    VALUES (src.RoleId, src.ModuleId, src.MenuKey, src.CanView, src.CanAdd, src.CanEdit, src.CanDelete,
            src.CanAuthorize, src.CanReject, 1, src.ApplicationId, N'seed', @Now, N'seed', @Now);

SELECT COUNT(*) AS RoleMenuPermissionRows FROM dbo.RoleMenuPermission WHERE IsActive = 1;

DROP TABLE #MenuCatalog;
GO
