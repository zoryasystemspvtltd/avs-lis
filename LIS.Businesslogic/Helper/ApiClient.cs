using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace LIS.BusinessLogic.Helper
{
    public class ApiClient : IDisposable
    {
        private HttpClient httpClient;
        public ApiClient()
        {
            httpClient = new HttpClient();
        }

        public HttpClient GetHttpClient()
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            httpClient.BaseAddress = new Uri(Config.GetConfigValue(Config.ExternalAPIBaseUri));
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var userName = Config.GetConfigValue(Config.ExternalAPIUserId);
            var password = Config.GetConfigValue(Config.ExternalAPIPassword);

            string authInfo = Convert.ToBase64String(Encoding.Default.GetBytes($"{userName}:{password}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authInfo);
            httpClient.DefaultRequestHeaders.Add("UserName", userName);
            httpClient.DefaultRequestHeaders.Add("Password", password);
            return httpClient;
        }

        ~ApiClient()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                httpClient.Dispose();
                httpClient = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
