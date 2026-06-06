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
    public class EquipmentTestMappingManager : IEquipmentTestMappingManager
    {
        private ILogger logger;
        private ModuleRepo<TestMappingMaster> repo;
        private ModuleRepo<EquipmentMaster> equipmentRepo;
        private ModuleRepo<HisTestMaster> hisTestRepo;
        private IModuleIdentity identity;
        private IFileHandler file;
        private GenericUnitOfWork genericUnitOfWork;

        public EquipmentTestMappingManager(ILogger Logger
            , IModuleIdentity identity
            , IFileHandler file
            , GenericUnitOfWork genericUnitOfWork)
        {
            this.identity = identity;
            this.file = file;
            this.logger = Logger;
            this.genericUnitOfWork = genericUnitOfWork;
            repo = new ModuleRepo<TestMappingMaster>(logger, this.identity, this.genericUnitOfWork);
            equipmentRepo = new ModuleRepo<EquipmentMaster>(logger, this.identity, this.genericUnitOfWork);
            hisTestRepo = new ModuleRepo<HisTestMaster>(logger, this.identity, this.genericUnitOfWork);
        }

        public IEnumerable<TestPanelMapping> Get(int equeipmentId)
        {
            List<TestPanelMapping> mappings = new List<TestPanelMapping>();

            var equipment = equipmentRepo.Get(equeipmentId);

            if (equipment != null)
            {
                //var lisTestList = this.Get(equipment.Model);
                var hisTestList = hisTestRepo.Get();

                var existingmappings = repo.Get(p => p.EquipmentId == equeipmentId);

                foreach (var test in hisTestList)
                {
                    var mappinglist = existingmappings
                                        .Where(p => p.HISTestCode.Equals(test.HISTestCode))
                                        .GroupBy(n => new
                                        {
                                            n.HISTestCode,
                                            n.HISTestCodeDescription,
                                            n.IsActive,
                                            n.EquipmentId
                                        },
                                        (k, g) => new TestPanelMapping()
                                        {
                                            HISTestCode = k.HISTestCode,
                                            HISTestCodeDescription = k.HISTestCodeDescription,
                                            IsActive = k.IsActive,
                                            EquipmentId = k.EquipmentId,
                                            LisTests = g.Select(t => new TestNameItem()
                                            {
                                                Id = t.Id,
                                                Code = t.LISTestCode,
                                                Description = t.LISTestCodeDescription
                                            }).ToList()
                                        }
                                        )
                                        .ToList();

                    if (mappinglist != null && mappinglist.Count > 0)
                    {
                        foreach (var mapping in mappinglist)
                        {
                            if (mapping.LisTests == null || mapping.LisTests.Count == 0)
                            {
                                mapping.IsActive = false;
                            }
                            mapping.DepartmentCode = test.DepartmentCode;
                            mappings.Add(mapping);
                        }
                    }
                    else
                    {
                        mappings.Add(new TestPanelMapping()
                        {
                            DepartmentCode = test.DepartmentCode,
                            EquipmentId = equipment.Id,
                            HISTestCode = test.HISTestCode,
                            HISTestCodeDescription = $"{test.HISTestCode} - {test.HISTestCodeDescription}"
                        });
                    }
                }

            }

            return mappings;
        }

        public string[] Get()
        {
            var model = file.GetModels();
            return model;
        }

        public List<TestNameItem> Get(string model)
        {
            var availableTest = file.GetJsonMappings(model);
            return availableTest;
        }

        public void Save(int equipmentId, TestMappingMaster[] mappings)
        {
            var mappingIds = mappings.Select(p => p.Id).ToList();
            repo.Delete(p => p.EquipmentId == equipmentId && !mappingIds.Contains(p.Id));

            foreach (var mapping in mappings)
            {
                var hisTest = hisTestRepo.Get(p => p.HISTestCode.Equals(mapping.HISTestCode)).FirstOrDefault();
                if (mapping.Id == 0)
                {
                    mapping.EquipmentId = equipmentId;
                    mapping.SpecimenCode = hisTest.HISSpecimenCode;
                    mapping.SpecimenName = hisTest.HISSpecimenName;
                    repo.Add(mapping);
                }
                else
                {
                    var map = repo.Get(mapping.Id);
                    map.EquipmentId = equipmentId;
                    map.SpecimenCode = hisTest.HISSpecimenCode;
                    map.SpecimenName = hisTest.HISSpecimenName;
                    map.LISTestCode = mapping.LISTestCode;
                    map.LISTestCodeDescription = mapping.LISTestCodeDescription;
                    map.IsActive = mapping.IsActive;
                    repo.Update(map);
                }

            }
        }
    }
}
