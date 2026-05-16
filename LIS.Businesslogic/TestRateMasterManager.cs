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
    public class TestRateMasterManager : ITestRateMasterManager
    {
        private readonly ModuleRepo<TestRateMaster> rateRepo;
        private readonly ModuleRepo<HisTestMaster> testRepo;
        private readonly ModuleRepo<CorporateMaster> corporateRepo;
        private readonly ModuleRepo<ReferralDoctorMaster> doctorRepo;
        private readonly ModuleRepo<TestProfileMaster> profileRepo;
        private readonly IModuleIdentity identity;
        private readonly ILogger logger;

        public TestRateMasterManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork unitOfWork)
        {
            this.logger = logger;
            this.identity = identity;
            rateRepo = new ModuleRepo<TestRateMaster>(logger, identity, unitOfWork);
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, unitOfWork);
            corporateRepo = new ModuleRepo<CorporateMaster>(logger, identity, unitOfWork);
            doctorRepo = new ModuleRepo<ReferralDoctorMaster>(logger, identity, unitOfWork);
            profileRepo = new ModuleRepo<TestProfileMaster>(logger, identity, unitOfWork);
        }

        public long Add(TestRateMaster item)
        {
            Stamp(item, true);
            return rateRepo.Add(item);
        }

        public void Update(TestRateMaster item)
        {
            Stamp(item, false);
            rateRepo.Update(item);
        }

        public void Delete(TestRateMaster item)
        {
            var existing = rateRepo.Get(item.Id);
            if (existing != null)
            {
                existing.IsActive = false;
                Stamp(existing, false);
                rateRepo.Update(existing);
            }
        }

        public TestRateMaster GetById(int id)
        {
            return Enrich(rateRepo.Get(id));
        }

        public IEnumerable<TestRateMaster> GetAllActive()
        {
            return rateRepo.Get(r => r.IsActive).Select(Enrich).ToList();
        }

        public ItemList<TestRateMaster> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var result = new ItemList<TestRateMaster>();
            var query = rateRepo.Get();

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                var search = option.SearchText.Trim();
                var testIds = testRepo.Get(t =>
                    t.HISTestCode.Contains(search) ||
                    t.HISTestCodeDescription.Contains(search)).Select(t => t.Id).ToList();

                query = query.Where(r => testIds.Contains(r.TestId));
            }

            var list = query.ToList().Select(Enrich).ToList();
            result.TotalRecord = list.Count;

            var sortColumn = string.IsNullOrEmpty(option.SortColumnName) ? "EffectiveStart" : option.SortColumnName;
            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int pageSize = option.RecordPerPage == 0 ? result.TotalRecord : option.RecordPerPage;

            result.Items = list
                .OrderBy(sortColumn, option.SortDirection)
                .Skip(minRow)
                .Take(pageSize)
                .ToList();

            return result;
        }

        public IEnumerable<TestRateMaster> GetByTestId(int testId)
        {
            return rateRepo.Get(r => r.TestId == testId && r.IsActive).Select(Enrich).ToList();
        }

        public TestRateMaster GetEffectiveRate(int testId, int rateType, int? corporateId, int? referralDoctorId, int? profileId)
        {
            var today = DateTime.Today;
            var query = rateRepo.Get(r =>
                r.TestId == testId &&
                r.IsActive &&
                r.RateType == rateType &&
                r.EffectiveStart <= today &&
                r.EffectiveEnd >= today);

            if (corporateId.HasValue)
            {
                query = query.Where(r => r.CorporateId == corporateId);
            }

            if (referralDoctorId.HasValue)
            {
                query = query.Where(r => r.ReferralDoctorId == referralDoctorId);
            }

            if (profileId.HasValue)
            {
                query = query.Where(r => r.TestProfileId == profileId);
            }

            var match = Enrich(query.OrderByDescending(r => r.EffectiveStart).FirstOrDefault());
            if (match != null && rateType == (int)RateType.Emergency && match.EmergencyRate > 0)
            {
                match.Rate = match.EmergencyRate;
            }

            return match;
        }

        private TestRateMaster Enrich(TestRateMaster rate)
        {
            if (rate == null)
            {
                return null;
            }

            var test = testRepo.Get(rate.TestId);
            if (test != null)
            {
                rate.TestCode = test.HISTestCode;
                rate.TestName = test.HISTestCodeDescription;
            }

            if (rate.CorporateId.HasValue)
            {
                var corp = corporateRepo.Get(rate.CorporateId.Value);
                rate.CorporateName = corp?.Name;
            }

            if (rate.ReferralDoctorId.HasValue)
            {
                var doc = doctorRepo.Get(rate.ReferralDoctorId.Value);
                rate.ReferralDoctorName = doc?.Name;
            }

            if (rate.TestProfileId.HasValue)
            {
                var profile = profileRepo.Get(rate.TestProfileId.Value);
                rate.ProfileName = profile?.Name;
            }

            return rate;
        }

        private void Stamp(TestRateMaster item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew)
            {
                item.CreatedOn = now;
                item.CreatedBy = identity?.ActivityMember;
            }

            item.ModifiedOn = now;
            item.ModifiedBy = identity?.ActivityMember;
        }
    }
}
