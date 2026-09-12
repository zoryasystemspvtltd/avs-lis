using Lis.Api.Models;
using Lis.Api.Providers;
using LIS.BusinessLogic;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Reports;
using LIS.Logger;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
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
        private readonly IReportLayoutConfigurationManager reportLayoutManager;
        private readonly IReportProductionPresentationAdapter presentationAdapter;
        private readonly ApplicationUserManager userManager;
        private readonly ILogger logger;

        public OperationalReportsController(
            IReportManager reportManager,
            ITestReportManager testReportManager,
            IRadiologyReportManager radiologyReportManager,
            IReportLayoutConfigurationManager reportLayoutManager,
            IReportProductionPresentationAdapter presentationAdapter,
            ApplicationUserManager userManager,
            ILogger logger)
        {
            this.reportManager = reportManager;
            this.testReportManager = testReportManager;
            this.radiologyReportManager = radiologyReportManager;
            this.reportLayoutManager = reportLayoutManager;
            this.presentationAdapter = presentationAdapter;
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
        [Route("TestReportPrintOptions")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IHttpActionResult GetTestReportPrintOptions(string labNo = null, string invoiceNo = null)
        {
            try
            {
                return Ok(testReportManager.GetPrintableTestOptions(labNo, invoiceNo));
            }
            catch (TestReportValidationException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Unable to load print options."));
            }
        }

        [HttpGet]
        [Route("TestReport")]
        [QAuthorize(ModuleName = "Reports", ModulePermissionTypes = ModulePermissionType.CanView)]
        public IHttpActionResult GetTestReport(string labNo = null, string invoiceNo = null, long? testRequestDetailId = null)
        {
            try
            {
                var report = testReportManager.GetDiagnosticTestReport(labNo, invoiceNo, testRequestDetailId);
                EnrichLabApprover(report);
                EnrichLabTechnician(report);
                EnrichReportBranding(report);
                EnrichReportLayout(report);
                ApplyDiagnosticPresentation(report, testRequestDetailId);
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
                EnrichRadiologyLayout(report);
                ApplyRadiologyPresentation(report, radiologyRequestId);
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
            header.ApprovedByQualification = user.Qualification;
            header.ApprovedByDesignation = user.DoctorDesignation;
            header.ApprovedBySignatureImage = DoctorSignatureStorage.GetSignatureDataUri(user.DoctorSignaturePath);
        }

        /// <summary>
        /// Resolves the reviewing technician (<see cref="DiagnosticTestReportHeader.ReviewedBy"/>)
        /// from Technician Approval and fills display name + signature from the same user signature store.
        /// Missing signature must never block printing.
        /// </summary>
        private void EnrichLabTechnician(DiagnosticTestReportDto report)
        {
            var header = report?.Header;
            if (header == null || string.IsNullOrWhiteSpace(header.ReviewedBy))
            {
                return;
            }

            try
            {
                var user = ResolveUserByName(header.ReviewedBy);
                if (user == null)
                {
                    return;
                }

                header.ReviewedByName = BuildDisplayName(user, header.ReviewedBy);
                header.ReviewedByQualification = user.Qualification;
                header.ReviewedBySignatureImage = DoctorSignatureStorage.GetSignatureDataUri(user.DoctorSignaturePath);
            }
            catch (Exception ex)
            {
                logger.LogError("Technician signature enrich failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Fills laboratory branding / footer fields from Web.config appSettings (Report:* keys)
        /// and ClientApplication name as fallback. No hard-coded lab identity.
        /// </summary>
        private void EnrichReportBranding(DiagnosticTestReportDto report)
        {
            var header = report?.Header;
            if (header == null)
            {
                return;
            }

            header.LabName = ReadSetting("Report:LabName") ?? ReadSetting("Report.LabName");
            header.Tagline = ReadSetting("Report:Tagline") ?? ReadSetting("Report.Tagline");
            header.CentreName = ReadSetting("Report:CentreName") ?? ReadSetting("Report.CentreName");
            header.LogoUrl = ReadSetting("Report:LogoUrl") ?? ReadSetting("Report.LogoUrl");
            header.LicenseName = ReadSetting("Report:LicenseName") ?? ReadSetting("Report.LicenseName");
            header.Address = ReadSetting("Report:Address") ?? ReadSetting("Report.Address");
            header.Email = ReadSetting("Report:Email") ?? ReadSetting("Report.Email");
            header.ContactNumbers = ReadSetting("Report:ContactNumbers") ?? ReadSetting("Report.ContactNumbers");
            header.PharmacyContact = ReadSetting("Report:PharmacyContact") ?? ReadSetting("Report.PharmacyContact");
            header.AppointmentContact = ReadSetting("Report:AppointmentContact") ?? ReadSetting("Report.AppointmentContact");

            if (string.IsNullOrWhiteSpace(header.LabName) || string.IsNullOrWhiteSpace(header.CentreName))
            {
                try
                {
                    using (var db = new IdentityDbContext())
                    {
                        var app = db.ClientApplications.OrderBy(a => a.Id).FirstOrDefault();
                        if (app != null)
                        {
                            if (string.IsNullOrWhiteSpace(header.LabName))
                            {
                                header.LabName = app.Name;
                            }
                            if (string.IsNullOrWhiteSpace(header.Tagline) && !string.IsNullOrWhiteSpace(app.Description))
                            {
                                header.Tagline = app.Description;
                            }
                            if (string.IsNullOrWhiteSpace(header.CentreName))
                            {
                                header.CentreName = app.Name;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }

            if (string.IsNullOrWhiteSpace(header.LicenseName))
            {
                header.LicenseName = header.LabName;
            }
        }

        private static string ReadSetting(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Attaches physical print layout for stationery clearance / signature placement.
        /// Failures are swallowed so report printing never blocks on configuration.
        /// </summary>
        private void EnrichReportLayout(DiagnosticTestReportDto report)
        {
            if (report == null || reportLayoutManager == null)
            {
                return;
            }

            try
            {
                report.Layout = reportLayoutManager.GetByReportType(ReportLayoutReportTypes.Diagnostic);
            }
            catch (Exception ex)
            {
                logger.LogError("Report layout enrich failed (Diagnostic): " + ex.Message);
                report.Layout = null;
            }
        }

        private void EnrichRadiologyLayout(DiagnosticRadiologyReportDto report)
        {
            if (report == null || reportLayoutManager == null)
            {
                return;
            }

            try
            {
                report.Layout = reportLayoutManager.GetByReportType(ReportLayoutReportTypes.Radiology);
            }
            catch (Exception ex)
            {
                logger.LogError("Report layout enrich failed (Radiology): " + ex.Message);
                report.Layout = null;
            }
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

        /// <summary>
        /// Phase 4 presentation switch (post-enrichment). Failures stay on Existing Angular — never throws to client.
        /// </summary>
        private void ApplyDiagnosticPresentation(DiagnosticTestReportDto report, long? testRequestDetailId)
        {
            if (report == null || presentationAdapter == null)
            {
                return;
            }

            try
            {
                report.Presentation = presentationAdapter.Apply(new ReportProductionPresentationContext
                {
                    ReportType = ReportTemplateTypes.Diagnostic,
                    ReportData = report,
                    TestRequestDetailId = testRequestDetailId,
                    InvoiceNo = report.Header?.InvoiceNo
                });
            }
            catch (Exception ex)
            {
                logger.LogError("RTE_PRESENTATION_FALLBACK Diagnostic controller catch ExceptionType={0}", ex.GetType().Name);
                report.Presentation = new ReportProductionPresentationDto
                {
                    PresentationMode = ReportProductionPresentationModes.Existing,
                    FallbackReason = ReportProductionFallbackReasons.RenderException,
                    PresentationTraceId = Guid.NewGuid().ToString("N")
                };
            }
        }

        private void ApplyRadiologyPresentation(DiagnosticRadiologyReportDto report, long radiologyRequestId)
        {
            if (report == null || presentationAdapter == null)
            {
                return;
            }

            try
            {
                report.Presentation = presentationAdapter.Apply(new ReportProductionPresentationContext
                {
                    ReportType = ReportTemplateTypes.Radiology,
                    ReportData = report,
                    InvoiceNo = report.Header?.InvoiceNo,
                    RadiologyRequestId = radiologyRequestId
                });
            }
            catch (Exception ex)
            {
                logger.LogError("RTE_PRESENTATION_FALLBACK Radiology controller catch ExceptionType={0}", ex.GetType().Name);
                report.Presentation = new ReportProductionPresentationDto
                {
                    PresentationMode = ReportProductionPresentationModes.Existing,
                    FallbackReason = ReportProductionFallbackReasons.RenderException,
                    PresentationTraceId = Guid.NewGuid().ToString("N")
                };
            }
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
