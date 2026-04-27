namespace LIS.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class fresh : DbMigration
    {
        public override void Up()
        {
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
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TestResultDetails", "TestResultId", "dbo.TestResults");
            DropForeignKey("dbo.TestResults", "TestRequestId", "dbo.TestRequestDetails");
            DropForeignKey("dbo.TestResults", "PatientId", "dbo.PatientDetails");
            DropForeignKey("dbo.TestResults", "EquipmentId", "dbo.EquipmentMaster");
            DropForeignKey("dbo.TestParameters", "TestRequestDetailsId", "dbo.TestRequestDetails");
            DropForeignKey("dbo.TestRequestDetails", "PatientId", "dbo.PatientDetails");
            DropForeignKey("dbo.TestMappingMaster", "EquipmentId", "dbo.EquipmentMaster");
            DropForeignKey("dbo.HISParameterRangMaster", "HisParameterId", "dbo.HISParameterMaster");
            DropForeignKey("dbo.HISParameterMaster", "HisTestId", "dbo.HISTestMaster");
            DropForeignKey("dbo.HISTestMaster", "DepartmentCode", "dbo.Department");
            DropForeignKey("dbo.ControlResultDetails", "ControlResultId", "dbo.ControlResults");
            DropForeignKey("dbo.ControlResults", "EquipmentId", "dbo.EquipmentMaster");
            DropIndex("dbo.TestResults", new[] { "EquipmentId" });
            DropIndex("dbo.TestResults", new[] { "TestRequestId" });
            DropIndex("dbo.TestResults", new[] { "PatientId" });
            DropIndex("dbo.TestResultDetails", new[] { "TestResultId" });
            DropIndex("dbo.TestRequestDetails", new[] { "PatientId" });
            DropIndex("dbo.TestRequestDetails", new[] { "SampleNo", "HISTestCode", "ReportStatus" });
            DropIndex("dbo.TestParameters", new[] { "TestRequestDetailsId" });
            DropIndex("dbo.TestMappingMaster", new[] { "EquipmentId" });
            DropIndex("dbo.HISParameterRangMaster", new[] { "HisParameterId" });
            DropIndex("dbo.HISTestMaster", new[] { "DepartmentCode" });
            DropIndex("dbo.HISParameterMaster", new[] { "HisTestId" });
            DropIndex("dbo.ControlResults", new[] { "EquipmentId" });
            DropIndex("dbo.ControlResultDetails", new[] { "ControlResultId" });
            DropTable("dbo.TestResults");
            DropTable("dbo.TestResultDetails");
            DropTable("dbo.TestRequestDetails");
            DropTable("dbo.TestParameters");
            DropTable("dbo.TestMappingMaster");
            DropTable("dbo.PatientDetails");
            DropTable("dbo.HISSpecimenMaster");
            DropTable("dbo.HISParameterRangMaster");
            DropTable("dbo.HISTestMaster");
            DropTable("dbo.HISParameterMaster");
            DropTable("dbo.EquipmentHeartBeat");
            DropTable("dbo.Department");
            DropTable("dbo.EquipmentMaster");
            DropTable("dbo.ControlResults");
            DropTable("dbo.ControlResultDetails");
        }
    }
}
