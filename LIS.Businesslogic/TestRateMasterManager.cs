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
            NormalizeRateContext(item);
            ValidateNoOverlappingRate(item, null);
            Stamp(item, true);
            return rateRepo.Add(item);
        }

        public void Update(TestRateMaster item)
        {
            NormalizeRateContext(item);
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
            return EnrichBatch(rateRepo.Get(r => r.IsActive).ToList());
        }

        public ItemList<TestRateMaster> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var result = new ItemList<TestRateMaster>();
            var query = rateRepo.Get(r => true);

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                var search = option.SearchText.Trim();
                var testIds = testRepo.Get(t =>
                    t.HISTestCode.Contains(search) ||
                    t.HISTestCodeDescription.Contains(search)).Select(t => t.Id).ToList();

                query = query.Where(r => testIds.Contains(r.TestId));
            }

            result.TotalRecord = query.Count();

            var sortColumn = ResolveSortColumn(option.SortColumnName);
            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int pageSize = option.RecordPerPage == 0 ? result.TotalRecord : option.RecordPerPage;
            if (pageSize <= 0)
            {
                pageSize = 10;
            }

            List<TestRateMaster> page;
            if (sortColumn == "TestName" || sortColumn == "TestCode")
            {
                page = EnrichBatch(query.ToList())
                    .OrderBy(sortColumn, option.SortDirection)
                    .Skip(minRow)
                    .Take(pageSize)
                    .ToList();
            }
            else
            {
                page = ApplyQueryableSort(query, sortColumn, option.SortDirection)
                    .Skip(minRow)
                    .Take(pageSize)
                    .ToList();
                page = EnrichBatch(page);
            }

            result.Items = page;
            return result;
        }

        public IEnumerable<TestRateMaster> GetByTestId(int testId)
        {
            return EnrichBatch(rateRepo.Get(r => r.TestId == testId && r.IsActive).ToList());
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

            return EnrichBatch(new List<TestRateMaster> { rate }).FirstOrDefault();
        }

        private List<TestRateMaster> EnrichBatch(List<TestRateMaster> rates)
        {
            if (rates == null || rates.Count == 0)
            {
                return rates ?? new List<TestRateMaster>();
            }

            var testIds = rates.Select(r => r.TestId).Distinct().ToList();
            var corporateIds = rates.Where(r => r.CorporateId.HasValue).Select(r => r.CorporateId.Value).Distinct().ToList();
            var doctorIds = rates.Where(r => r.ReferralDoctorId.HasValue).Select(r => r.ReferralDoctorId.Value).Distinct().ToList();
            var profileIds = rates.Where(r => r.TestProfileId.HasValue).Select(r => r.TestProfileId.Value).Distinct().ToList();

            var tests = testIds.Count == 0
                ? new Dictionary<int, HisTestMaster>()
                : testRepo.Get(t => testIds.Contains(t.Id)).ToDictionary(t => t.Id);
            var corporates = corporateIds.Count == 0
                ? new Dictionary<int, CorporateMaster>()
                : corporateRepo.Get(c => corporateIds.Contains(c.Id)).ToDictionary(c => c.Id);
            var doctors = doctorIds.Count == 0
                ? new Dictionary<int, ReferralDoctorMaster>()
                : doctorRepo.Get(d => doctorIds.Contains(d.Id)).ToDictionary(d => d.Id);
            var profiles = profileIds.Count == 0
                ? new Dictionary<int, TestProfileMaster>()
                : profileRepo.Get(p => profileIds.Contains(p.Id)).ToDictionary(p => p.Id);

            foreach (var rate in rates)
            {
                if (tests.TryGetValue(rate.TestId, out var test))
                {
                    rate.TestCode = test.HISTestCode;
                    rate.TestName = test.HISTestCodeDescription;
                }

                if (rate.CorporateId.HasValue && corporates.TryGetValue(rate.CorporateId.Value, out var corp))
                {
                    rate.CorporateName = corp?.Name;
                }

                if (rate.ReferralDoctorId.HasValue && doctors.TryGetValue(rate.ReferralDoctorId.Value, out var doc))
                {
                    rate.ReferralDoctorName = doc?.Name;
                }

                if (rate.TestProfileId.HasValue && profiles.TryGetValue(rate.TestProfileId.Value, out var profile))
                {
                    rate.ProfileName = profile?.Name;
                }

                rate.RateTypeLabel = FormatRateType(rate.RateType);
            }

            return rates;
        }

        private static IQueryable<TestRateMaster> ApplyQueryableSort(IQueryable<TestRateMaster> query, string sortColumn, bool ascending)
        {
            switch (sortColumn)
            {
                case "Rate":
                    return ascending ? query.OrderBy(r => r.Rate) : query.OrderByDescending(r => r.Rate);
                case "EmergencyRate":
                    return ascending ? query.OrderBy(r => r.EmergencyRate) : query.OrderByDescending(r => r.EmergencyRate);
                case "EffectiveEnd":
                    return ascending ? query.OrderBy(r => r.EffectiveEnd) : query.OrderByDescending(r => r.EffectiveEnd);
                case "Id":
                    return ascending ? query.OrderBy(r => r.Id) : query.OrderByDescending(r => r.Id);
                case "TestId":
                    return ascending ? query.OrderBy(r => r.TestId) : query.OrderByDescending(r => r.TestId);
                case "RateType":
                    return ascending ? query.OrderBy(r => r.RateType) : query.OrderByDescending(r => r.RateType);
                default:
                    return ascending ? query.OrderBy(r => r.EffectiveStart) : query.OrderByDescending(r => r.EffectiveStart);
            }
        }

        private static string FormatRateType(int rateType)
        {
            switch (rateType)
            {
                case (int)RateType.Standard: return "Standard";
                case (int)RateType.Corporate: return "Corporate";
                case (int)RateType.ReferralDoctor: return "Referral Doctor";
                case (int)RateType.Profile: return "Profile";
                case (int)RateType.Emergency: return "Emergency";
                default: return rateType.ToString();
            }
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
                item.IsActive = true;
                item.CreatedOn = now;
                item.CreatedBy = identity?.ActivityMember;
            }

            item.ModifiedOn = now;
            item.ModifiedBy = identity?.ActivityMember;
        }

        private static void NormalizeRateContext(TestRateMaster item)
        {
            if (item == null)
            {
                return;
            }

            item.CorporateId = NormalizeContextId(item.CorporateId);
            item.ReferralDoctorId = NormalizeContextId(item.ReferralDoctorId);
            item.TestProfileId = NormalizeContextId(item.TestProfileId);
        }

        private static int? NormalizeContextId(int? value)
        {
            return value.HasValue && value.Value > 0 ? value : null;
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

            var corporateId = NormalizeContextId(item.CorporateId);
            var referralDoctorId = NormalizeContextId(item.ReferralDoctorId);
            var profileId = NormalizeContextId(item.TestProfileId);

            var overlapping = rateRepo.Get(r =>
                    r.IsActive &&
                    r.TestId == item.TestId &&
                    r.RateType == item.RateType &&
                    (!excludeId.HasValue || r.Id != excludeId.Value))
                .AsEnumerable()
                .Where(r =>
                    NullableEquals(NormalizeContextId(r.CorporateId), corporateId) &&
                    NullableEquals(NormalizeContextId(r.ReferralDoctorId), referralDoctorId) &&
                    NullableEquals(NormalizeContextId(r.TestProfileId), profileId) &&
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
