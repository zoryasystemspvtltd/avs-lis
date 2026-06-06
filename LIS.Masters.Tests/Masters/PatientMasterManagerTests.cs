using LIS.DtoModel.Models;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace LIS.Masters.Tests.Masters
{
    [TestClass]
    public class PatientMasterManagerTests : IntegrationTestBase
    {
        [TestMethod]
        public void Patient_Create_Edit_Search_Deactivate_No_Duplicate()
        {
            var suffix = UniqueCode("PAT");
            var patient = MasterTestDataBuilder.Patient(suffix);
            var id = Services.PatientMaster.Add(patient);
            Assert.IsTrue(id > 0);

            var loaded = Services.PatientMaster.GetById(id);
            Assert.IsFalse(string.IsNullOrEmpty(loaded.HisPatientId));
            Assert.IsTrue(loaded.HisPatientId.StartsWith("PAT"));
            var originalHisId = loaded.HisPatientId;

            loaded.Name = "Updated " + suffix;
            loaded.Phone = "1111111111";
            Services.PatientMaster.Update(loaded);

            var updated = Services.PatientMaster.GetById(id);
            Assert.AreEqual("Updated " + suffix, updated.Name);
            Assert.AreEqual(originalHisId, updated.HisPatientId, "Edit must update same record, not create duplicate patient id");

            var search = Services.PatientMaster.Get(ListOptionsFactory.Create(search: suffix));
            Assert.IsTrue(search.Items.Any(p => p.Id == id));

            var countBefore = Services.Db.PatientDetails.Count(p => p.HisPatientId == originalHisId);
            Services.PatientMaster.Update(updated);
            var countAfter = Services.Db.PatientDetails.Count(p => p.HisPatientId == originalHisId);
            Assert.AreEqual(countBefore, countAfter, "Second update must not create duplicate row");

            Services.PatientMaster.Delete(new PatientDetail { Id = id });
            Assert.IsFalse(Services.PatientMaster.GetAllActive().Any(p => p.Id == id));
        }

        [TestMethod]
        public void Patient_Duplicate_HisPatientId_Throws_On_Create()
        {
            var suffix = UniqueCode("DUP");
            var patient = MasterTestDataBuilder.Patient(suffix, "PAT-DUP-TEST");
            var id = Services.PatientMaster.Add(patient);

            var duplicate = MasterTestDataBuilder.Patient("dup2", "PAT-DUP-TEST");
            Assert.ThrowsException<System.InvalidOperationException>(() => Services.PatientMaster.Add(duplicate));

            Services.PatientMaster.Delete(new PatientDetail { Id = id });
        }

        [TestMethod]
        public void Patient_Duplicate_Name_And_Phone_Throws_On_Create()
        {
            var suffix = UniqueCode("PHN");
            var patient = MasterTestDataBuilder.Patient(suffix);
            patient.Phone = "9000000001";
            var id = Services.PatientMaster.Add(patient);

            var duplicate = MasterTestDataBuilder.Patient("other");
            duplicate.Phone = "900-000-0001";
            duplicate.Name = patient.Name;
            Assert.ThrowsException<System.InvalidOperationException>(() => Services.PatientMaster.Add(duplicate));

            Services.PatientMaster.Delete(new PatientDetail { Id = id });
        }
    }
}
