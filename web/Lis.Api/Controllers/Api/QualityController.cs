using Lis.Api.Providers;
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
    public class QualityController : ApiController
    {
        private IQualityControlManager manager;

        public QualityController(IQualityControlManager qualityControlManager)
        {
            manager = qualityControlManager;
        }

        private ListOptions ApiOption
        {
            get
            {
                var apiOption = System.Web.HttpContext.Current.Request.Headers.GetValues("ApiOption");
                if (apiOption == null || apiOption.Count() == 0)
                {
                    throw new KeyNotFoundException("Invalid Option specified");
                }

                var option = JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                });
                return option;
            }
        }
        /// <summary>
        /// Get All Quality Control results
        /// </summary>
        /// <returns>List of all quality control results</returns>
        [AllowAnonymous]
        [HttpGet]
        public ItemList<dynamic> Get()
        {
            var controlResults = manager.Get(ApiOption);

            return controlResults;
        }

        /// <summary>
        /// Get Quality Control result by result id
        /// </summary>
        /// <param name="Id">control result Id</param>
        /// <returns>Quality control result</returns>
        [AllowAnonymous]
        [HttpGet]
        public dynamic Get(string Id)
        {
            var controlResult = manager.Get(Id);

            return controlResult;
        }

        /// <summary>
        /// Get Quality Control result by result id
        /// </summary>
        /// <param name="Id">control result Id</param>
        /// <returns>Quality control result</returns>
        [AllowAnonymous]
        [HttpGet]
        public List<ControlResultDetails> Get(string paramCode, bool flag = true)
        {
            var controlResultList = manager.GetQualityMonthwiseData(paramCode);

            return controlResultList;
        }
    }
}
