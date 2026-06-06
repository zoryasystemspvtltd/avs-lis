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
