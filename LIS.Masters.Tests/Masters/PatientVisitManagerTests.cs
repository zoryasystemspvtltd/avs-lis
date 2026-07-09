using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class PatientVisitManagerTests : IntegrationTestBase
    {
        [TestMethod]
        public void StartVisit_Creates_New_Visit_And_Updates_Current_Visit_On_Patient()
        {
            var suffix = UniqueCode("VIS");
            var patient = MasterTestDataBuilder.Patient(suffix);
            var patientId = Services.PatientMaster.Add(patient);
            var before = Services.PatientMaster.GetById(patientId);
            var originalVisitId = before.VisitId;

            var visit = Services.PatientVisit.StartVisit(patientId);
            Assert.IsNotNull(visit);
            Assert.IsTrue(visit.PatientVisitId > 0);
            Assert.AreNotEqual(originalVisitId, visit.VisitId);

            var after = Services.PatientMaster.GetById(patientId);
            Assert.AreEqual(visit.VisitId, after.VisitId);

            var history = Services.PatientVisit.GetVisitHistory(patientId).ToList();
            Assert.IsTrue(history.Count >= 2);
            Assert.IsTrue(history.Any(v => v.VisitId == originalVisitId));
            Assert.IsTrue(history.Any(v => v.VisitId == visit.VisitId));
        }

        [TestMethod]
        public void GetCurrentVisit_Returns_Latest_Current_Visit()
        {
            var suffix = UniqueCode("CUR");
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(suffix));
            var newVisit = Services.PatientVisit.StartVisit(patientId);

            var current = Services.PatientVisit.GetCurrentVisit(patientId);
            Assert.IsNotNull(current);
            Assert.AreEqual(newVisit.PatientVisitId, current.PatientVisitId);
            Assert.AreEqual(newVisit.VisitId, current.VisitId);
        }

        [TestMethod]
        public void Search_By_Historical_VisitId_Finds_Patient()
        {
            var suffix = UniqueCode("HIS");
            var patientId = Services.PatientMaster.Add(MasterTestDataBuilder.Patient(suffix));
            var originalVisitId = Services.PatientMaster.GetById(patientId).VisitId;
            Services.PatientVisit.StartVisit(patientId);

            var search = Services.PatientMaster.Get(ListOptionsFactory.Create(search: originalVisitId));
            Assert.IsTrue(search.Items.Any(p => p.Id == patientId));
        }
    }
}
