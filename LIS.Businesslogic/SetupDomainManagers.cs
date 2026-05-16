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

        public HisParameterMasterManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.HISParamCode, x => x.HISParamDescription, x => true, "HISParamCode")
        {
            testRepo = new ModuleRepo<HisTestMaster>(logger, identity, uow);
        }

        public new long Add(HISParameterMaster item)
        {
            item.CreatedOn = DateTime.Now;
            return base.Add(item);
        }

        public new void Update(HISParameterMaster item)
        {
            base.Update(item);
        }

        public new void Delete(HISParameterMaster item)
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
        public HisParameterRangeCrudManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork uow)
            : base(logger, identity, uow, x => x.HISRangeCode, x => x.HISRangeValue, x => true, "HISRangeCode") { }

        public new long Add(HISParameterRangMaster item)
        {
            item.CreatedOn = DateTime.Now;
            return base.Add(item);
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

        public long Add(PatientDetail item)
        {
            item.IsActive = true;
            item.CreatedOn = DateTime.Now;
            return repo.Add(item);
        }

        public void Update(PatientDetail item)
        {
            repo.Update(item);
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
