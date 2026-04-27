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
    public class DailyStatusController : ApiController
    {
        private ITestRequestDetailsManager manager;
        private ILogger logger;

        public DailyStatusController(ITestRequestDetailsManager manager,
            ILogger logger)
        {
            this.manager = manager;
            this.logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<NameValue> Get(int id)
        {
            try
            {
                switch (id)
                {
                    case 0:
                        return manager.DailySampleSummary();
                    case 1:
                        return manager.DailyTechnicianApprovalSummary();
                    case 2:
                        return manager.DailyDoctorApprovalSummary();
                    default:
                        return manager.DailySampleSummary();

                }
            }
            catch (Exception e)
            {
                logger.LogException(e);
                return null;
            }
        }
    }
}
