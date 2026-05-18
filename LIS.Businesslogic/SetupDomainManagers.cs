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
    public class HisParameterMasterManager : MasterCrudManager<HISParameterMaster>, IMasterCrudManager<HISParameterMaster>
    {
        private readonly ModuleRepo<HisTestMaster> testRepo;
        private readonly ModuleRepo<HISParameterRangMaster> rangeRepo;

        public HisParameterMasterManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.HISParamCode, x => x.HISParamDescription, x => true, "HISParamCode")
        {
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, uow);
            rangeRepo = new ModuleRepo<HISParameterRangMaster>(logger, identity, uow);
        }

        public new long Add(HISParameterMaster item)
        {
            if (ExistsDuplicate(item, null))
            {
                throw new InvalidOperationException("A parameter with this code already exists for the selected test.");
            }

            item.CreatedOn = DateTime.Now;
            if (item.HisTestId > 0 && string.IsNullOrEmpty(item.HISTestCode))
            {
                var test = testRepo.Get(item.HisTestId);
                if (test != null)
                {
                    item.HISTestCode = test.HISTestCode;
                }
            }

            return base.Add(item);
        }

        public new void Update(HISParameterMaster item)
        {
            if (ExistsDuplicate(item, item.Id))
            {
                throw new InvalidOperationException("A parameter with this code already exists for the selected test.");
            }

            if (item.HisTestId > 0)
            {
                var test = testRepo.Get(item.HisTestId);
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

        private bool ExistsDuplicate(HISParameterMaster item, int? excludeId)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.HISParamCode))
            {
                return false;
            }

            return Repo.Get(p =>
                p.HisTestId == item.HisTestId &&
                p.HISParamCode == item.HISParamCode &&
                (!excludeId.HasValue || p.Id != excludeId.Value)).Any();
        }

        public override ItemList<HISParameterMaster> Get(ListOptions option)
        {
            var result = base.Get(option);
            if (result?.Items != null)
            {
                Enrich(result.Items);
            }

            return result;
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
                if (tests.TryGetValue(p.HisTestId, out var test))
                {
                    p.HISTestCode = p.HISTestCode ?? test.HISTestCode;
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

        public new long Add(HISParameterRangMaster item)
        {
            item.CreatedOn = DateTime.Now;
            return base.Add(item);
        }

        public override ItemList<HISParameterRangMaster> Get(ListOptions option)
        {
            var result = base.Get(option);
            if (result?.Items != null)
            {
                Enrich(result.Items);
            }

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

        public new void Delete(HISParameterRangMaster item)
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

        public TestMappingCrudManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.LISTestCode, x => x.HISTestCodeDescription, x => x.IsActive, "HISTestCode")
        {
            equipmentRepo = new ModuleRepo<EquipmentMaster>(logger, identity, uow);
        }

        public new long Add(TestMappingMaster item)
        {
            item.CreatedOn = DateTime.Now;
            item.CreatedBy = Identity?.ActivityMember;
            if (ExistsDuplicate(item, null))
            {
                throw new InvalidOperationException("Duplicate mapping for this equipment and LIS test code.");
            }

            return base.Add(item);
        }

        public new void Update(TestMappingMaster item)
        {
            if (ExistsDuplicate(item, item.Id))
            {
                throw new InvalidOperationException("Duplicate mapping for this equipment and LIS test code.");
            }

            base.Update(item);
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
                    (m.HISTestCode != null && m.HISTestCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (m.LISTestCode != null && m.LISTestCode.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (m.HISTestCodeDescription != null && m.HISTestCodeDescription.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            var list = query.ToList();
            var equipmentNames = equipmentRepo.Get().ToDictionary(e => e.Id, e => e.Name);
            foreach (var m in list)
            {
                if (equipmentNames.TryGetValue(m.EquipmentId, out var name))
                {
                    m.GroupName = name;
                }
            }

            result.TotalRecord = list.Count;
            var sortColumn = string.IsNullOrEmpty(option.SortColumnName) ? "HISTestCode" : option.SortColumnName;
            int minRow = (option.CurrentPage - 1) * option.RecordPerPage;
            int pageSize = option.RecordPerPage == 0 ? result.TotalRecord : option.RecordPerPage;

            result.Items = list
                .OrderBy(sortColumn, option.SortDirection)
                .Skip(minRow)
                .Take(pageSize)
                .ToList();

            return result;
        }

        private bool ExistsDuplicate(TestMappingMaster item, int? excludeId)
        {
            return Repo.Get(m =>
                m.EquipmentId == item.EquipmentId &&
                m.LISTestCode == item.LISTestCode &&
                m.IsActive &&
                (!excludeId.HasValue || m.Id != excludeId.Value)).Any();
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

        public PatientMasterManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
        {
            repo = new ModuleRepo<PatientDetail>(logger, identity, uow);
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
                    (p.HisPatientId != null && p.HisPatientId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            var list = query.OrderBy(p => p.Name).ToList();
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
            var count = repo.Get().Count() + 1;
            return $"PAT{count:D5}";
        }

        public long Add(PatientDetail item)
        {
            if (string.IsNullOrWhiteSpace(item.HisPatientId))
            {
                item.HisPatientId = GenerateNextPatientId();
            }

            if (ExistsDuplicatePatient(item, null))
            {
                throw new InvalidOperationException("A patient with this patient ID already exists.");
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

            return repo.Add(item);
        }

        public void Update(PatientDetail item)
        {
            if (ExistsDuplicatePatient(item, item.Id))
            {
                throw new InvalidOperationException("A patient with this external ID already exists.");
            }

            repo.Update(item);
        }

        private bool ExistsDuplicatePatient(PatientDetail item, long? excludeId)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.HisPatientId))
            {
                return false;
            }

            return repo.Get(p =>
                p.HisPatientId == item.HisPatientId &&
                p.IsActive &&
                (!excludeId.HasValue || p.Id != excludeId.Value)).Any();
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
