using Lis.Api.Providers;
using LIS.BusinessLogic;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    [RoutePrefix("api/CommissionPayout")]
    public class CommissionPayoutController : ApiController
    {
        private readonly CommissionPayoutManager payouts;
        private readonly ILogger logger;

        public CommissionPayoutController(CommissionPayoutManager payouts, ILogger logger)
        {
            this.payouts = payouts;
            this.logger = logger;
        }

        [HttpGet]
        [Route("schedule")]
        [QAuthorize(ModuleName = "CommissionPayout", ModulePermissionTypes = ModulePermissionType.CanView)]
        public CommissionPayoutSchedule GetSchedule(int recipientType, int recipientId)
        {
            return Run(() => payouts.GetSchedule(recipientType, recipientId));
        }

        [HttpPost]
        [Route("preview")]
        [QAuthorize(ModuleName = "CommissionPayout", ModulePermissionTypes = ModulePermissionType.CanView)]
        public CommissionPayoutPreview Preview(CommissionPayoutRequest request)
        {
            return Run(() => payouts.Preview(request));
        }

        [HttpPost]
        [Route("confirm")]
        [QAuthorize(ModuleName = "CommissionPayout", ModulePermissionTypes = ModulePermissionType.CanAdd)]
        public long Confirm(CommissionPayoutRequest request)
        {
            return Run(() => payouts.Confirm(request));
        }

        [HttpGet]
        [Route("")]
        [QAuthorize(ModuleName = "CommissionPayout", ModulePermissionTypes = ModulePermissionType.CanView)]
        public List<CommissionPayout> List(int? recipientType = null, int? recipientId = null)
        {
            return Run(() => payouts.List(recipientType, recipientId));
        }

        [HttpGet]
        [Route("{id:long}")]
        [QAuthorize(ModuleName = "CommissionPayout", ModulePermissionTypes = ModulePermissionType.CanView)]
        public CommissionPayoutMemo Get(long id)
        {
            return Run(() =>
            {
                var memo = payouts.GetMemo(id);
                if (memo == null)
                {
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Payout was not found."));
                }

                memo.LabName = ReadSetting("Report:LabName") ?? ReadSetting("Report.LabName");
                return memo;
            });
        }

        private T Run<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (HttpResponseException)
            {
                throw;
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex.Message);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Commission payout failed."));
            }
        }

        private static string ReadSetting(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
