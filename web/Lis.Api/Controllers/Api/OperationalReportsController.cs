using Lis.Api.Models;
using Lis.Api.Providers;
using LIS.BusinessLogic;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    [RoutePrefix("api/Reports")]
    public class OperationalReportsController : ApiController
    {
        private readonly IReportManager reportManager;
        private readonly ITestReportManager testReportManager;
        private readonly IRadiologyReportManager radiologyReportManager;
        private readonly ApplicationUserManager userManager;
        private readonly ILogger logger;

        public OperationalReportsController(
            IReportManager reportManager,
            ITestReportManager testReportManager,
            IRadiologyReportManager radiologyReportManager,
            ApplicationUserManager userManager,
            ILogger logger)
        {
            this.reportManager = reportManager;
            this.testReportManager = testReportManager;
            this.radiologyReportManager = radiologyReportManager;
            this.userManager = userManager;
            this.logger = logger;
        }

        private ReportFilterOptions FilterOption
        {
            get
            {
                var apiOption = System.Web.HttpContext.Current.Request.Headers.GetValues("ApiOption");
                if (apiOption == null || !apiOption.Any())
                {
                    return DefaultOptions();
                }

                var option = JsonConvert.DeserializeObject<ReportFilterOptions>(
                    apiOption.FirstOrDefault(),
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                return option ?? DefaultOptions();
            }
        }

        private static ReportFilterOptions DefaultOptions()
        {
            var today = DateTime.Today;
            return new ReportFilterOptions
            {
                FromDate = new DateTime(today.Year, today.Month, 1),
                ToDate = today,
                RecordPerPage = 25,
                CurrentPage = 1,
                SortColumnName = "InvoiceDate",
                SortDirection = false
            };
        }

        [HttpGet]
        [Route("SaleInvoiceRegister")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<SaleInvoiceRegisterRow> GetSaleInvoiceRegister()
        {
            try
            {
                var options = FilterOption;
                ResolveCreatedByFilter(options);
                return reportManager.GetSaleInvoiceRegister(options);
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return new ItemList<SaleInvoiceRegisterRow> { TotalRecord = 0, Items = new List<SaleInvoiceRegisterRow>() };
            }
        }

        [HttpGet]
        [Route("TestBookingRegister")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<TestBookingRegisterRow> GetTestBookingRegister()
        {
            try
            {
                var options = FilterOption;
                if (string.IsNullOrWhiteSpace(options.SortColumnName) || options.SortColumnName == "InvoiceDate")
                {
                    options.SortColumnName = "BookingDate";
                }

                return reportManager.GetTestBookingRegister(options);
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return new ItemList<TestBookingRegisterRow> { TotalRecord = 0, Items = new List<TestBookingRegisterRow>() };
            }
        }

        [HttpGet]
        [Route("TestReportLabNumbers")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IHttpActionResult GetTestReportLabNumbers()
        {
            try
            {
                return Ok(testReportManager.GetPrintableLabNumbers());
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Unable to load lab numbers."));
            }
        }

        [HttpGet]
        [Route("TestReport")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IHttpActionResult GetTestReport(string labNo = null, string invoiceNo = null)
        {
            try
            {
                var report = testReportManager.GetDiagnosticTestReport(labNo, invoiceNo);
                EnrichLabApprover(report);
                return Ok(report);
            }
            catch (TestReportValidationException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Unable to load test report."));
            }
        }

        [HttpGet]
        [Route("RadiologyPrintAccessions")]
        [QAuthorize(ModuleName = "RadiologyReports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IHttpActionResult GetRadiologyPrintAccessions()
        {
            try
            {
                return Ok(radiologyReportManager.GetPrintableAccessions());
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Unable to load radiology accessions."));
            }
        }

        [HttpGet]
        [Route("RadiologyReport")]
        [QAuthorize(ModuleName = "RadiologyReports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IHttpActionResult GetRadiologyReport(long radiologyRequestId)
        {
            try
            {
                var report = radiologyReportManager.GetRadiologyReportForPrint(radiologyRequestId);
                EnrichRadiologyApprover(report);
                return Ok(report);
            }
            catch (TestReportValidationException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Unable to load radiology report."));
            }
        }

        [HttpGet]
        [Route("CollectionSummary")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<CollectionSummaryRow> GetCollectionSummary() => RunReport(reportManager.GetCollectionSummary);

        [HttpGet]
        [Route("CollectorWise")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<CollectorWiseRow> GetCollectorWise() => RunReport(reportManager.GetCollectorWiseReport);

        [HttpGet]
        [Route("PendingCollection")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<PendingCollectionRow> GetPendingCollection() => RunReport(reportManager.GetPendingCollectionReport);

        [HttpGet]
        [Route("Recollection")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<RecollectionRow> GetRecollection() => RunReport(reportManager.GetRecollectionReport);

        [HttpGet]
        [Route("ReceivedSamples")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<ReceivedSampleRow> GetReceivedSamples() => RunReport(reportManager.GetReceivedSamplesReport);

        [HttpGet]
        [Route("RejectedSamples")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<RejectedSampleRow> GetRejectedSamples() => RunReport(reportManager.GetRejectedSamplesReport);

        [HttpGet]
        [Route("SampleTurnaround")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<SampleTurnaroundRow> GetSampleTurnaround() => RunReport(reportManager.GetSampleTurnaroundReport);

        [HttpGet]
        [Route("PendingRadiology")]
        [QAuthorize(ModuleName = "RadiologyReports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<PendingRadiologyRow> GetPendingRadiology() => RunReport(reportManager.GetPendingRadiologyReport);

        [HttpGet]
        [Route("AuthorizedRadiology")]
        [QAuthorize(ModuleName = "RadiologyReports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<AuthorizedRadiologyRow> GetAuthorizedRadiology() => RunReport(reportManager.GetAuthorizedRadiologyReport);

        [HttpGet]
        [Route("ModalityStatistics")]
        [QAuthorize(ModuleName = "RadiologyReports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<ModalityStatisticsRow> GetModalityStatistics() => RunReport(reportManager.GetModalityStatisticsReport);

        [HttpGet]
        [Route("RadiologistProductivity")]
        [QAuthorize(ModuleName = "RadiologyReports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public ItemList<RadiologistProductivityRow> GetRadiologistProductivity() => RunReport(reportManager.GetRadiologistProductivityReport);

        /// <summary>
        /// Resolves the approving doctor (by username/email captured at approval time)
        /// and fills their display name, designation and inline signature image so the
        /// printed diagnostic report shows the same signature captured in User management.
        /// </summary>
        private void EnrichLabApprover(DiagnosticTestReportDto report)
        {
            var header = report?.Header;
            if (header == null || string.IsNullOrWhiteSpace(header.ApprovedBy))
            {
                return;
            }

            var user = ResolveUserByName(header.ApprovedBy);
            if (user == null)
            {
                return;
            }

            header.ApprovedByName = BuildDisplayName(user, header.ApprovedBy);
            header.ApprovedByDesignation = user.DoctorDesignation;
            header.ApprovedBySignatureImage = DoctorSignatureStorage.GetSignatureDataUri(user.DoctorSignaturePath);
        }

        private void EnrichRadiologyApprover(DiagnosticRadiologyReportDto report)
        {
            var header = report?.Header;
            if (header == null || string.IsNullOrWhiteSpace(header.AuthorizedBy))
            {
                return;
            }

            var user = ResolveUserByName(header.AuthorizedBy);
            if (user == null)
            {
                return;
            }

            header.AuthorizedByName = BuildDisplayName(user, header.AuthorizedBy);
            header.AuthorizedByDesignation = user.DoctorDesignation;
            header.AuthorizedBySignatureImage = DoctorSignatureStorage.GetSignatureDataUri(user.DoctorSignaturePath);
        }

        private ApplicationUser ResolveUserByName(string userNameOrEmail)
        {
            var key = (userNameOrEmail ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            try
            {
                return userManager.FindByName(key) ?? userManager.FindByEmail(key);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return null;
            }
        }

        private static string BuildDisplayName(ApplicationUser user, string fallback)
        {
            var name = string.Format("{0} {1}", user.FirstName, user.LastName).Trim();
            return string.IsNullOrWhiteSpace(name) ? fallback : name;
        }

        private void ResolveCreatedByFilter(ReportFilterOptions options)
        {
            if (options == null || string.IsNullOrWhiteSpace(options.CreatedById))
            {
                return;
            }

            var user = userManager.FindById(options.CreatedById.Trim());
            if (user == null)
            {
                throw new ArgumentException("Invalid Created By user.");
            }

            options.CreatedByUserName = !string.IsNullOrWhiteSpace(user.UserName)
                ? user.UserName.Trim()
                : user.Email?.Trim();
        }

        private ItemList<T> RunReport<T>(Func<ReportFilterOptions, ItemList<T>> action) where T : class
        {
            try
            {
                return action(FilterOption);
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return new ItemList<T> { TotalRecord = 0, Items = new List<T>() };
            }
        }
    }
}
