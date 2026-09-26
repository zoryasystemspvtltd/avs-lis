namespace LIS.DataAccess.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CommissionRateFoundation : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF OBJECT_ID(N'dbo.ReferralDoctorCommissionRate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReferralDoctorCommissionRate (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReferralDoctorCommissionRate PRIMARY KEY,
        ReferralDoctorId INT NOT NULL,
        CommissionPercent DECIMAL(5,2) NOT NULL,
        EffectiveFrom DATETIME NOT NULL,
        EffectiveTo DATETIME NULL,
        IsActive BIT NOT NULL,
        IsLocked BIT NOT NULL CONSTRAINT DF_ReferralDoctorCommissionRate_IsLocked DEFAULT (0),
        CreatedBy NVARCHAR(MAX) NULL,
        CreatedOn DATETIME NOT NULL,
        ModifiedBy NVARCHAR(MAX) NULL,
        ModifiedOn DATETIME NOT NULL,
        CONSTRAINT FK_ReferralDoctorCommissionRate_ReferralDoctorMaster
            FOREIGN KEY (ReferralDoctorId) REFERENCES dbo.ReferralDoctorMaster(Id)
    );
    CREATE INDEX IX_ReferralDoctorCommissionRate_Doctor_From
        ON dbo.ReferralDoctorCommissionRate(ReferralDoctorId, EffectiveFrom);
END

IF OBJECT_ID(N'dbo.SampleCollectorCommissionRate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SampleCollectorCommissionRate (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SampleCollectorCommissionRate PRIMARY KEY,
        CollectorId INT NOT NULL,
        CommissionPercent DECIMAL(5,2) NOT NULL,
        EffectiveFrom DATETIME NOT NULL,
        EffectiveTo DATETIME NULL,
        IsActive BIT NOT NULL,
        IsLocked BIT NOT NULL CONSTRAINT DF_SampleCollectorCommissionRate_IsLocked DEFAULT (0),
        CreatedBy NVARCHAR(MAX) NULL,
        CreatedOn DATETIME NOT NULL,
        ModifiedBy NVARCHAR(MAX) NULL,
        ModifiedOn DATETIME NOT NULL,
        CONSTRAINT FK_SampleCollectorCommissionRate_SampleCollectorMaster
            FOREIGN KEY (CollectorId) REFERENCES dbo.SampleCollectorMaster(Id)
    );
    CREATE INDEX IX_SampleCollectorCommissionRate_Collector_From
        ON dbo.SampleCollectorCommissionRate(CollectorId, EffectiveFrom);
END

IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenuPermission WHERE MenuKey = N'MASTER_DOCTOR_COMMISSION_RATE')
BEGIN
    INSERT INTO dbo.RoleMenuPermission
        (RoleId, ModuleId, MenuKey, CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject, IsActive, ApplicationId, CreatedBy, CreatedOn)
    SELECT RoleId, ModuleId, N'MASTER_DOCTOR_COMMISSION_RATE', CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject, IsActive, ApplicationId, N'stage1-commission-rate', GETDATE()
    FROM dbo.RoleMenuPermission
    WHERE MenuKey = N'MASTER_REFERRAL_DOCTOR';
END

IF OBJECT_ID(N'dbo.RoleMenuPermission', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenuPermission WHERE MenuKey = N'MASTER_COLLECTOR_COMMISSION_RATE')
BEGIN
    INSERT INTO dbo.RoleMenuPermission
        (RoleId, ModuleId, MenuKey, CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject, IsActive, ApplicationId, CreatedBy, CreatedOn)
    SELECT RoleId, ModuleId, N'MASTER_COLLECTOR_COMMISSION_RATE', CanView, CanAdd, CanEdit, CanDelete, CanAuthorize, CanReject, IsActive, ApplicationId, N'stage1-commission-rate', GETDATE()
    FROM dbo.RoleMenuPermission
    WHERE MenuKey = N'MASTER_SAMPLE_COLLECTOR';
END
");
        }

        public override void Down()
        {
        }
    }
}
