using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Notification;
using LIS.Logger;
using System;
using System.Linq;

namespace LIS.BusinessLogic.Notifications
{
    public class NotificationSecureDownloadManager : INotificationSecureDownloadManager
    {
        private readonly ModuleRepo<SecureLinkToken> tokenRepo;
        private readonly ModuleRepo<SaleInvoice> invoiceRepo;
        private readonly ModuleRepo<PatientDetail> patientRepo;
        private readonly ITestReportManager testReportManager;
        private readonly ILogger logger;

        public NotificationSecureDownloadManager(
            ILogger logger,
            IModuleIdentity identity,
            GenericUnitOfWork unitOfWork,
            ITestReportManager testReportManager)
        {
            tokenRepo = new ModuleRepo<SecureLinkToken>(logger, identity, unitOfWork);
            invoiceRepo = new ModuleRepo<SaleInvoice>(logger, identity, unitOfWork);
            patientRepo = new ModuleRepo<PatientDetail>(logger, identity, unitOfWork);
            this.testReportManager = testReportManager;
            this.logger = logger;
        }

        public SecureReportDownloadResult ValidateAndGetReport(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Fail("Download token is required.");
            }

            var tokenRow = tokenRepo.Get(t => t.Token == token).FirstOrDefault();
            if (tokenRow == null)
            {
                return Fail("Invalid download token.");
            }

            if (tokenRow.IsUsed)
            {
                return Fail("Download token has already been used.");
            }

            if (tokenRow.ExpiresOn < DateTime.Now)
            {
                return Fail("Download token has expired.");
            }

            var invoice = invoiceRepo.Get(i => i.InvoiceNo == tokenRow.InvoiceNo).FirstOrDefault();
            if (invoice == null)
            {
                return Fail("Invoice not found for this download token.");
            }

            if (tokenRow.PatientId.HasValue && invoice.PatientId != tokenRow.PatientId.Value)
            {
                return Fail("Patient validation failed for this download token.");
            }

            if (invoice.PaymentStatus != (int)PaymentStatusType.Paid)
            {
                return Fail("Report download is available only for fully paid invoices.");
            }

            try
            {
                var report = testReportManager.GetDiagnosticTestReport(null, invoice.InvoiceNo);
                return new SecureReportDownloadResult
                {
                    Success = true,
                    InvoiceNo = invoice.InvoiceNo,
                    PatientId = invoice.PatientId,
                    ReportData = report
                };
            }
            catch (TestReportValidationException ex)
            {
                logger.LogError(ex.Message);
                return Fail(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return Fail("Unable to load report for download.");
            }
        }

        public void MarkTokenUsed(string token)
        {
            var tokenRow = tokenRepo.Get(t => t.Token == token).FirstOrDefault();
            if (tokenRow == null || tokenRow.IsUsed)
            {
                return;
            }

            tokenRow.IsUsed = true;
            tokenRepo.Update(tokenRow);
        }

        private static SecureReportDownloadResult Fail(string message)
        {
            return new SecureReportDownloadResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }
}
