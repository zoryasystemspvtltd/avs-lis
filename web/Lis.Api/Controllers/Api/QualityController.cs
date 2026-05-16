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
    [RoutePrefix("api/Quality")]
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
                var defaultOption = new ListOptions
                {
                    RecordPerPage = 10,
                    CurrentPage = 1,
                    SortColumnName = "SampleNo",
                    SortDirection = true
                };

                var headers = System.Web.HttpContext.Current?.Request?.Headers;
                if (headers == null)
                {
                    return defaultOption;
                }

                var apiOption = headers.GetValues("ApiOption");
                if (apiOption == null || !apiOption.Any())
                {
                    return defaultOption;
                }

                return JsonConvert.DeserializeObject<ListOptions>(apiOption.FirstOrDefault(),
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }) ?? defaultOption;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("")]
        public ItemList<dynamic> GetList()
        {
            try
            {
                return manager.Get(ApiOption) ?? new ItemList<dynamic> { TotalRecord = 0, Items = new List<dynamic>() };
            }
            catch (Exception e)
            {
                return new ItemList<dynamic> { TotalRecord = 0, Items = new List<dynamic>() };
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("{id}")]
        public dynamic GetById(string id)
        {
            return manager.Get(id);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("Monthwise")]
        public List<ControlResultDetails> GetMonthwise(string paramCode)
        {
            return manager.GetQualityMonthwiseData(paramCode);
        }
    }
}
