using LIS.Com.Businesslogic;
using LIS.DtoModel;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Lis.Api.Client.Test
{
    class Program
    {
        static string token;
        static string rootUrl = "https://localhost:44392";
        static string accessToken = "test";
        static void Main(string[] args)
        {
            //Auth();
            //Get();
            GetSample("1545005E");
            //GetSampleByBI("4-4A16823996");
            Console.ReadKey();
        }

        private static async Task GetSampleByBI(string SampleNo)
        {
            LisContext.LisDOM.InitAPI(rootUrl, accessToken);
            var samples = await LisContext.LisDOM.GetTestRequestDetails(SampleNo);
            if (samples.Count() > 0)
            {
                Console.WriteLine(JsonConvert.SerializeObject(samples));
                foreach (var sample in samples)
                {
                    var isAck = await LisContext.LisDOM.AcknowledgeSample(sample.Id);
                    Console.WriteLine(JsonConvert.SerializeObject(isAck));
                }

            }

        }

        static void Auth()
        {
            AuthDetails authDetails = new AuthDetails();
            authDetails.Username = "admin@zorya.co.in";
            authDetails.Password = "Admin@123";
            string authRequest = $"grant_type=password&username={authDetails.Username}&password={authDetails.Password}";
            ICommunicationChannel communicationChannel = new CommunicationChannel(rootUrl, accessToken);
            var resp = communicationChannel.Authenticate("Token", authRequest).Result;
            token = resp.AccessToken;

            Console.WriteLine(token);
        }

        static void Get()
        {
            ICommunicationChannel communicationChannel = new CommunicationChannel(rootUrl, accessToken);
            var resp = communicationChannel.Get($"api/Quality", null, token).Result;
            if (resp.StatusCode == (int)HttpStatusCode.OK)
            {
                Console.WriteLine(JsonConvert.SerializeObject(resp));
            }
        }

        static void GetSample(string SampleNUmber)
        {
            string apiName = $"Lis/{SampleNUmber}";

            ICommunicationChannel communicationChannel = new CommunicationChannel(rootUrl, accessToken);
            var resp = communicationChannel.Get($"api/{apiName}", null, null).Result;
            if (resp.StatusCode == (int)HttpStatusCode.OK)
            {
                Console.WriteLine(JsonConvert.SerializeObject(resp));
                SetStatus(SampleNUmber);
            }
        }

        static void SetStatus(string SampleNUmber)
        {
            string apiName = $"Lis/{SampleNUmber}";

            ICommunicationChannel communicationChannel = new CommunicationChannel(rootUrl, accessToken);
            var resp = communicationChannel.Put($"api/{apiName}", null, token).Result;
            if (resp.StatusCode == (int)HttpStatusCode.OK)
            {
                Console.WriteLine(JsonConvert.SerializeObject(resp));
            }
        }
    }
}
