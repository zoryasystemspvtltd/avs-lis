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
        private readonly ModuleRepo<HisTestMaster> testRepo;
        private readonly ModuleRepo<HISParameterMaster> parameterRepo;
        private readonly ModuleRepo<HISParameterRangMaster> rangeRepo;

        public TestProfileMasterManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.Code, x => x.Name, x => x.IsActive)
        {
            detailRepo = new ModuleRepo<TestProfileDetail>(logger, identity, uow);
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, uow);
            parameterRepo = new ModuleRepo<HISParameterMaster>(logger, identity, uow);
            rangeRepo = new ModuleRepo<HISParameterRangMaster>(logger, identity, uow);
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

        public TestProfileHierarchyDto GetWithHierarchy(int id)
        {
            var profile = GetWithDetails(id);
            if (profile == null)
            {
                return null;
            }

            var dto = new TestProfileHierarchyDto
            {
                Id = profile.Id,
                Code = profile.Code,
                Name = profile.Name,
                PackageRate = profile.PackageRate,
                IsActive = profile.IsActive,
                CreatedBy = profile.CreatedBy,
                CreatedOn = profile.CreatedOn,
                ModifiedBy = profile.ModifiedBy,
                ModifiedOn = profile.ModifiedOn,
                ProfileDetails = profile.ProfileDetails,
                Tests = BuildTestHierarchy(profile.ProfileDetails)
            };

            return dto;
        }

        public void SaveWithDetails(TestProfileMaster profile, IEnumerable<TestProfileDetail> details)
        {
            var lineItems = (details ?? Enumerable.Empty<TestProfileDetail>()).ToList();
            ValidateProfile(profile, lineItems);

            long id;
            if (profile.Id == 0)
            {
                profile.ProfileDetails = null;
                id = Add(profile);
                profile.Id = (int)id;
            }
            else
            {
                id = profile.Id;
                detailRepo.Delete(d => d.TestProfileId == id);
                profile.ProfileDetails = null;
                Update(profile);
            }

            foreach (var detail in lineItems)
            {
                detail.TestProfileId = (int)id;
                detail.TestProfileMaster = null;
                detail.HisTestMaster = null;
                detailRepo.Add(detail);
            }
        }

        private void ValidateProfile(TestProfileMaster profile, List<TestProfileDetail> lineItems)
        {
            if (profile == null)
            {
                throw new ArgumentException("Profile is required.");
            }

            if (string.IsNullOrWhiteSpace(profile.Code))
            {
                throw new InvalidOperationException("Profile code is required.");
            }

            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                throw new InvalidOperationException("Profile name is required.");
            }

            var code = profile.Code.Trim();
            var name = profile.Name.Trim();
            profile.Code = code;
            profile.Name = name;

            if (Repo.Get(p => p.Code == code && p.Id != profile.Id).Any())
            {
                throw new InvalidOperationException("Profile code already exists.");
            }

            if (Repo.Get(p => p.Name == name && p.Id != profile.Id).Any())
            {
                throw new InvalidOperationException("Profile name already exists.");
            }

            if (!lineItems.Any())
            {
                throw new InvalidOperationException("At least one test is required in the profile.");
            }

            if (lineItems.Any(l => l.Quantity <= 0))
            {
                throw new InvalidOperationException("Test quantity must be greater than zero.");
            }

            var testIds = lineItems.Select(l => l.TestId).ToList();
            if (testIds.Count != testIds.Distinct().Count())
            {
                throw new InvalidOperationException("Duplicate tests are not allowed in a profile.");
            }

            foreach (var testId in testIds)
            {
                var test = testRepo.Get(testId);
                if (test == null)
                {
                    throw new InvalidOperationException($"Test id {testId} was not found.");
                }

                if (!test.IsActive)
                {
                    throw new InvalidOperationException($"Test '{test.HISTestCode}' is inactive and cannot be added to a profile.");
                }
            }
        }

        private List<TestProfileTestNodeDto> BuildTestHierarchy(IEnumerable<TestProfileDetail> details)
        {
            var nodes = new List<TestProfileTestNodeDto>();
            if (details == null)
            {
                return nodes;
            }

            foreach (var detail in details.OrderBy(d => d.TestId))
            {
                var test = testRepo.Get(detail.TestId);
                var node = new TestProfileTestNodeDto
                {
                    TestId = detail.TestId,
                    Quantity = detail.Quantity,
                    TestCode = test?.HISTestCode,
                    TestName = !string.IsNullOrWhiteSpace(test?.HISTestCodeDescription)
                        ? test.HISTestCodeDescription
                        : test?.HISTestCode,
                    Parameters = BuildParameterNodes(detail.TestId)
                };
                nodes.Add(node);
            }

            return nodes;
        }

        private List<TestProfileParameterNodeDto> BuildParameterNodes(int testId)
        {
            var parameters = parameterRepo.Get(p => p.HisTestId == testId).OrderBy(p => p.HISParamCode).ToList();
            var nodes = new List<TestProfileParameterNodeDto>();

            foreach (var param in parameters)
            {
                var ranges = rangeRepo.Get(r => r.HisParameterId == param.Id)
                    .OrderBy(r => r.HISRangeCode)
                    .Select(r => new TestProfileRangeNodeDto
                    {
                        RangeCode = r.HISRangeCode,
                        RangeValue = r.HISRangeValue,
                        Gender = r.Gender,
                        MinValue = r.MinValue,
                        MaxValue = r.MaxValue
                    })
                    .ToList();

                nodes.Add(new TestProfileParameterNodeDto
                {
                    ParamCode = param.HISParamCode,
                    Description = param.HISParamDescription,
                    Unit = param.HISParamUnit,
                    Method = param.HISParamMethod,
                    Ranges = ranges
                });
            }

            return nodes;
        }

        private void Stamp(TestProfileMaster item, bool isNew)
        {
            var now = DateTime.Now;
            if (isNew)
            {
                item.CreatedOn = now;
                item.CreatedBy = Identity.ActivityMember;
                item.IsActive = true;
            }

            item.ModifiedOn = now;
            item.ModifiedBy = Identity.ActivityMember;
        }
    }
}
