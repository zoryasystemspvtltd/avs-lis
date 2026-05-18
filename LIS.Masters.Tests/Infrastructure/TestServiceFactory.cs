using LIS.BusinessLogic;
using LIS.Businesslogic;
using LIS.DataAccess;
using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;

namespace LIS.Masters.Tests.Infrastructure
{
    /// <summary>
    /// Wires managers against the configured AVSLIS database (integration tests).
    /// </summary>
    public sealed class TestServiceFactory : IDisposable
    {
        public ApplicationDBContext Db { get; }
        public GenericUnitOfWork Uow { get; }
        public ILogger Logger { get; }
        public IModuleIdentity Identity { get; }

        public ReferralDoctorManager ReferralDoctor { get; }
        public CorporateManager Corporate { get; }
        public TestGroupManager TestGroup { get; }
        public TestCategoryManager TestCategory { get; }
        public UnitManager Unit { get; }
        public MethodManager Method { get; }
        public SampleTypeManager SampleType { get; }
        public ContainerManager Container { get; }
        public TestProfileMasterManager TestProfile { get; }
        public DepartmentManager Department { get; }
        public SpecimenManager Specimen { get; }
        public HISTestMasterManager HisTest { get; }
        public TestRateMasterManager TestRate { get; }
        public SaleInvoiceManager SaleInvoice { get; }
        public PatientMasterManager PatientMaster { get; }
        public PatientDetailManager PatientWorkflow { get; }
        public HisParameterMasterManager HisParameter { get; }
        public HisParameterRangeCrudManager HisParameterRange { get; }
        public TestMappingCrudManager TestMapping { get; }
        public EquipmentManager Equipment { get; }

        private TestServiceFactory(ApplicationDBContext db)
        {
            Db = db;
            Uow = new GenericUnitOfWork(db);
            Logger = LIS.Logger.Logger.LogInstance;
            Identity = new TestModuleIdentity();

            ReferralDoctor = new ReferralDoctorManager(Logger, Identity, Uow);
            Corporate = new CorporateManager(Logger, Identity, Uow);
            TestGroup = new TestGroupManager(Logger, Identity, Uow);
            TestCategory = new TestCategoryManager(Logger, Identity, Uow);
            Unit = new UnitManager(Logger, Identity, Uow);
            Method = new MethodManager(Logger, Identity, Uow);
            SampleType = new SampleTypeManager(Logger, Identity, Uow);
            Container = new ContainerManager(Logger, Identity, Uow);
            TestProfile = new TestProfileMasterManager(Logger, Identity, Uow);
            Department = new DepartmentManager(Logger, Identity, Uow);
            Specimen = new SpecimenManager(Logger, Identity, Uow);
            HisTest = new HISTestMasterManager(Logger, Identity, Uow);
            TestRate = new TestRateMasterManager(Logger, Identity, Uow);
            SaleInvoice = new SaleInvoiceManager(Logger, Identity, Uow, TestRate);
            PatientMaster = new PatientMasterManager(Logger, Identity, Uow);
            PatientWorkflow = new PatientDetailManager(Logger, Identity, Uow, db);
            HisParameter = new HisParameterMasterManager(Logger, Identity, Uow);
            HisParameterRange = new HisParameterRangeCrudManager(Logger, Identity, Uow);
            TestMapping = new TestMappingCrudManager(Logger, Identity, Uow);
            Equipment = new EquipmentManager(Logger, Identity, Uow);
        }

        public static bool TryCreate(out TestServiceFactory factory, out string error)
        {
            factory = null;
            error = null;
            try
            {
                var db = new ApplicationDBContext();
                db.Database.Connection.Open();
                db.Database.Connection.Close();
                factory = new TestServiceFactory(db);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public void Dispose()
        {
            Uow?.Dispose();
            Db?.Dispose();
        }
    }
}
