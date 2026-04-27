
using Lis.Api.Client;
using LIS.DtoModel;
using LIS.DtoModel.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LIS.Com.Businesslogic
{
    public class BarcodePrintCommand
    {
        private static BarcodePrintCommand context;
        public static BarcodePrintCommand LisDOM
        {
            get
            {
                if (context == null)
                {
                    context = new BarcodePrintCommand();
                }
                return context;
            }
        }

        private string serverUrl = "https://localhost:44392/";// ConfigurationManager.AppSettings["ServerURL"];
        private string apiKey = "2b29e86e5d3844768d3ee545f148e5fa";// ConfigurationManager.AppSettings["ApiKey"];
        private ICommunicationChannel api;
      
        public void InitAPI(string serverUrl, string apiKey)
        {
            this.api = new CommunicationChannel(serverUrl, apiKey);
        }
        public async Task<IEnumerable<TestRequestDetail>> GetAllNewSampleDetails()
        {
            try
            {
                //InitAPI(serverUrl, apiKey);
                string apiName = $"NewSample";
                var response = await api.Get($"api/{apiName}", null, null);
                var jsonModel = JsonConvert.SerializeObject(response.Result);
                IEnumerable<TestRequestDetail> items = null;
                if (jsonModel.Length > 0)
                {
                    items = JsonConvert.DeserializeObject<IEnumerable<TestRequestDetail>>(jsonModel);
                }
                return items;
            }
            catch (System.Exception ex)
            {
                throw;
            }
        }

        public async Task<List<TestRequestDetail>> GetSampleDetails(string requestNo)
        {
            try
            {
               // InitAPI(serverUrl, apiKey);
                string apiName = $"NewSample/{requestNo}";
                var response = await api.Get($"api/{apiName}");
                var jsonModel = JsonConvert.SerializeObject(response.Result);
                List<TestRequestDetail> items = null;
                if (jsonModel.Length > 0)
                {
                    items = JsonConvert.DeserializeObject<List<TestRequestDetail>>(jsonModel);
                }
                return items;
            }
            catch (System.Exception)
            {
                throw;
            }
        }
    }
}
