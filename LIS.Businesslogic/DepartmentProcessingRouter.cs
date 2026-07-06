using LIS.DataAccess.Repo;
using LIS.DtoModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LIS.BusinessLogic
{
    /// <summary>
    /// Single source of truth for laboratory vs diagnostic routing decisions.
    /// Uses Department.ProcessingCategory only — never name/code heuristics or UI line types.
    /// </summary>
    public class DepartmentProcessingRouter
    {
        private readonly ModuleRepo<Departments> departmentRepo;
        private readonly Dictionary<string, Departments> cache =
            new Dictionary<string, Departments>(StringComparer.OrdinalIgnoreCase);

        public DepartmentProcessingRouter(ModuleRepo<Departments> departmentRepo)
        {
            this.departmentRepo = departmentRepo ?? throw new ArgumentNullException(nameof(departmentRepo));
        }

        public Departments ResolveDepartment(string departmentCode)
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
            {
                return null;
            }

            var code = departmentCode.Trim();
            if (cache.TryGetValue(code, out var cached))
            {
                return cached;
            }

            var dept = departmentRepo.Get(d =>
                    d.Code != null && d.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (dept != null)
            {
                cache[code] = dept;
            }

            return dept;
        }

        public string GetProcessingCategory(string departmentCode)
        {
            var dept = ResolveDepartment(departmentCode);
            return DepartmentProcessingCategories.Normalize(dept?.ProcessingCategory);
        }

        public string GetProcessingCategory(HisTestMaster test)
        {
            if (test == null)
            {
                return DepartmentProcessingCategories.Laboratory;
            }

            return GetProcessingCategory(test.DepartmentCode);
        }

        public bool IsDiagnosticTest(HisTestMaster test)
        {
            return DepartmentProcessingCategories.IsDiagnostic(GetProcessingCategory(test));
        }

        public bool IsLaboratoryTest(HisTestMaster test)
        {
            return !IsDiagnosticTest(test);
        }

        public void EnsureLaboratoryTest(HisTestMaster test, string profileContext)
        {
            if (test == null)
            {
                throw new InvalidOperationException("Test is required.");
            }

            if (IsDiagnosticTest(test))
            {
                var testLabel = !string.IsNullOrWhiteSpace(test.HISTestCodeDescription)
                    ? test.HISTestCodeDescription
                    : test.HISTestCode;
                throw new InvalidOperationException(
                    $"Test '{testLabel}' belongs to a Diagnostic department and cannot be used in {profileContext}.");
            }
        }
    }
}
