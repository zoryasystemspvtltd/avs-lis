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
            ValidateNoOverlappingRate(item, null);
            Stamp(item, true);
            return rateRepo.Add(item);
        }

        public void Update(TestRateMaster item)
        {
            ValidateNoOverlappingRate(item, item.Id);
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

            var sortColumn = ResolveSortColumn(option.SortColumnName);
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

        public TestRateMaster GetEffectiveRate(int testId, int rateType, int? corporateId, int? referralDoctorId, int? profileId, DateTime? effectiveOn = null)
        {
            var asOf = (effectiveOn ?? DateTime.Today).Date;
            var query = rateRepo.Get(r =>
                r.TestId == testId &&
                r.IsActive &&
                r.RateType == rateType &&
                r.EffectiveStart <= asOf &&
                r.EffectiveEnd >= asOf);

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

        public TestRateMaster GetEffectiveRateForInvoice(int testId, DateTime invoiceDate, int? corporateId, int? referralDoctorId, int? profileId = null, bool useEmergency = false)
        {
            var asOf = invoiceDate.Date;

            if (useEmergency)
            {
                var emergency = GetEffectiveRate(testId, (int)RateType.Emergency, null, null, null, asOf);
                if (emergency != null)
                {
                    return emergency;
                }
            }

            if (corporateId.HasValue)
            {
                var corporate = GetEffectiveRate(testId, (int)RateType.Corporate, corporateId, null, null, asOf);
                if (corporate != null)
                {
                    return corporate;
                }
            }

            if (referralDoctorId.HasValue)
            {
                var doctor = GetEffectiveRate(testId, (int)RateType.ReferralDoctor, null, referralDoctorId, null, asOf);
                if (doctor != null)
                {
                    return doctor;
                }
            }

            if (profileId.HasValue)
            {
                var profile = GetEffectiveRate(testId, (int)RateType.Profile, null, null, profileId, asOf);
                if (profile != null)
                {
                    return profile;
                }
            }

            return GetEffectiveRate(testId, (int)RateType.Standard, null, null, null, asOf);
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

        private static string ResolveSortColumn(string sortColumnName)
        {
            if (string.IsNullOrWhiteSpace(sortColumnName))
            {
                return "EffectiveStart";
            }

            switch (sortColumnName.Trim())
            {
                case "rate":
                case "Rate":
                    return "Rate";
                case "emergencyRate":
                case "EmergencyRate":
                    return "EmergencyRate";
                case "effectiveStart":
                case "EffectiveStart":
                    return "EffectiveStart";
                case "effectiveEnd":
                case "EffectiveEnd":
                    return "EffectiveEnd";
                case "testName":
                case "TestName":
                    return "TestName";
                case "testCode":
                case "TestCode":
                    return "TestCode";
                case "id":
                case "Id":
                    return "Id";
                default:
                    return "EffectiveStart";
            }
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

        private void ValidateNoOverlappingRate(TestRateMaster item, int? excludeId)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.TestId <= 0)
            {
                throw new ArgumentException("Test is required.");
            }

            var start = item.EffectiveStart.Date;
            var end = item.EffectiveEnd.Date;
            if (end < start)
            {
                throw new InvalidOperationException("Effective end date must be on or after effective start date.");
            }

            var overlapping = rateRepo.Get(r =>
                    r.IsActive &&
                    r.TestId == item.TestId &&
                    r.RateType == item.RateType &&
                    (!excludeId.HasValue || r.Id != excludeId.Value))
                .AsEnumerable()
                .Where(r =>
                    NullableEquals(r.CorporateId, item.CorporateId) &&
                    NullableEquals(r.ReferralDoctorId, item.ReferralDoctorId) &&
                    NullableEquals(r.TestProfileId, item.TestProfileId) &&
                    r.EffectiveStart.Date <= end &&
                    r.EffectiveEnd.Date >= start)
                .Any();

            if (overlapping)
            {
                throw new InvalidOperationException(
                    "An active Test Rate already exists for the selected test and overlapping effective period.");
            }
        }

        private static bool NullableEquals(int? left, int? right)
        {
            return left == right;
        }
    }
}
