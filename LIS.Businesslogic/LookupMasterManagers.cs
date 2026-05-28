using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;

namespace LIS.BusinessLogic
{
    public class ReferralDoctorManager : MasterCrudManager<ReferralDoctorMaster>, IMasterCrudManager<ReferralDoctorMaster>
    {
        public ReferralDoctorManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.Code, x => x.Name, x => x.IsActive) { }

        public new long Add(ReferralDoctorMaster item) { Stamp(item, true); return base.Add(item); }
        public new void Update(ReferralDoctorMaster item) { Stamp(item, false); base.Update(item); }

        private void Stamp(ReferralDoctorMaster item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew) { item.CreatedOn = now; item.CreatedBy = Identity.ActivityMember; }
            item.ModifiedOn = now; item.ModifiedBy = Identity.ActivityMember;
        }
    }

    public class CorporateManager : MasterCrudManager<CorporateMaster>, IMasterCrudManager<CorporateMaster>
    {
        public CorporateManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.Code, x => x.Name, x => x.IsActive) { }

        public new long Add(CorporateMaster item) { Stamp(item, true); return base.Add(item); }
        public new void Update(CorporateMaster item) { Stamp(item, false); base.Update(item); }

        private void Stamp(CorporateMaster item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew) { item.CreatedOn = now; item.CreatedBy = Identity.ActivityMember; }
            item.ModifiedOn = now; item.ModifiedBy = Identity.ActivityMember;
        }
    }

    public class TestGroupManager : MasterCrudManager<TestGroupMaster>, IMasterCrudManager<TestGroupMaster>
    {
        public TestGroupManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.Code, x => x.Name, x => x.IsActive) { }

        public new long Add(TestGroupMaster item) { Stamp(item, true); return base.Add(item); }
        public new void Update(TestGroupMaster item) { Stamp(item, false); base.Update(item); }

        private void Stamp(TestGroupMaster item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew) { item.CreatedOn = now; item.CreatedBy = Identity.ActivityMember; }
            item.ModifiedOn = now; item.ModifiedBy = Identity.ActivityMember;
        }
    }

    public class TestCategoryManager : MasterCrudManager<TestCategoryMaster>, IMasterCrudManager<TestCategoryMaster>
    {
        public TestCategoryManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.Code, x => x.Name, x => x.IsActive) { }

        public new long Add(TestCategoryMaster item) { Stamp(item, true); return base.Add(item); }
        public new void Update(TestCategoryMaster item) { Stamp(item, false); base.Update(item); }

        private void Stamp(TestCategoryMaster item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew) { item.CreatedOn = now; item.CreatedBy = Identity.ActivityMember; }
            item.ModifiedOn = now; item.ModifiedBy = Identity.ActivityMember;
        }
    }

    public class UnitManager : MasterCrudManager<UnitMaster>, IMasterCrudManager<UnitMaster>
    {
        public UnitManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.Code, x => x.Name, x => x.IsActive) { }

        public override long Add(UnitMaster item)
        {
            item.IsActive = true;
            Stamp(item, true);
            return base.Add(item);
        }
        public override void Update(UnitMaster item) { Stamp(item, false); base.Update(item); }

        private void Stamp(UnitMaster item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew) { item.CreatedOn = now; item.CreatedBy = Identity.ActivityMember; }
            item.ModifiedOn = now; item.ModifiedBy = Identity.ActivityMember;
        }
    }

    public class MethodManager : MasterCrudManager<MethodMaster>, IMasterCrudManager<MethodMaster>
    {
        public MethodManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.Code, x => x.Name, x => x.IsActive) { }

        public override long Add(MethodMaster item)
        {
            item.IsActive = true;
            Stamp(item, true);
            return base.Add(item);
        }
        public override void Update(MethodMaster item) { Stamp(item, false); base.Update(item); }

        private void Stamp(MethodMaster item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew) { item.CreatedOn = now; item.CreatedBy = Identity.ActivityMember; }
            item.ModifiedOn = now; item.ModifiedBy = Identity.ActivityMember;
        }
    }

    public class SampleTypeManager : MasterCrudManager<SampleTypeMaster>, IMasterCrudManager<SampleTypeMaster>
    {
        public SampleTypeManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.Code, x => x.Name, x => x.IsActive) { }

        public new long Add(SampleTypeMaster item) { Stamp(item, true); return base.Add(item); }
        public new void Update(SampleTypeMaster item) { Stamp(item, false); base.Update(item); }

        private void Stamp(SampleTypeMaster item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew) { item.CreatedOn = now; item.CreatedBy = Identity.ActivityMember; }
            item.ModifiedOn = now; item.ModifiedBy = Identity.ActivityMember;
        }
    }

    public class ContainerManager : MasterCrudManager<ContainerMaster>, IMasterCrudManager<ContainerMaster>
    {
        public ContainerManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.Code, x => x.Name, x => x.IsActive) { }

        public new long Add(ContainerMaster item) { Stamp(item, true); return base.Add(item); }
        public new void Update(ContainerMaster item) { Stamp(item, false); base.Update(item); }

        private void Stamp(ContainerMaster item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew) { item.CreatedOn = now; item.CreatedBy = Identity.ActivityMember; }
            item.ModifiedOn = now; item.ModifiedBy = Identity.ActivityMember;
        }
    }
}
