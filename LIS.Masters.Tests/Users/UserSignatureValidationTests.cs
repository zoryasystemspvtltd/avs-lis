using Lis.Api.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LIS.Masters.Tests.Users
{
    /// <summary>
    /// Validates shared Doctor/Technician signature upload rules (same storage, PNG/JPG, 2 MB).
    /// Role gating (Doctor OR Technician) is covered by UsersController; report print paths unchanged.
    /// </summary>
    [TestClass]
    public class UserSignatureValidationTests
    {
        [TestMethod]
        public void Accepts_Png_Under_2MB()
        {
            string error;
            Assert.IsTrue(DoctorSignatureStorage.TryValidateMetadata("sig.png", 1024, "image/png", out error));
            Assert.IsNull(error);
        }

        [TestMethod]
        public void Accepts_Jpg_And_Jpeg_Under_2MB()
        {
            string error;
            Assert.IsTrue(DoctorSignatureStorage.TryValidateMetadata("sig.jpg", 2048, "image/jpeg", out error));
            Assert.IsTrue(DoctorSignatureStorage.TryValidateMetadata("sig.jpeg", 2048, "image/jpeg", out error));
            Assert.IsTrue(DoctorSignatureStorage.TryValidateMetadata("sig.jpg", 2048, "image/pjpeg", out error));
        }

        [TestMethod]
        public void Rejects_Empty_File()
        {
            string error;
            Assert.IsFalse(DoctorSignatureStorage.TryValidateMetadata("sig.png", 0, "image/png", out error));
            StringAssert.Contains(error, "required");
        }

        [TestMethod]
        public void Rejects_File_Over_2MB()
        {
            string error;
            var overLimit = DoctorSignatureStorage.MaxFileSizeBytes + 1;
            Assert.IsFalse(DoctorSignatureStorage.TryValidateMetadata("sig.png", overLimit, "image/png", out error));
            StringAssert.Contains(error, "2 MB");
        }

        [TestMethod]
        public void Rejects_Invalid_Extension()
        {
            string error;
            Assert.IsFalse(DoctorSignatureStorage.TryValidateMetadata("sig.exe", 1024, "application/octet-stream", out error));
            StringAssert.Contains(error, "PNG or JPG");
        }

        [TestMethod]
        public void Rejects_Unsupported_Mime_When_Provided()
        {
            string error;
            Assert.IsFalse(DoctorSignatureStorage.TryValidateMetadata("sig.png", 1024, "application/pdf", out error));
            StringAssert.Contains(error, "unsupported");
        }

        [TestMethod]
        public void Allows_Missing_Mime_When_Extension_Valid()
        {
            string error;
            Assert.IsTrue(DoctorSignatureStorage.TryValidateMetadata("sig.png", 1024, null, out error));
            Assert.IsTrue(DoctorSignatureStorage.TryValidateMetadata("sig.jpg", 1024, "", out error));
        }

        [TestMethod]
        public void MaxSize_Is_Exactly_2MB()
        {
            Assert.AreEqual(2 * 1024 * 1024L, DoctorSignatureStorage.MaxFileSizeBytes);
            string error;
            Assert.IsTrue(DoctorSignatureStorage.TryValidateMetadata(
                "sig.png", DoctorSignatureStorage.MaxFileSizeBytes, "image/png", out error));
        }

        [TestMethod]
        public void Storage_Folder_Unchanged_For_Doctor_And_Technician()
        {
            // One signature per user — same relative folder; no Technician-specific path introduced.
            Assert.AreEqual("uploads/doctors", DoctorSignatureStorage.RelativeFolder);
        }
    }
}
