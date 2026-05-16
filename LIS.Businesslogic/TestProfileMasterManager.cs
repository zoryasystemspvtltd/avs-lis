using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.BusinessLogic
{
    public class TestProfileMasterManager : MasterCrudManager<TestProfileMaster>, ITestProfileMasterManager
    {
        private readonly ModuleRepo<TestProfileDetail> detailRepo;

        public TestProfileMasterManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.Code, x => x.Name, x => x.IsActive)
        {
            detailRepo = new ModuleRepo<TestProfileDetail>(logger, identity, uow);
        }

        public new long Add(TestProfileMaster item)
        {
            Stamp(item, true);
            return base.Add(item);
        }

        public new void Update(TestProfileMaster item)
        {
            Stamp(item, false);
            base.Update(item);
        }

        public TestProfileMaster GetWithDetails(int id)
        {
            var profile = GetById(id);
            if (profile != null)
            {
                profile.ProfileDetails = detailRepo.Get(d => d.TestProfileId == id).ToList();
            }

            return profile;
        }

        public void SaveWithDetails(TestProfileMaster profile, IEnumerable<TestProfileDetail> details)
        {
            long id;
            if (profile.Id == 0)
            {
                id = Add(profile);
                profile.Id = (int)id;
            }
            else
            {
                Update(profile);
                id = profile.Id;
                foreach (var old in detailRepo.Get(d => d.TestProfileId == id).ToList())
                {
                    detailRepo.Delete(old);
                }
            }

            foreach (var detail in details ?? Enumerable.Empty<TestProfileDetail>())
            {
                detail.TestProfileId = (int)id;
                detailRepo.Add(detail);
            }
        }

        private void Stamp(TestProfileMaster item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew)
            {
                item.CreatedOn = now;
                item.CreatedBy = Identity.ActivityMember;
            }

            item.ModifiedOn = now;
            item.ModifiedBy = Identity.ActivityMember;
        }
    }
}
