using LIS.BusinessLogic;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.TestResultEdit;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Workflows
{
    [TestClass]
    public class TestResultEditManagerTests : IntegrationTestBase
    {
        private TestResultEditManager CreateManager()
        {
            return new TestResultEditManager(Services.Logger, Services.Identity, Services.Uow);
        }

        private (string sampleNo, long testResultId, long testRequestId, long detailId) EnsureEditableSample()
        {
            var row = Services.Db.TestResults
                .Join(Services.Db.TestRequestDetails,
                    tr => tr.TestRequestId,
                    req => req.Id,
                    (tr, req) => new { tr, req })
                .Where(x => x.req.ReportStatus == ReportStatusType.ReportGenerated
                    || x.req.ReportStatus == ReportStatusType.TechnicianApproved)
                .OrderByDescending(x => x.tr.Id)
                .FirstOrDefault();

            if (row == null)
            {
                Assert.Inconclusive("No editable test results in database.");
            }

            var detail = Services.Db.TestResultDetails
                .Where(d => d.TestResultId == row.tr.Id)
                .OrderBy(d => d.Id)
                .FirstOrDefault();

            if (detail == null)
            {
                Assert.Inconclusive("No test result detail rows in database.");
            }

            return (row.tr.SampleNo, row.tr.Id, row.req.Id, detail.Id);
        }

        [TestMethod]
        public void Search_By_SampleNo_Returns_Row_With_Results()
        {
            var sample = EnsureEditableSample();
            var manager = CreateManager();

            var rows = manager.Search(new TestResultEditSearchOptions { SampleNo = sample.sampleNo });

            Assert.IsTrue(rows.Any(r =>
                r.SampleNo != null &&
                r.SampleNo.Equals(sample.sampleNo, StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(rows.First(r =>
                r.SampleNo.Equals(sample.sampleNo, StringComparison.OrdinalIgnoreCase)).HasResults);
        }

        [TestMethod]
        public void Search_Combines_Criteria_With_Or_Not_And()
        {
            var sample = EnsureEditableSample();
            var manager = CreateManager();

            // Sample No matches, Patient Name is deliberately non-matching.
            // With AND this would return nothing; with OR the sample must still appear.
            var rows = manager.Search(new TestResultEditSearchOptions
            {
                SampleNo = sample.sampleNo,
                PatientName = "__no_such_patient_" + Guid.NewGuid().ToString("N")
            });

            Assert.IsTrue(
                rows.Any(r => r.SampleNo != null &&
                    r.SampleNo.Equals(sample.sampleNo, StringComparison.OrdinalIgnoreCase)),
                "OR search should return the sample matched by Sample No even when the Patient Name term does not match.");
        }

        [TestMethod]
        public void GetBySampleNo_Loads_Parameters_And_Allows_Edit_When_ReportGenerated()
        {
            var sample = EnsureEditableSample();
            var manager = CreateManager();

            var dto = manager.GetBySampleNo(sample.sampleNo, isAdministrator: true);

            Assert.IsNotNull(dto);
            Assert.IsFalse(string.IsNullOrEmpty(dto.PatientName));
            Assert.IsTrue(dto.Tests.Any());
            var test = dto.Tests.First(t => t.TestResultId == sample.testResultId);
            Assert.IsTrue(test.Parameters.Any());
            Assert.IsTrue(test.CanEdit || dto.Tests.Any(t => t.CanEdit));
        }

        [TestMethod]
        public void Save_Updates_Detail_Without_Duplicate_Rows()
        {
            var sample = EnsureEditableSample();
            var manager = CreateManager();
            var before = Services.Db.TestResultDetails.Count(d => d.TestResultId == sample.testResultId);

            var loaded = manager.GetBySampleNo(sample.sampleNo, true);
            var test = loaded.Tests.First(t => t.TestResultId == sample.testResultId);
            var param = test.Parameters.First();
            var newValue = (param.ResultValue ?? "0") + "1";

            var save = manager.Save(new TestResultEditSaveRequest
            {
                TestResultId = test.TestResultId,
                TestRequestId = test.TestRequestId,
                Parameters = new[]
                {
                    new TestResultEditParameterSaveDto
                    {
                        DetailId = param.DetailId,
                        ResultValue = newValue.Length > 20 ? newValue.Substring(0, 20) : newValue
                    }
                }
            }, true);

            Assert.IsTrue(save.Success);
            Assert.AreEqual(before, Services.Db.TestResultDetails.Count(d => d.TestResultId == sample.testResultId));

            var reloaded = manager.GetBySampleNo(sample.sampleNo, true);
            var updated = reloaded.Tests.First(t => t.TestResultId == sample.testResultId)
                .Parameters.First(p => p.DetailId == param.DetailId);
            Assert.AreEqual(newValue.Length > 20 ? newValue.Substring(0, 20) : newValue, updated.ResultValue);
        }

        [TestMethod]
        public void Manual_Entry_Creates_Results_For_Sample_Without_Analyzer_Data()
        {
            var req = Services.Db.TestRequestDetails
                .Where(r => r.ReportStatus == ReportStatusType.New && r.SampleNo != null)
                .OrderByDescending(r => r.Id)
                .FirstOrDefault(r => !Services.Db.TestResults.Any(tr => tr.TestRequestId == r.Id));

            if (req == null)
            {
                Assert.Inconclusive("No New sample without analyzer results in database.");
            }

            var param = Services.Db.HISParameterMaster
                .FirstOrDefault(p => p.HISTestCode == req.HISTestCode);

            if (param == null)
            {
                var testMaster = Services.Db.HisTestMaster
                    .FirstOrDefault(t => t.HISTestCode == req.HISTestCode);
                if (testMaster != null)
                {
                    var mappedParamId = Services.Db.TestParameterMappingMaster
                        .Where(m => m.IsActive && m.HisTestId == testMaster.Id)
                        .Select(m => m.HisParameterId)
                        .FirstOrDefault();
                    if (mappedParamId > 0)
                    {
                        param = Services.Db.HISParameterMaster.FirstOrDefault(p => p.Id == mappedParamId);
                    }
                }
            }

            if (param == null)
            {
                Assert.Inconclusive($"No parameters configured for test {req.HISTestCode}.");
            }

            var manager = CreateManager();
            var loaded = manager.GetBySampleNo(req.SampleNo, true);
            var test = loaded.Tests.First(t => t.TestRequestId == req.Id);
            Assert.AreEqual(0L, test.TestResultId);
            Assert.IsTrue(test.Parameters.Any());
            Assert.IsTrue(test.CanEdit);

            var save = manager.Save(new TestResultEditSaveRequest
            {
                TestResultId = 0,
                TestRequestId = req.Id,
                Parameters = new[]
                {
                    new TestResultEditParameterSaveDto
                    {
                        ParameterCode = param.HISParamCode ?? param.LISParamCode,
                        ResultValue = "12.5"
                    }
                }
            }, true);

            Assert.IsTrue(save.Success);
            Assert.IsTrue(Services.Db.TestResults.Any(tr => tr.TestRequestId == req.Id));
            var reloaded = manager.GetBySampleNo(req.SampleNo, true);
            var savedTest = reloaded.Tests.First(t => t.TestRequestId == req.Id);
            Assert.IsTrue(savedTest.TestResultId > 0);
            Assert.IsTrue(savedTest.Parameters.Any(p => p.ResultValue == "12.5"));
        }

        [TestMethod]
        public void GetBySampleNo_Scaffolds_Entry_When_Result_Shell_Has_No_Details()
        {
            var req = Services.Db.TestRequestDetails
                .Where(r => r.ReportStatus == ReportStatusType.New && r.SampleNo != null)
                .OrderByDescending(r => r.Id)
                .FirstOrDefault(r => !Services.Db.TestResultDetails.Any(d =>
                    Services.Db.TestResults.Any(tr => tr.TestRequestId == r.Id && tr.Id == d.TestResultId)));

            if (req == null)
            {
                Assert.Inconclusive("No suitable sample for empty-shell entry test.");
            }

            var param = Services.Db.HISParameterMaster
                .FirstOrDefault(p => p.HISTestCode == req.HISTestCode);
            if (param == null)
            {
                var testMaster = Services.Db.HisTestMaster
                    .FirstOrDefault(t => t.HISTestCode == req.HISTestCode);
                if (testMaster != null)
                {
                    var mappedParamId = Services.Db.TestParameterMappingMaster
                        .Where(m => m.IsActive && m.HisTestId == testMaster.Id)
                        .Select(m => m.HisParameterId)
                        .FirstOrDefault();
                    if (mappedParamId > 0)
                    {
                        param = Services.Db.HISParameterMaster.FirstOrDefault(p => p.Id == mappedParamId);
                    }
                }
            }

            if (param == null)
            {
                Assert.Inconclusive($"No parameters configured for test {req.HISTestCode}.");
            }

            var existing = Services.Db.TestResults.FirstOrDefault(tr => tr.TestRequestId == req.Id);
            if (existing == null)
            {
                existing = new LIS.DtoModel.Models.TestResult
                {
                    PatientId = req.PatientId,
                    HISTestCode = req.HISTestCode,
                    LISTestCode = req.HISTestCode,
                    SampleNo = req.SampleNo,
                    TestRequestId = req.Id,
                    ResultDate = DateTime.Now,
                    CreatedBy = "test",
                    CreatedOn = DateTime.Now
                };
                Services.Db.TestResults.Add(existing);
                Services.Db.SaveChanges();
            }
            else
            {
                var staleDetails = Services.Db.TestResultDetails.Where(d => d.TestResultId == existing.Id).ToList();
                foreach (var detail in staleDetails)
                {
                    Services.Db.TestResultDetails.Remove(detail);
                }
                Services.Db.SaveChanges();
            }

            var manager = CreateManager();
            var dto = manager.GetBySampleNo(req.SampleNo, true);
            var test = dto.Tests.First(t => t.TestRequestId == req.Id);

            Assert.AreEqual(0L, test.TestResultId);
            Assert.IsTrue(test.Parameters.Any());
            Assert.IsTrue(test.CanEdit);
        }

        [TestMethod]
        public void Manual_Entry_Fills_Empty_Result_Shell()
        {
            var req = Services.Db.TestRequestDetails
                .Where(r => r.ReportStatus == ReportStatusType.New && r.SampleNo != null)
                .OrderByDescending(r => r.Id)
                .FirstOrDefault();

            if (req == null)
            {
                Assert.Inconclusive("No New sample in database.");
            }

            var param = Services.Db.HISParameterMaster
                .FirstOrDefault(p => p.HISTestCode == req.HISTestCode);
            if (param == null)
            {
                var testMaster = Services.Db.HisTestMaster
                    .FirstOrDefault(t => t.HISTestCode == req.HISTestCode);
                if (testMaster != null)
                {
                    var mappedParamId = Services.Db.TestParameterMappingMaster
                        .Where(m => m.IsActive && m.HisTestId == testMaster.Id)
                        .Select(m => m.HisParameterId)
                        .FirstOrDefault();
                    if (mappedParamId > 0)
                    {
                        param = Services.Db.HISParameterMaster.FirstOrDefault(p => p.Id == mappedParamId);
                    }
                }
            }

            if (param == null)
            {
                Assert.Inconclusive($"No parameters configured for test {req.HISTestCode}.");
            }

            var shell = Services.Db.TestResults.FirstOrDefault(tr => tr.TestRequestId == req.Id);
            if (shell == null)
            {
                shell = new LIS.DtoModel.Models.TestResult
                {
                    PatientId = req.PatientId,
                    HISTestCode = req.HISTestCode,
                    LISTestCode = req.HISTestCode,
                    SampleNo = req.SampleNo,
                    TestRequestId = req.Id,
                    ResultDate = DateTime.Now,
                    CreatedBy = "test",
                    CreatedOn = DateTime.Now
                };
                Services.Db.TestResults.Add(shell);
                Services.Db.SaveChanges();
            }
            else
            {
                var staleDetails = Services.Db.TestResultDetails.Where(d => d.TestResultId == shell.Id).ToList();
                foreach (var detail in staleDetails)
                {
                    Services.Db.TestResultDetails.Remove(detail);
                }
                Services.Db.SaveChanges();
            }

            var manager = CreateManager();
            var save = manager.Save(new TestResultEditSaveRequest
            {
                TestResultId = 0,
                TestRequestId = req.Id,
                Parameters = new[]
                {
                    new TestResultEditParameterSaveDto
                    {
                        ParameterCode = param.HISParamCode ?? param.LISParamCode,
                        ResultValue = "7.25"
                    }
                }
            }, true);

            Assert.IsTrue(save.Success);
            Assert.IsTrue(Services.Db.TestResultDetails.Any(d => d.TestResultId == shell.Id && d.ParamValue == "7.25"));
        }

        [TestMethod]
        public void Save_Blocked_When_DoctorApproved()
        {
            var row = Services.Db.TestResults
                .Join(Services.Db.TestRequestDetails,
                    tr => tr.TestRequestId,
                    req => req.Id,
                    (tr, req) => new { tr, req })
                .Where(x => x.req.ReportStatus == ReportStatusType.DoctorApproved)
                .OrderByDescending(x => x.tr.Id)
                .FirstOrDefault();

            if (row == null)
            {
                Assert.Inconclusive("No doctor-approved sample in database.");
            }

            var manager = CreateManager();
            var dto = manager.GetBySampleNo(row.tr.SampleNo, true);
            var test = dto.Tests.First(t => t.TestResultId == row.tr.Id);
            Assert.IsFalse(test.CanEdit);

            var detail = test.Parameters.First();
            Assert.ThrowsException<InvalidOperationException>(() => manager.Save(
                new TestResultEditSaveRequest
                {
                    TestResultId = test.TestResultId,
                    TestRequestId = test.TestRequestId,
                    Parameters = new[]
                    {
                        new TestResultEditParameterSaveDto { DetailId = detail.DetailId, ResultValue = "999" }
                    }
                }, true));
        }
    }
}
