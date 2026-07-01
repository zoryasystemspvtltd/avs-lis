using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LIS.Masters.Tests.Workflows
{
    [TestClass]
    public class RadiologyReportManagerTests : IntegrationTestBase
    {
        [TestMethod]
        public void Doctor_Approval_Queue_Excludes_Entry_Pending_Items()
        {
            var pending = Services.RadiologyReport.GetPendingQueue(new SampleWorkflowSearchOptions
            {
                RecordPerPage = 500,
                CurrentPage = 1
            });
            var doctor = Services.RadiologyReport.GetDoctorApprovalQueue(new SampleWorkflowSearchOptions
            {
                RecordPerPage = 500,
                CurrentPage = 1
            });

            Assert.IsNotNull(pending);
            Assert.IsNotNull(doctor);
            foreach (var row in pending.Items ?? Enumerable.Empty<RadiologyQueueRow>())
            {
                Assert.AreNotEqual("Under Review", row.Status);
            }
            foreach (var row in doctor.Items ?? Enumerable.Empty<RadiologyQueueRow>())
            {
                Assert.AreEqual("Under Review", row.Status);
            }
        }
    }
}
