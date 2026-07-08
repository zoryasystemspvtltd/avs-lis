namespace LIS.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class fresh : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ContainerMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        Color = c.String(maxLength: 50),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ControlResultDetails",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        LISParamCode = c.String(),
                        LISParamValue = c.String(),
                        LISParamUnit = c.String(),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        ControlResultId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ControlResults", t => t.ControlResultId)
                .Index(t => t.ControlResultId);
            
            CreateTable(
                "dbo.ControlResults",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        SampleNo = c.String(),
                        ResultDate = c.DateTime(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        EquipmentId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.EquipmentMaster", t => t.EquipmentId)
                .Index(t => t.EquipmentId);
            
            CreateTable(
                "dbo.EquipmentMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Model = c.String(),
                        AccessKey = c.String(nullable: false, maxLength: 50),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CorporateMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        Address = c.String(maxLength: 200),
                        Phone = c.String(maxLength: 15),
                        ContactPerson = c.String(maxLength: 100),
                        DefaultDiscountPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Department",
                c => new
                    {
                        Code = c.String(nullable: false, maxLength: 15),
                        Name = c.String(nullable: false, maxLength: 55),
                    })
                .PrimaryKey(t => t.Code);
            
            CreateTable(
                "dbo.EquipmentHeartBeat",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AccessKey = c.String(nullable: false, maxLength: 50),
                        IsAlive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.HISParameterMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        HISTestCode = c.String(),
                        HISParamCode = c.String(),
                        HISParamDescription = c.String(),
                        HISParamUnit = c.String(),
                        HISParamMethod = c.String(),
                        LISParamCode = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        HisTestId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.HISTestMaster", t => t.HisTestId)
                .Index(t => t.HisTestId);
            
            CreateTable(
                "dbo.HISTestMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        HISTestCode = c.String(),
                        HISTestCodeDescription = c.String(),
                        HISSpecimenCode = c.String(),
                        HISSpecimenName = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        CreatedBy = c.String(),
                        IsActive = c.Boolean(nullable: false),
                        DepartmentCode = c.String(maxLength: 15),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Department", t => t.DepartmentCode)
                .Index(t => t.DepartmentCode);
            
            CreateTable(
                "dbo.HISParameterRangMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        HISRangeCode = c.String(),
                        HISRangeValue = c.String(),
                        Gender = c.String(),
                        AgeFrom = c.Decimal(nullable: false, precision: 18, scale: 2),
                        AgeTo = c.Decimal(nullable: false, precision: 18, scale: 2),
                        AgeType = c.String(),
                        MinValue = c.Decimal(nullable: false, precision: 18, scale: 2),
                        MaxValue = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CreatedOn = c.DateTime(nullable: false),
                        HisParameterId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.HISParameterMaster", t => t.HisParameterId)
                .Index(t => t.HisParameterId);
            
            CreateTable(
                "dbo.HISSpecimenMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(),
                        Name = c.String(),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.MethodMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.PatientDetails",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        HisPatientId = c.String(maxLength: 20),
                        Name = c.String(maxLength: 100),
                        Age = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Gender = c.String(maxLength: 10),
                        Phone = c.String(maxLength: 15),
                        IsActive = c.Boolean(nullable: false),
                        DateOfBirth = c.DateTime(nullable: false),
                        CreatedBy = c.String(maxLength: 80),
                        CreatedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ReferralDoctorMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        Phone = c.String(maxLength: 15),
                        Email = c.String(maxLength: 100),
                        Address = c.String(maxLength: 200),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SaleInvoiceDetail",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        SaleInvoiceId = c.Long(nullable: false),
                        TestId = c.Int(nullable: false),
                        Rate = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Quantity = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DiscountAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TaxAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        NetAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        RequestDetailId = c.Long(nullable: false),
                        SampleNo = c.String(maxLength: 30),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.HISTestMaster", t => t.TestId)
                .ForeignKey("dbo.SaleInvoice", t => t.SaleInvoiceId)
                .ForeignKey("dbo.TestRequestDetails", t => t.RequestDetailId)
                .Index(t => t.SaleInvoiceId)
                .Index(t => t.TestId)
                .Index(t => t.RequestDetailId);
            
            CreateTable(
                "dbo.SaleInvoice",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        InvoiceNo = c.String(nullable: false, maxLength: 50),
                        InvoiceDate = c.DateTime(nullable: false),
                        InvoiceStatus = c.Int(nullable: false),
                        PaymentStatus = c.Int(nullable: false),
                        RequestDetailId = c.Long(),
                        PatientId = c.Long(nullable: false),
                        GrossAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DiscountAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TaxAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        NetAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PaidAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DueAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        RefDoctorName = c.String(),
                        ReferralDoctorId = c.Int(),
                        CorporateId = c.Int(),
                        Notes = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        CreatedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PatientDetails", t => t.PatientId)
                .ForeignKey("dbo.TestRequestDetails", t => t.RequestDetailId)
                .Index(t => t.RequestDetailId)
                .Index(t => t.PatientId);
            
            CreateTable(
                "dbo.TestRequestDetails",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        SampleNo = c.String(maxLength: 30),
                        HISTestCode = c.String(maxLength: 20),
                        HISTestName = c.String(maxLength: 100),
                        SampleCollectionDate = c.DateTime(nullable: false),
                        SampleReceivedDate = c.DateTime(nullable: false),
                        SpecimenCode = c.String(maxLength: 20),
                        SpecimenName = c.String(maxLength: 100),
                        CreatedBy = c.String(maxLength: 80),
                        CreatedOn = c.DateTime(nullable: false),
                        ReportStatus = c.Int(nullable: false),
                        IPNo = c.String(maxLength: 20),
                        BedNo = c.String(maxLength: 20),
                        MRNo = c.String(maxLength: 20),
                        HISRequestId = c.String(maxLength: 20),
                        HISRequestNo = c.String(maxLength: 20),
                        DepartmentId = c.String(maxLength: 20),
                        Department = c.String(maxLength: 80),
                        PatientId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PatientDetails", t => t.PatientId)
                .Index(t => new { t.SampleNo, t.HISTestCode, t.ReportStatus }, unique: true)
                .Index(t => t.PatientId);
            
            CreateTable(
                "dbo.SampleTypeMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TestCategoryMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TestGroupMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        SpecimenTag = c.String(maxLength: 10),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TestMappingMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        HISTestCode = c.String(),
                        HISTestCodeDescription = c.String(),
                        SpecimenCode = c.String(),
                        SpecimenName = c.String(),
                        LISTestCode = c.String(),
                        LISTestCodeDescription = c.String(),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        GroupName = c.String(),
                        EquipmentId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.EquipmentMaster", t => t.EquipmentId)
                .Index(t => t.EquipmentId);
            
            CreateTable(
                "dbo.TestParameters",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        HISParamCode = c.String(),
                        HISParamName = c.String(),
                        HISTestCode = c.String(),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        TestRequestDetailsId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TestRequestDetails", t => t.TestRequestDetailsId)
                .Index(t => t.TestRequestDetailsId);
            
            CreateTable(
                "dbo.TestProfileDetail",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TestProfileId = c.Int(nullable: false),
                        TestId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.HISTestMaster", t => t.TestId)
                .ForeignKey("dbo.TestProfileMaster", t => t.TestProfileId)
                .Index(t => t.TestProfileId)
                .Index(t => t.TestId);
            
            CreateTable(
                "dbo.TestProfileMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 100),
                        PackageRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TestRateMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TestId = c.Int(nullable: false),
                        Rate = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EmergencyRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DiscountPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TaxPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                        RateType = c.Int(nullable: false),
                        CorporateId = c.Int(),
                        ReferralDoctorId = c.Int(),
                        TestProfileId = c.Int(),
                        EffectiveStart = c.DateTime(nullable: false),
                        EffectiveEnd = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.HISTestMaster", t => t.TestId)
                .Index(t => t.TestId);
            
            CreateTable(
                "dbo.TestResultDetails",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        LISParamCode = c.String(),
                        LISParamValue = c.String(),
                        LISParamUnit = c.String(),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        TestResultId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TestResults", t => t.TestResultId)
                .Index(t => t.TestResultId);
            
            CreateTable(
                "dbo.TestResults",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        SampleNo = c.String(),
                        HISTestCode = c.String(),
                        LISTestCode = c.String(),
                        SpecimenCode = c.String(),
                        SpecimenName = c.String(),
                        ResultDate = c.DateTime(nullable: false),
                        SampleCollectionDate = c.DateTime(nullable: false),
                        SampleReceivedDate = c.DateTime(nullable: false),
                        AuthorizationDate = c.DateTime(),
                        AuthorizedBy = c.String(),
                        ReviewDate = c.DateTime(),
                        ReviewedBy = c.String(),
                        TechnicianNote = c.String(),
                        DoctorNote = c.String(),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        PatientId = c.Long(nullable: false),
                        TestRequestId = c.Long(nullable: false),
                        EquipmentId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.EquipmentMaster", t => t.EquipmentId)
                .ForeignKey("dbo.PatientDetails", t => t.PatientId)
                .ForeignKey("dbo.TestRequestDetails", t => t.TestRequestId)
                .Index(t => t.PatientId)
                .Index(t => t.TestRequestId)
                .Index(t => t.EquipmentId);
            
            CreateTable(
                "dbo.UnitMaster",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 20),
                        Name = c.String(nullable: false, maxLength: 50),
                        IsActive = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedOn = c.DateTime(nullable: false),
                        ModifiedBy = c.String(),
                        ModifiedOn = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TestResultDetails", "TestResultId", "dbo.TestResults");
            DropForeignKey("dbo.TestResults", "TestRequestId", "dbo.TestRequestDetails");
            DropForeignKey("dbo.TestResults", "PatientId", "dbo.PatientDetails");
            DropForeignKey("dbo.TestResults", "EquipmentId", "dbo.EquipmentMaster");
            DropForeignKey("dbo.TestRateMaster", "TestId", "dbo.HISTestMaster");
            DropForeignKey("dbo.TestProfileDetail", "TestProfileId", "dbo.TestProfileMaster");
            DropForeignKey("dbo.TestProfileDetail", "TestId", "dbo.HISTestMaster");
            DropForeignKey("dbo.TestParameters", "TestRequestDetailsId", "dbo.TestRequestDetails");
            DropForeignKey("dbo.TestMappingMaster", "EquipmentId", "dbo.EquipmentMaster");
            DropForeignKey("dbo.SaleInvoiceDetail", "RequestDetailId", "dbo.TestRequestDetails");
            DropForeignKey("dbo.SaleInvoiceDetail", "SaleInvoiceId", "dbo.SaleInvoice");
            DropForeignKey("dbo.SaleInvoice", "RequestDetailId", "dbo.TestRequestDetails");
            DropForeignKey("dbo.TestRequestDetails", "PatientId", "dbo.PatientDetails");
            DropForeignKey("dbo.SaleInvoice", "PatientId", "dbo.PatientDetails");
            DropForeignKey("dbo.SaleInvoiceDetail", "TestId", "dbo.HISTestMaster");
            DropForeignKey("dbo.HISParameterRangMaster", "HisParameterId", "dbo.HISParameterMaster");
            DropForeignKey("dbo.HISParameterMaster", "HisTestId", "dbo.HISTestMaster");
            DropForeignKey("dbo.HISTestMaster", "DepartmentCode", "dbo.Department");
            DropForeignKey("dbo.ControlResultDetails", "ControlResultId", "dbo.ControlResults");
            DropForeignKey("dbo.ControlResults", "EquipmentId", "dbo.EquipmentMaster");
            DropIndex("dbo.TestResults", new[] { "EquipmentId" });
            DropIndex("dbo.TestResults", new[] { "TestRequestId" });
            DropIndex("dbo.TestResults", new[] { "PatientId" });
            DropIndex("dbo.TestResultDetails", new[] { "TestResultId" });
            DropIndex("dbo.TestRateMaster", new[] { "TestId" });
            DropIndex("dbo.TestProfileDetail", new[] { "TestId" });
            DropIndex("dbo.TestProfileDetail", new[] { "TestProfileId" });
            DropIndex("dbo.TestParameters", new[] { "TestRequestDetailsId" });
            DropIndex("dbo.TestMappingMaster", new[] { "EquipmentId" });
            DropIndex("dbo.TestRequestDetails", new[] { "PatientId" });
            DropIndex("dbo.TestRequestDetails", new[] { "SampleNo", "HISTestCode", "ReportStatus" });
            DropIndex("dbo.SaleInvoice", new[] { "PatientId" });
            DropIndex("dbo.SaleInvoice", new[] { "RequestDetailId" });
            DropIndex("dbo.SaleInvoiceDetail", new[] { "RequestDetailId" });
            DropIndex("dbo.SaleInvoiceDetail", new[] { "TestId" });
            DropIndex("dbo.SaleInvoiceDetail", new[] { "SaleInvoiceId" });
            DropIndex("dbo.HISParameterRangMaster", new[] { "HisParameterId" });
            DropIndex("dbo.HISTestMaster", new[] { "DepartmentCode" });
            DropIndex("dbo.HISParameterMaster", new[] { "HisTestId" });
            DropIndex("dbo.ControlResults", new[] { "EquipmentId" });
            DropIndex("dbo.ControlResultDetails", new[] { "ControlResultId" });
            DropTable("dbo.UnitMaster");
            DropTable("dbo.TestResults");
            DropTable("dbo.TestResultDetails");
            DropTable("dbo.TestRateMaster");
            DropTable("dbo.TestProfileMaster");
            DropTable("dbo.TestProfileDetail");
            DropTable("dbo.TestParameters");
            DropTable("dbo.TestMappingMaster");
            DropTable("dbo.TestGroupMaster");
            DropTable("dbo.TestCategoryMaster");
            DropTable("dbo.SampleTypeMaster");
            DropTable("dbo.TestRequestDetails");
            DropTable("dbo.SaleInvoice");
            DropTable("dbo.SaleInvoiceDetail");
            DropTable("dbo.ReferralDoctorMaster");
            DropTable("dbo.PatientDetails");
            DropTable("dbo.MethodMaster");
            DropTable("dbo.HISSpecimenMaster");
            DropTable("dbo.HISParameterRangMaster");
            DropTable("dbo.HISTestMaster");
            DropTable("dbo.HISParameterMaster");
            DropTable("dbo.EquipmentHeartBeat");
            DropTable("dbo.Department");
            DropTable("dbo.CorporateMaster");
            DropTable("dbo.EquipmentMaster");
            DropTable("dbo.ControlResults");
            DropTable("dbo.ControlResultDetails");
            DropTable("dbo.ContainerMaster");
        }
    }
}
