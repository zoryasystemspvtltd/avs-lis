using Lis.Api.Providers;
using LIS.BusinessLogic.Helper;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.ExternalApi;
using LIS.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Lis.Api.Controllers.Api
{
    public class HospitalsController : ApiController
    {
        private ILogger logger;
        private IExternalApiManager apiManager;
        private IResponseManager responseMgr;
        private IEquipmentManager manager;
        public HospitalsController(IExternalApiManager externalApiManager ,IResponseManager responseManager, ILogger Logger, IEquipmentManager manager)
        {
            apiManager = externalApiManager;
            responseMgr = responseManager;
            logger = Logger;
            this.manager = manager;
        }

        [AllowAnonymous]
        public async Task<HttpResponseMessage> Get()
        {
            HttpResponseMessage responseMessage = null;
            
            try
            {
                var testDetails = new List<TestDetail>();

                using (var client = new ApiClient().GetHttpClient())
                {
                    var requestParams = Config.GetConfigValue(Config.OrderParameters);
                    requestParams = requestParams.Replace(":", "&");
                    if (!string.IsNullOrEmpty(requestParams))
                    {
                        requestParams = $"?{requestParams}";
                    }

                    var requestUri = Config.GetConfigValue(Config.TestRequestUri);
                    var testRequestUri = $"{requestUri}{requestParams}";

                    logger.LogDebug("Get Test requisition :'{0}'", testRequestUri);
                    
                    responseMessage = await client.GetAsync(testRequestUri);
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        string httpResponseResult = responseMessage.Content.ReadAsStringAsync().ContinueWith(task => task.Result).Result;
                        testDetails = JsonConvert.DeserializeObject<List<TestDetail>>(httpResponseResult);
                        logger.LogInfo("Get Test requisition response :'{0}'", httpResponseResult);
                    }
                }

                if (testDetails != null)
                {
                    apiManager.SaveHISTestDetails(testDetails);                    
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return Request.CreateResponse(HttpStatusCode.NotImplemented);
            }

            return responseMessage;
        }
        
    }
}
