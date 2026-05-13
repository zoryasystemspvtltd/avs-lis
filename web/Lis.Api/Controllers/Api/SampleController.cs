using Lis.Api.Providers;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    public class SampleController : ApiController
    {
        private ITestRequestDetailsManager manager;
        private ILogger logger;
        private IResponseManager responseMgr;

        public SampleController(ITestRequestDetailsManager testRequestDetailsManager, IResponseManager responseManager, ILogger Logger)
        {
            manager = testRequestDetailsManager;
            responseMgr = responseManager;
            logger = Logger;
        }
        /// <summary>
        /// This is used for technician review
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [QAuthorize(ModuleName = "Reports"
        , ModulePermissionTypes = ModulePermissionType.CanAuthorize
            | ModulePermissionType.CanReject
        )]
        public HttpResponseMessage Post(AuthorizeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }

                manager.TechnicianReview(request.Id, request.Status, request.Note, request.RunIndex);

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// This is used as doctor approval
        /// </summary>
        /// <param name="testRequestDetail"></param>
        /// <returns></returns>
        [QAuthorize(ModuleName = "DoctorsApprovals"
        , ModulePermissionTypes = ModulePermissionType.CanAuthorize
            | ModulePermissionType.CanReject
        )]
        public HttpResponseMessage Put(AuthorizeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Request.CreateResponse(HttpStatusCode.PreconditionFailed, ModelState.Keys);
                }

                manager.DoctorReview(request.Id, request.Status, request.Note, request.RunIndex);

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// Get result by request id
        /// </summary>
        /// <param name="Id">Request Id</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ReviewTest Get(long Id)
        {
            try
            {
                var testRequestDetail = manager.GetTestResultByRequestId(Id);

                return testRequestDetail;
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}

public class AuthorizeRequest
{
    public long Id { get; set; }

    public ReportStatusType Status { get; set; }

    public string Note { get; set; }

    public long RunIndex { get; set; }
}
