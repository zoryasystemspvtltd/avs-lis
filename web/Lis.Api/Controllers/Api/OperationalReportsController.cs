using Lis.Api.Providers;
using LIS.BusinessLogic;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
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
        private readonly ILogger logger;

        public OperationalReportsController(IReportManager reportManager, ITestReportManager testReportManager, ILogger logger)
        {
            this.reportManager = reportManager;
            this.testReportManager = testReportManager;
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
                return reportManager.GetSaleInvoiceRegister(FilterOption);
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
    }
}
