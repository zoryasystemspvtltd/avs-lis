using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace LIS.BusinessLogic
{
    public class HisParameterMasterManager : MasterCrudManager<HISParameterMaster>, IMasterCrudManager<HISParameterMaster>
    {
        public const string ParameterCodeExistsMessage = "Parameter Code already exists.";
        public const string ParameterDescriptionExistsMessage = "Description already exists.";
        public const string ParameterCombinationExistsMessage = "Parameter Code and Description combination already exists.";

        private readonly ModuleRepo<HisTestMaster> testRepo;
        private readonly ModuleRepo<HISParameterRangMaster> rangeRepo;

        public HisParameterMasterManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.HISParamCode, x => x.HISParamDescription, x => true, "HISParamCode")
        {
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, uow);
            rangeRepo = new ModuleRepo<HISParameterRangMaster>(logger, identity, uow);
        }

        public override long Add(HISParameterMaster item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            NormalizeParameter(item);

            if (item.HisTestId.HasValue && item.HisTestId.Value <= 0)
            {
                item.HisTestId = null;
            }

            if (item.HisTestId.HasValue && item.HisTestId.Value > 0 && string.IsNullOrEmpty(item.HISTestCode))
            {
                var test = testRepo.Get(item.HisTestId.Value);
                if (test != null)
                {
                    item.HISTestCode = test.HISTestCode;
                }
            }

            ValidateParameterUniqueness(item, null);
            item.CreatedOn = DateTime.Now;

            return base.Add(item);
        }

        public override void Update(HISParameterMaster item)
        {
            NormalizeParameter(item);

            ValidateParameterUniqueness(item, item.Id);

            if (item.HisTestId.HasValue && item.HisTestId.Value > 0)
            {
                var test = testRepo.Get(item.HisTestId.Value);
                if (test != null)
                {
                    item.HISTestCode = test.HISTestCode;
                }
            }

            base.Update(item);
        }

        public new void Delete(HISParameterMaster item)
        {
            if (item?.Id > 0)
            {
                var existing = Repo.Get(item.Id);
                if (existing != null)
                {
                    if (rangeRepo.Get(r => r.HisParameterId == existing.Id).Any())
                    {
                        throw new InvalidOperationException("Cannot delete parameter while ranges exist. Remove ranges first.");
                    }

                    Repo.Delete(existing);
                }
            }
        }

        private void ValidateParameterUniqueness(HISParameterMaster item, int? excludeId)
        {
            if (item == null)
            {
                return;
            }

            var code = item.HISParamCode;
            var description = item.HISParamDescription;

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("Parameter Code is required.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new InvalidOperationException("Description is required.");
            }

            var candidates = UnitOfWork.Context.Set<HISParameterMaster>()
                .AsNoTracking()
                .Where(p => !excludeId.HasValue || p.Id != excludeId.Value)
                .ToList();

            if (candidates.Any(p => !string.IsNullOrWhiteSpace(p.HISParamCode)
                && string.Equals(p.HISParamCode.Trim(), code, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(ParameterCodeExistsMessage);
            }

            if (candidates.Any(p => !string.IsNullOrWhiteSpace(p.HISParamDescription)
                && string.Equals(p.HISParamDescription.Trim(), description, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(ParameterDescriptionExistsMessage);
            }

            if (candidates.Any(p => !string.IsNullOrWhiteSpace(p.HISParamCode)
                && !string.IsNullOrWhiteSpace(p.HISParamDescription)
                && string.Equals(p.HISParamCode.Trim(), code, StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.HISParamDescription.Trim(), description, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(ParameterCombinationExistsMessage);
            }
        }

        private static void NormalizeParameter(HISParameterMaster item)
        {
            if (item == null)
            {
                return;
            }

            item.HISParamCode = (item.HISParamCode ?? string.Empty).Trim();
            item.HISParamDescription = (item.HISParamDescription ?? string.Empty).Trim();
            item.HISTestCode = (item.HISTestCode ?? string.Empty).Trim();
            item.HISTestCodeDescription = (item.HISTestCodeDescription ?? string.Empty).Trim();
            item.HISParamUnit = (item.HISParamUnit ?? string.Empty).Trim();
            item.HISParamMethod = (item.HISParamMethod ?? string.Empty).Trim();
            item.LISParamCode = (item.LISParamCode ?? string.Empty).Trim();
            item.Comments = (item.Comments ?? string.Empty).Trim();
        }

        public override ItemList<HISParameterMaster> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var result = new ItemList<HISParameterMaster>();
            var query = Repo.Get().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(option.SearchText))
            {
                var search = option.SearchText.Trim();
                query = query.Where(p =>
                    (!string.IsNullOrEmpty(p.HISParamCode) &&
                        p.HISParamCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(p.HISParamDescription) &&
                        p.HISParamDescription.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(p.HISParamUnit) &&
                        p.HISParamUnit.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(p.HISParamMethod) &&
                        p.HISParamMethod.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(p.LISParamCode) &&
                        p.LISParamCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(p.HISTestCode) &&
                        p.HISTestCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            var list = query.ToList();
            result.TotalRecord = list.Count;

            var sortColumn = ResolveParameterSortColumn(option.SortColumnName);
            int minRow = Math.Max(0, (option.CurrentPage - 1) * option.RecordPerPage);
            int pageSize = option.RecordPerPage <= 0 ? result.TotalRecord : option.RecordPerPage;

            result.Items = list
                .OrderBy(sortColumn, option.SortDirection)
                .Skip(minRow)
                .Take(pageSize)
                .ToList();

            try
            {
                Enrich(result.Items);
            }
            catch (Exception)
            {
                // Enrichment is non-critical for list/search; never fail the page.
            }

            return result;
        }

        private string ResolveParameterSortColumn(string sortColumnName)
        {
            if (string.IsNullOrWhiteSpace(sortColumnName))
            {
                return "HISParamCode";
            }

            var prop = typeof(HISParameterMaster).GetProperty(
                sortColumnName.Trim(),
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if (prop != null)
            {
                return prop.Name;
            }

            return "HISParamCode";
        }

        public override HISParameterMaster GetById(int id)
        {
            var item = base.GetById(id);
            if (item != null)
            {
                Enrich(new[] { item });
            }

            return item;
        }

        private void Enrich(IEnumerable<HISParameterMaster> items)
        {
            var tests = testRepo.Get().ToDictionary(t => t.Id, t => t);
            foreach (var p in items)
            {
                if (p.HisTestId.HasValue && tests.TryGetValue(p.HisTestId.Value, out var test))
                {
                    p.HISTestCode = p.HISTestCode ?? test.HISTestCode;
                    p.HISTestCodeDescription = p.HISTestCodeDescription ?? test.HISTestCodeDescription;
                }
            }
        }
    }

    public class HisParameterRangeCrudManager : MasterCrudManager<HISParameterRangMaster>, IMasterCrudManager<HISParameterRangMaster>
    {
        private readonly ModuleRepo<HISParameterMaster> parameterRepo;

        public HisParameterRangeCrudManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.HISRangeCode, x => x.HISRangeValue, x => true, "HISRangeCode")
        {
            parameterRepo = new ModuleRepo<HISParameterMaster>(logger, identity, uow);
        }

        public string GenerateNextRangeCode()
        {
            var codes = Repo.Get().Select(r => r.HISRangeCode).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
            var max = 0;
            foreach (var code in codes)
            {
                var trimmed = code.Trim();
                if (trimmed.Length > 1 && trimmed[0] == 'R' && int.TryParse(trimmed.Substring(1), out var num))
                {
                    if (num > max)
                    {
                        max = num;
                    }
                }
            }

            return $"R{(max + 1).ToString("D7", System.Globalization.CultureInfo.InvariantCulture)}";
        }

        public override long Add(HISParameterRangMaster item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.HisParameterId <= 0)
            {
                throw new InvalidOperationException("Parameter is required.");
            }

            NormalizeAgeType(item);

            if (string.IsNullOrWhiteSpace(item.HISRangeCode))
            {
                item.HISRangeCode = GenerateNextRangeCode();
            }

            item.HISRangeCode = item.HISRangeCode.Trim();
            if (Repo.Get(r => r.HISRangeCode == item.HISRangeCode).Any())
            {
                throw new InvalidOperationException("A parameter range with this code already exists.");
            }

            item.CreatedOn = DateTime.Now;
            return base.Add(item);
        }

        public override void Update(HISParameterRangMaster item)
        {
            if (item == null || item.Id <= 0)
            {
                throw new ArgumentException("Invalid parameter range record.");
            }

            if (item.HisParameterId <= 0)
            {
                throw new InvalidOperationException("Parameter is required.");
            }

            NormalizeAgeType(item);

            var existing = Repo.Get(item.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Parameter range record not found.");
            }

            existing.HISRangeValue = item.HISRangeValue;
            existing.Gender = item.Gender;
            existing.AgeFrom = item.AgeFrom;
            existing.AgeTo = item.AgeTo;
            existing.AgeType = item.AgeType;
            existing.MinValue = item.MinValue;
            existing.MaxValue = item.MaxValue;
            existing.HisParameterId = item.HisParameterId;

            base.Update(existing);
        }

        private static void NormalizeAgeType(HISParameterRangMaster item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.AgeType))
            {
                return;
            }

            var normalized = item.AgeType.Trim();
            if (normalized.Equals("Years", StringComparison.OrdinalIgnoreCase))
            {
                normalized = "Year";
            }

            if (!normalized.Equals("Year", StringComparison.OrdinalIgnoreCase) &&
                !normalized.Equals("Month", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Age Type must be Year or Month.");
            }

            item.AgeType = normalized;
        }

        public override ItemList<HISParameterRangMaster> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var ranges = Repo.Get().ToList();
            Enrich(ranges);

            if (!string.IsNullOrWhiteSpace(option.SearchText))
            {
                var search = option.SearchText.Trim();
                // Match listing-visible identity fields: range code, parameter code, parameter name.
                ranges = ranges.Where(r =>
                        (!string.IsNullOrWhiteSpace(r.HISRangeCode) &&
                            r.HISRangeCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrWhiteSpace(r.HisParamCode) &&
                            r.HisParamCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrWhiteSpace(r.HisParamDescription) &&
                            r.HisParamDescription.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
            }

            var result = new ItemList<HISParameterRangMaster>
            {
                TotalRecord = ranges.Count
            };
            var minRow = Math.Max(0, (option.CurrentPage - 1) * option.RecordPerPage);
            var pageSize = option.RecordPerPage <= 0 ? result.TotalRecord : option.RecordPerPage;
            result.Items = ranges
                .OrderBy(ResolveSortColumn(option.SortColumnName), option.SortDirection)
                .Skip(minRow)
                .Take(pageSize)
                .ToList();

            return result;
        }

        public override HISParameterRangMaster GetById(int id)
        {
            var item = base.GetById(id);
            if (item != null)
            {
                Enrich(new[] { item });
            }

            return item;
        }

        private void Enrich(IEnumerable<HISParameterRangMaster> items)
        {
            var parameters = parameterRepo.Get().ToDictionary(p => p.Id, p => p);
            foreach (var range in items)
            {
                if (parameters.TryGetValue(range.HisParameterId, out var parameter))
                {
                    range.HisParamCode = parameter.HISParamCode;
                    range.HisParamDescription = parameter.HISParamDescription;
                }
            }
        }

        public override void Delete(HISParameterRangMaster item)
        {
            if (item?.Id > 0)
            {
                var existing = Repo.Get(item.Id);
                if (existing != null)
                {
                    Repo.Delete(existing);
                }
            }
        }
    }

    public class TestMappingCrudManager : MasterCrudManager<TestMappingMaster>, IMasterCrudManager<TestMappingMaster>
    {
        private readonly ModuleRepo<EquipmentMaster> equipmentRepo;
        private readonly ModuleRepo<HISParameterMaster> parameterRepo;

        public TestMappingCrudManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.LISTestCode, x => x.HISParamDescription, x => x.IsActive, "HISParamCode")
        {
            equipmentRepo = new ModuleRepo<EquipmentMaster>(logger, identity, uow);
            parameterRepo = new ModuleRepo<HISParameterMaster>(logger, identity, uow);
        }

        public new long Add(TestMappingMaster item)
        {
            item.CreatedOn = DateTime.Now;
            item.CreatedBy = Identity?.ActivityMember;
            ApplyParameterMetadata(item);
            if (ExistsDuplicate(item, null))
            {
                throw new InvalidOperationException("Analyzer Parameter Mapping already exists.");
            }

            return base.Add(item);
        }

        public new void Update(TestMappingMaster item)
        {
            ApplyParameterMetadata(item);
            if (ExistsDuplicate(item, item.Id))
            {
                throw new InvalidOperationException("Analyzer Parameter Mapping already exists.");
            }

            base.Update(item);
        }

        public new TestMappingMaster GetById(int id)
        {
            var item = base.GetById(id);
            if (item != null)
            {
                EnrichMappingItem(item);
            }

            return item;
        }

        public new void Delete(TestMappingMaster item)
        {
            var existing = Repo.Get(item.Id);
            if (existing != null)
            {
                existing.IsActive = false;
                Repo.Update(existing);
            }
        }

        public override ItemList<TestMappingMaster> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var result = new ItemList<TestMappingMaster>();
            var query = Repo.Get().AsEnumerable();

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                var search = option.SearchText.Trim();
                query = query.Where(m =>
                    (m.HISParamCode != null && m.HISParamCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (m.LISTestCode != null && m.LISTestCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (m.HISParamDescription != null && m.HISParamDescription.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            var list = query.ToList();
            var equipmentNames = equipmentRepo.Get().ToDictionary(e => e.Id, e => e.Name);
            var paramNames = parameterRepo.Get().ToDictionary(
                p => (p.HISParamCode ?? string.Empty).Trim(),
                p => p.HISParamDescription,
                StringComparer.OrdinalIgnoreCase);
            foreach (var m in list)
            {
                EnrichMappingItem(m, equipmentNames, paramNames);
            }

            result.TotalRecord = list.Count;
            var sortColumn = ResolveMappingSortColumn(option.SortColumnName);
            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int pageSize = option.RecordPerPage == 0 ? result.TotalRecord : option.RecordPerPage;

            result.Items = list
                .OrderBy(sortColumn, option.SortDirection)
                .Skip(minRow)
                .Take(pageSize)
                .ToList();

            return result;
        }

        private static string ResolveMappingSortColumn(string sortColumnName)
        {
            const string fallback = "HISParamCode";
            if (string.IsNullOrWhiteSpace(sortColumnName))
            {
                return fallback;
            }

            var col = sortColumnName.Trim();
            var prop = typeof(TestMappingMaster).GetProperty(
                col,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            return prop != null ? prop.Name : fallback;
        }

        private void ApplyParameterMetadata(TestMappingMaster item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.HISParamCode))
            {
                return;
            }

            var param = parameterRepo.Get()
                .FirstOrDefault(p => p.HISParamCode != null &&
                    p.HISParamCode.Equals(item.HISParamCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (param == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(item.HISParamDescription))
            {
                item.HISParamDescription = param.HISParamDescription;
            }
        }

        private void EnrichMappingItem(
            TestMappingMaster m,
            Dictionary<int, string> equipmentNames = null,
            Dictionary<string, string> paramNames = null)
        {
            if (m == null)
            {
                return;
            }

            if (equipmentNames == null)
            {
                equipmentNames = equipmentRepo.Get().ToDictionary(e => e.Id, e => e.Name);
            }

            if (paramNames == null)
            {
                paramNames = parameterRepo.Get().ToDictionary(
                    p => (p.HISParamCode ?? string.Empty).Trim(),
                    p => p.HISParamDescription,
                    StringComparer.OrdinalIgnoreCase);
            }

            if (equipmentNames.TryGetValue(m.EquipmentId, out var name))
            {
                m.GroupName = name;
            }

            if (string.IsNullOrWhiteSpace(m.HISParamDescription) &&
                !string.IsNullOrWhiteSpace(m.HISParamCode) &&
                paramNames.TryGetValue(m.HISParamCode.Trim(), out var paramName))
            {
                m.HISParamDescription = paramName;
            }
        }

        private bool ExistsDuplicate(TestMappingMaster item, int? excludeId)
        {
            var hisParam = (item.HISParamCode ?? string.Empty).Trim();
            var lisCode = (item.LISTestCode ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(hisParam) || string.IsNullOrEmpty(lisCode))
            {
                return false;
            }

            return Repo.Get(m =>
                    m.EquipmentId == item.EquipmentId &&
                    m.IsActive &&
                    (!excludeId.HasValue || m.Id != excludeId.Value))
                .AsEnumerable()
                .Any(m =>
                    string.Equals((m.HISParamCode ?? string.Empty).Trim(), hisParam, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals((m.LISTestCode ?? string.Empty).Trim(), lisCode, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class TestParameterMappingCrudManager : MasterCrudManager<TestParameterMappingMaster>, IMasterCrudManager<TestParameterMappingMaster>
    {
        private readonly ModuleRepo<HisTestMaster> testRepo;
        private readonly ModuleRepo<HISParameterMaster> parameterRepo;

        public TestParameterMappingCrudManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.HISTestCode, x => x.HISParamCode, x => x.IsActive, "HISTestCode")
        {
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, uow);
            parameterRepo = new ModuleRepo<HISParameterMaster>(logger, identity, uow);
        }

        public new long Add(TestParameterMappingMaster item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.HisTestId <= 0 || item.HisParameterId <= 0)
            {
                throw new InvalidOperationException("Test and Parameter are required.");
            }

            ValidateSequence(item);

            if (ExistsDuplicate(item, null))
            {
                throw new InvalidOperationException(BuildDuplicateMessage(item));
            }

            item.CreatedOn = DateTime.Now;
            item.CreatedBy = Identity?.ActivityMember;
            return base.Add(item);
        }

        public new void Update(TestParameterMappingMaster item)
        {
            ValidateSequence(item);

            if (ExistsDuplicate(item, item.Id))
            {
                throw new InvalidOperationException(BuildDuplicateMessage(item));
            }

            base.Update(item);
        }

        public new void Delete(TestParameterMappingMaster item)
        {
            var existing = Repo.Get(item.Id);
            if (existing != null)
            {
                existing.IsActive = false;
                Repo.Update(existing);
            }
        }

        public override ItemList<TestParameterMappingMaster> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var mappings = Repo.Get().ToList();
            Enrich(mappings);

            if (!string.IsNullOrWhiteSpace(option.SearchText))
            {
                var search = option.SearchText.Trim();
                mappings = mappings.Where(item =>
                    (!string.IsNullOrEmpty(item.HISTestCode) &&
                        item.HISTestCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(item.HISTestCodeDescription) &&
                        item.HISTestCodeDescription.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(item.HISParamCode) &&
                        item.HISParamCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(item.HISParamDescription) &&
                        item.HISParamDescription.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
            }

            var result = new ItemList<TestParameterMappingMaster>
            {
                TotalRecord = mappings.Count
            };
            var minRow = Math.Max(0, (option.CurrentPage - 1) * option.RecordPerPage);
            var pageSize = option.RecordPerPage <= 0 ? result.TotalRecord : option.RecordPerPage;

            result.Items = mappings
                .OrderBy(item => item.HISTestCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Sequence)
                .ThenBy(item => item.HISParamCode, StringComparer.OrdinalIgnoreCase)
                .Skip(minRow)
                .Take(pageSize)
                .ToList();

            return result;
        }

        private static void ValidateSequence(TestParameterMappingMaster item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.Sequence < 1)
            {
                throw new InvalidOperationException("Sequence must be greater than zero.");
            }
        }

        public override TestParameterMappingMaster GetById(int id)
        {
            var item = base.GetById(id);
            if (item != null)
            {
                Enrich(new[] { item });
            }

            return item;
        }

        private void Enrich(IEnumerable<TestParameterMappingMaster> items)
        {
            var tests = testRepo.Get().ToDictionary(t => t.Id, t => t);
            var parameters = parameterRepo.Get().ToDictionary(p => p.Id, p => p);
            foreach (var item in items)
            {
                if (tests.TryGetValue(item.HisTestId, out var test))
                {
                    item.HISTestCode = test.HISTestCode;
                    item.HISTestCodeDescription = test.HISTestCodeDescription;
                }

                if (parameters.TryGetValue(item.HisParameterId, out var param))
                {
                    item.HISParamCode = param.HISParamCode;
                    item.HISParamDescription = param.HISParamDescription;
                }
            }
        }

        private bool ExistsDuplicate(TestParameterMappingMaster item, int? excludeId)
        {
            return Repo.Get(m =>
                    m.IsActive &&
                    m.HisTestId == item.HisTestId &&
                    m.HisParameterId == item.HisParameterId &&
                    (!excludeId.HasValue || m.Id != excludeId.Value))
                .Any();
        }

        private string BuildDuplicateMessage(TestParameterMappingMaster item)
        {
            var test = testRepo.Get(item.HisTestId);
            var parameter = parameterRepo.Get(item.HisParameterId);
            var testLabel = test != null
                ? $"{test.HISTestCode} - {test.HISTestCodeDescription}"
                : $"Test Id {item.HisTestId}";
            var paramLabel = parameter != null
                ? $"{parameter.HISParamCode} - {parameter.HISParamDescription}"
                : $"Parameter Id {item.HisParameterId}";
            return $"A mapping already exists for Test \"{testLabel}\" and Parameter \"{paramLabel}\".";
        }
    }

    public class TestParameterCatalogManager
    {
        private readonly ModuleRepo<TestParameter> repo;

        public TestParameterCatalogManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
        {
            repo = new ModuleRepo<TestParameter>(logger, identity, uow);
        }

        public ItemList<TestParameter> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var result = new ItemList<TestParameter>();
            var query = repo.Get().AsEnumerable();

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                var search = option.SearchText.Trim();
                query = query.Where(p =>
                    (p.HISParamCode != null && p.HISParamCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.HISParamName != null && p.HISParamName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.HISTestCode != null && p.HISTestCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            var list = query.OrderByDescending(p => p.CreatedOn).ToList();
            result.TotalRecord = list.Count;
            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int pageSize = option.RecordPerPage == 0 ? result.TotalRecord : option.RecordPerPage;
            result.Items = list.Skip(minRow).Take(pageSize).ToList();
            return result;
        }

        public TestParameter GetById(long id)
        {
            return repo.Get(id);
        }

        public void Update(TestParameter item)
        {
            if (item == null || item.Id <= 0)
            {
                throw new ArgumentException("Invalid parameter record.");
            }

            var existing = repo.Get(item.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Parameter record not found.");
            }

            existing.HISParamCode = item.HISParamCode;
            existing.HISParamName = item.HISParamName;
            repo.Update(existing);
        }
    }

    public class PatientMasterManager
    {
        private readonly ModuleRepo<PatientDetail> repo;
        private readonly PatientVisitManager visitManager;

        public PatientMasterManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow, PatientVisitManager visitManager)
        {
            repo = new ModuleRepo<PatientDetail>(logger, identity, uow);
            this.visitManager = visitManager;
        }

        public ItemList<PatientDetail> Get(ListOptions option)
        {
            if (option == null)
            {
                return null;
            }

            var result = new ItemList<PatientDetail>();
            var query = repo.Get().AsEnumerable();

            if (!string.IsNullOrEmpty(option.SearchText))
            {
                var search = option.SearchText.Trim();
                query = query.Where(p =>
                    (p.Name != null && p.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.Phone != null && p.Phone.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.HisPatientId != null && p.HisPatientId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.MRNo != null && p.MRNo.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (p.VisitId != null && p.VisitId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            var list = query.OrderBy(p => p.Name).ToList();
            if (!string.IsNullOrEmpty(option.SearchText))
            {
                var visitPatientIds = visitManager.GetPatientIdsMatchingVisitSearch(option.SearchText.Trim()).ToList();
                if (visitPatientIds.Any())
                {
                    var existingIds = new HashSet<long>(list.Select(p => p.Id));
                    var extra = repo.Get()
                        .Where(p => p.IsActive && visitPatientIds.Contains(p.Id) && !existingIds.Contains(p.Id))
                        .OrderBy(p => p.Name)
                        .ToList();
                    if (extra.Any())
                    {
                        list = list.Concat(extra).OrderBy(p => p.Name).ToList();
                    }
                }
            }

            result.TotalRecord = list.Count;
            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int pageSize = option.RecordPerPage == 0 ? result.TotalRecord : option.RecordPerPage;
            result.Items = list.Skip(minRow).Take(pageSize).ToList();
            return result;
        }

        public PatientDetail GetById(long id)
        {
            return repo.Get(id);
        }

        public string GenerateNextPatientId()
        {
            return GenerateNextCode("PAT", 5, repo.Get().Select(p => p.HisPatientId));
        }

        public string GenerateNextMrNo()
        {
            return GenerateNextCode("MR", 5, repo.Get().Select(p => p.MRNo));
        }

        public string GenerateNextVisitId()
        {
            return visitManager.GenerateNextVisitId();
        }

        private static string GenerateNextCode(string prefix, int digits, IEnumerable<string> codes)
        {
            var max = 0;
            foreach (var code in codes.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                var trimmed = code.Trim();
                if (trimmed.Length > prefix.Length &&
                    trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(trimmed.Substring(prefix.Length), out var num) &&
                    num > max)
                {
                    max = num;
                }
            }

            return $"{prefix}{(max + 1).ToString($"D{digits}", System.Globalization.CultureInfo.InvariantCulture)}";
        }

        public long Add(PatientDetail item)
        {
            NormalizePatientFields(item);

            if (string.IsNullOrWhiteSpace(item.HisPatientId))
            {
                item.HisPatientId = GenerateNextPatientId();
            }

            if (string.IsNullOrWhiteSpace(item.MRNo))
            {
                item.MRNo = GenerateNextMrNo();
            }

            if (string.IsNullOrWhiteSpace(item.VisitId))
            {
                item.VisitId = GenerateNextVisitId();
            }

            ValidatePatientRequiredFields(item);

            if (ExistsDuplicateMrNo(item, null))
            {
                throw new InvalidOperationException("MR No already exists.");
            }

            if (ExistsDuplicateVisitId(item, null))
            {
                throw new InvalidOperationException("Visit ID already exists.");
            }

            if (ExistsDuplicatePatient(item, null))
            {
                throw new InvalidOperationException("Patient already exists.");
            }

            item.IsActive = true;
            item.CreatedOn = DateTime.Now;
            if (item.DateOfBirth == default(DateTime))
            {
                item.DateOfBirth = DateTime.Today;
            }

            if (item.Age <= 0 && item.DateOfBirth != default(DateTime))
            {
                item.Age = (decimal)item.DateOfBirth.Age();
            }

            var id = repo.Add(item);
            visitManager.CreateVisitForPatient(id, item.VisitId, null, VisitStatusType.New);
            return id;
        }

        public void Update(PatientDetail item)
        {
            if (item == null || item.Id <= 0)
            {
                throw new ArgumentException("Invalid patient record.");
            }

            NormalizePatientFields(item);
            ValidatePatientRequiredFields(item);

            var existing = repo.Get(item.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Patient record not found.");
            }

            if (ExistsDuplicateMrNo(item, item.Id))
            {
                throw new InvalidOperationException("MR No already exists.");
            }

            if (ExistsDuplicateVisitId(item, item.Id))
            {
                throw new InvalidOperationException("Visit ID already exists.");
            }

            if (ExistsDuplicatePatient(item, item.Id))
            {
                throw new InvalidOperationException("Patient already exists.");
            }

            existing.Name = item.Name;
            existing.PatientPrefix = item.PatientPrefix;
            existing.MRNo = item.MRNo;
            existing.VisitId = item.VisitId;
            existing.Phone = item.Phone;
            existing.Address = item.Address;
            existing.Gender = item.Gender;
            existing.Age = item.Age;
            existing.DateOfBirth = item.DateOfBirth;
            existing.IsActive = item.IsActive;

            repo.Update(existing);
        }

        private static void NormalizePatientFields(PatientDetail item)
        {
            if (item == null)
            {
                return;
            }

            item.PatientPrefix = string.IsNullOrWhiteSpace(item.PatientPrefix)
                ? null
                : item.PatientPrefix.Trim();
            item.MRNo = string.IsNullOrWhiteSpace(item.MRNo)
                ? null
                : item.MRNo.Trim();
            item.VisitId = string.IsNullOrWhiteSpace(item.VisitId)
                ? null
                : item.VisitId.Trim();
            item.Phone = string.IsNullOrWhiteSpace(item.Phone)
                ? item.Phone
                : item.Phone.Trim();
            item.Name = string.IsNullOrWhiteSpace(item.Name)
                ? item.Name
                : item.Name.Trim();
        }

        private static void ValidatePatientRequiredFields(PatientDetail item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (string.IsNullOrWhiteSpace(item.Phone))
            {
                throw new InvalidOperationException("Phone number is required.");
            }

            if (string.IsNullOrWhiteSpace(item.PatientPrefix))
            {
                throw new InvalidOperationException("Salutation is required.");
            }

            if (string.IsNullOrWhiteSpace(item.MRNo))
            {
                throw new InvalidOperationException("MR No is required.");
            }

            if (string.IsNullOrWhiteSpace(item.VisitId))
            {
                throw new InvalidOperationException("Visit ID is required.");
            }
        }

        private bool ExistsDuplicateMrNo(PatientDetail item, long? excludeId)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.MRNo))
            {
                return false;
            }

            var mrNo = item.MRNo.Trim();
            return repo.Get(p =>
                p.IsActive &&
                (!excludeId.HasValue || p.Id != excludeId.Value)).AsEnumerable()
                .Any(p =>
                    !string.IsNullOrWhiteSpace(p.MRNo) &&
                    p.MRNo.Trim().Equals(mrNo, StringComparison.OrdinalIgnoreCase));
        }

        private bool ExistsDuplicateVisitId(PatientDetail item, long? excludeId)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.VisitId))
            {
                return false;
            }

            var visitId = item.VisitId.Trim();
            return visitManager.ExistsVisitIdForOtherPatients(visitId, excludeId);
        }

        private bool ExistsDuplicatePatient(PatientDetail item, long? excludeId)
        {
            if (item == null)
            {
                return false;
            }

            var candidates = repo.Get(p =>
                p.IsActive &&
                (!excludeId.HasValue || p.Id != excludeId.Value)).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(item.HisPatientId))
            {
                var patientId = item.HisPatientId.Trim();
                if (candidates.Any(p =>
                    p.HisPatientId != null &&
                    p.HisPatientId.Trim().Equals(patientId, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            var phone = NormalizePatientPhone(item.Phone);
            var name = NormalizePatientName(item.Name);
            if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(name))
            {
                if (candidates.Any(p =>
                    NormalizePatientPhone(p.Phone) == phone &&
                    NormalizePatientName(p.Name) == name))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizePatientPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            return new string(phone.Where(char.IsDigit).ToArray());
        }

        private static string NormalizePatientName(string name)
        {
            return string.IsNullOrWhiteSpace(name)
                ? string.Empty
                : name.Trim().ToUpperInvariant();
        }

        public void Delete(PatientDetail item)
        {
            var existing = repo.Get(item.Id);
            if (existing != null)
            {
                existing.IsActive = false;
                repo.Update(existing);
            }
        }

        public IEnumerable<PatientDetail> GetAllActive()
        {
            return repo.Get(p => p.IsActive).OrderBy(p => p.Name).ToList();
        }
    }
}
