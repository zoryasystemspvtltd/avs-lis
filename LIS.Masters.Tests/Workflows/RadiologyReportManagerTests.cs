using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

        [TestMethod]
        public void GetReport_Includes_Patient_Header_Fields()
        {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(suffix));
            var patient = Services.PatientMaster.GetById(patientId);
            var invoiceNo = UniqueCode("RAD");

            var requestId = Services.RadiologyReport.CreateRequest(new RadiologyRequestDetail
            {
                PatientId = patientId,
                HISRequestNo = invoiceNo,
                HISTestCode = "RAD-UT",
                HISTestName = "Radiology Header Test",
                Modality = "X-Ray",
                Department = "Radiology"
            });

            Services.RadiologyReport.SaveReport(new RadiologyReportSaveRequest
            {
                RadiologyRequestId = requestId,
                ClinicalHistory = "History",
                Findings = "Findings",
                Impression = "Impression",
                SubmitForReview = false
            });

            var detail = Services.RadiologyReport.GetReport(requestId);
            Assert.AreEqual(patient.Name, detail.PatientName);
            Assert.AreEqual(invoiceNo, detail.InvoiceNo);
            Assert.AreEqual(patient.Age, detail.Age);
            Assert.AreEqual(patient.Gender, detail.Gender);
            Assert.IsTrue(detail.ResultDate.HasValue);
        }
    }
}
