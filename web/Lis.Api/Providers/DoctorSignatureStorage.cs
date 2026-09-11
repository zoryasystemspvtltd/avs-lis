using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace Lis.Api.Providers
{
    public static class DoctorSignatureStorage
    {
        public const long MaxFileSizeBytes = 2 * 1024 * 1024;
        public const string RelativeFolder = "uploads/doctors";

        private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg" };
        private static readonly string[] AllowedMimeTypes =
        {
            "image/png",
            "image/jpeg",
            "image/pjpeg"
        };

        public static string GetUploadRoot()
        {
            var root = HostingEnvironment.MapPath("~/" + RelativeFolder);
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }

            return root;
        }

        public static bool TryValidate(HttpPostedFile file, out string error)
        {
            if (file == null || file.ContentLength == 0)
            {
                error = "Signature file is required.";
                return false;
            }

            return TryValidateMetadata(file.FileName, file.ContentLength, file.ContentType, out error);
        }

        /// <summary>
        /// Shared PNG/JPG + 2 MB rules for Doctor/Technician signature uploads (no HostingEnvironment dependency).
        /// </summary>
        public static bool TryValidateMetadata(string fileName, long contentLength, string contentType, out string error)
        {
            error = null;

            if (contentLength <= 0)
            {
                error = "Signature file is required.";
                return false;
            }

            if (contentLength > MaxFileSizeBytes)
            {
                error = "Signature must not exceed 2 MB.";
                return false;
            }

            var extension = Path.GetExtension(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(extension)
                || !AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                error = "Signature must be a PNG or JPG image.";
                return false;
            }

            var mimeType = (contentType ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(mimeType)
                && !AllowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
            {
                error = "Signature has an unsupported file type.";
                return false;
            }

            return true;
        }

        public static string SaveSignature(string userId, HttpPostedFile file, string existingRelativePath)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".png";
            }

            extension = extension.ToLowerInvariant();
            var fileName = string.Format(
                "Doctor_{0}_{1}{2}",
                SanitizeUserId(userId),
                DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),
                extension);

            var root = GetUploadRoot();
            var physicalPath = Path.Combine(root, fileName);
            var fullRoot = Path.GetFullPath(root);
            var fullTarget = Path.GetFullPath(physicalPath);
            if (!fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid signature file path.");
            }

            file.SaveAs(physicalPath);

            var relativePath = RelativeFolder.Replace('\\', '/') + "/" + fileName;
            DeletePhysicalFile(existingRelativePath);
            return relativePath;
        }

        public static void DeletePhysicalFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var normalized = relativePath.Replace('/', '\\').TrimStart('\\');
            if (normalized.Contains(".."))
            {
                return;
            }

            if (!normalized.StartsWith(RelativeFolder.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var physicalPath = HostingEnvironment.MapPath("~/" + normalized.Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(physicalPath) || !File.Exists(physicalPath))
            {
                return;
            }

            var root = Path.GetFullPath(GetUploadRoot());
            var fullTarget = Path.GetFullPath(physicalPath);
            if (fullTarget.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(physicalPath);
            }
        }

        /// <summary>
        /// Reads a stored signature and returns it as an inline base64 data URI
        /// (e.g. "data:image/png;base64,...") suitable for embedding in reports.
        /// Returns null when the path is empty or the file cannot be resolved.
        /// </summary>
        public static string GetSignatureDataUri(string relativePath)
        {
            var physicalPath = ResolvePhysicalPath(relativePath);
            if (physicalPath == null)
            {
                return null;
            }

            var extension = Path.GetExtension(physicalPath).ToLowerInvariant();
            var contentType = extension == ".png" ? "image/png" : "image/jpeg";
            var bytes = File.ReadAllBytes(physicalPath);
            return string.Format("data:{0};base64,{1}", contentType, Convert.ToBase64String(bytes));
        }

        public static string ResolvePhysicalPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            var normalized = relativePath.Replace('/', '\\').TrimStart('\\');
            if (normalized.Contains("..")
                || !normalized.StartsWith(RelativeFolder.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var physicalPath = HostingEnvironment.MapPath("~/" + normalized.Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(physicalPath) || !File.Exists(physicalPath))
            {
                return null;
            }

            var root = Path.GetFullPath(GetUploadRoot());
            var fullTarget = Path.GetFullPath(physicalPath);
            return fullTarget.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? physicalPath : null;
        }

        private static string SanitizeUserId(string userId)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(userId.Where(c => !invalid.Contains(c)).ToArray());
        }
    }
}
