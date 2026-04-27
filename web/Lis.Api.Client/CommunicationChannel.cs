using LIS.DtoModel;
using LIS.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lis.Api.Client
{
    public class CommunicationChannel : ICommunicationChannel
    {
        private readonly int TimeOutInSeconds = 10 * 60;
        private static HttpClient client;
        public string accessKey { get; set; }
        public string rootUrl { get; set; }
        public CommunicationChannel(string RootUrl, string AccessKey)
        {
            this.rootUrl = RootUrl;
            this.accessKey = AccessKey;
        }

        public async Task<Token> Authenticate(string action, string token)
        {
            Logger.LogInstance.LogDebug($"Authenticate {action}");
            using (client = new HttpClient() { Timeout = TimeSpan.FromSeconds(TimeOutInSeconds) })
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("accesskey", this.accessKey);

                string path = $"{this.rootUrl}/{action}";
                var cts = new CancellationTokenSource();
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    var content = new StringContent(token, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(path, content, cts.Token);
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<Token>(jsonString);

                    return data;
                }
                catch
                {
                    return default(Token);
                }
            }
        }

        /// <summary>
        /// Get request
        /// </summary>
        /// <param name="address">API address</param>
        /// <param name="action">Action</param>
        /// <param name="corelationId">CorelationId</param>
        /// <param name="headerParams">Header parameters</param>
        /// <param name="token">Authentication token</param>
        /// <returns>APIResponse</returns>
        public async Task<IAPIResponse> Get(string action, List<KeyValuePair<string, object>> headerParams = null, string token = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInstance.LogDebug($"Get {action}");
            using (client = new HttpClient() { Timeout = TimeSpan.FromSeconds(TimeOutInSeconds) })
            {
                client.DefaultRequestHeaders.Accept.Add(
              new MediaTypeWithQualityHeaderValue("application/json"));

                if (token != null)
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }

                if (headerParams != null)
                {
                    foreach (var item in headerParams)
                    {
                        client.DefaultRequestHeaders.Add(item.Key, JsonConvert.SerializeObject(item.Value));
                    }
                }

                client.DefaultRequestHeaders.Add("accesskey", this.accessKey);

                string path = $"{this.rootUrl}/{action}";

                CancellationToken cToken = cancellationToken;
                if (cancellationToken == default(CancellationToken))
                {
                    var cts = new CancellationTokenSource();
                    cToken = cts.Token;
                }

                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    HttpResponseMessage response = await client.GetAsync(path, cToken);
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var jsonObject = JsonConvert.DeserializeObject(jsonString);
                    var data = new APIResponse()
                    {
                        Result = jsonObject,
                        StatusCode = 200
                    };
                    return data;
                }
                catch (TaskCanceledException ex)
                {
                    Logger.LogInstance.LogException(ex);
                    if (ex.CancellationToken == cToken)
                    {
                        // a real cancellation, triggered by the caller
                        APIResponse invalidResponse = new APIResponse();
                        invalidResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                        invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                        return invalidResponse;
                    }
                    else
                    {
                        // a web request timeout
                        APIResponse invalidResponse = new APIResponse();
                        invalidResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                        invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                        return invalidResponse;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogInstance.LogException(ex);
                    APIResponse invalidResponse = new APIResponse();
                    invalidResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                    return invalidResponse;
                }
            }
        }

        /// <summary>
        /// POST request
        /// </summary>
        /// <param name="address">API address</param>
        /// <param name="action">Action</param>
        /// <param name="corelationId">CorelationId</param>
        /// <param name="model">Model to send for post request</param>
        /// <param name="token">Authentication token</param>
        /// <returns>APIResponse</returns>
        public async Task<IAPIResponse> Post(string action, object model = null, string token = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInstance.LogDebug($"Post {action}");
            using (client = new HttpClient() { Timeout = TimeSpan.FromSeconds(TimeOutInSeconds) })
            {
                client.DefaultRequestHeaders.Accept.Add(
              new MediaTypeWithQualityHeaderValue("application/json"));


                if (token != null)
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }


                client.DefaultRequestHeaders.Add("accesskey", this.accessKey);


                string path = $"{this.rootUrl}/{action}";

                CancellationToken cToken = cancellationToken;
                if (cancellationToken == default(CancellationToken))
                {
                    var cts = new CancellationTokenSource();
                    cToken = cts.Token;
                }
                string json = JsonConvert.SerializeObject(model);
                Logger.LogInstance.LogDebug(json);
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(path, content, cToken);
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<APIResponse>(jsonString);
                    Logger.LogInstance.LogDebug(jsonString);
                    return data;
                }
                catch (TaskCanceledException ex)
                {
                    Logger.LogInstance.LogException(ex);
                    if (ex.CancellationToken == cToken)
                    {
                        // a real cancellation, triggered by the caller
                        APIResponse invalidResponse = new APIResponse();
                        invalidResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                        invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                        return invalidResponse;
                    }
                    else
                    {
                        // a web request timeout
                        APIResponse invalidResponse = new APIResponse();
                        invalidResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                        invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                        return invalidResponse;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogInstance.LogException(ex);
                    APIResponse invalidResponse = new APIResponse();
                    invalidResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                    return invalidResponse;
                }
            }
        }

        /// <summary>
        /// POST request
        /// </summary>
        /// <param name="address">API address</param>
        /// <param name="action">Action</param>
        /// <param name="corelationId">CorelationId</param>
        /// <param name="model">Model to send for post request</param>
        /// <param name="token">Authentication token</param>
        /// <returns>Generic Object</returns>
        public async Task<T> Post<T>(string action, object model = null)
        {
            Logger.LogInstance.LogDebug($"Post {action}");
            using (client = new HttpClient() { Timeout = TimeSpan.FromSeconds(TimeOutInSeconds) })
            {
                client.DefaultRequestHeaders.Accept.Add(
              new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add("accesskey", this.accessKey);

                string path = $"{this.rootUrl}/{action}";
                var cts = new CancellationTokenSource();
                string json = JsonConvert.SerializeObject(model);
                Logger.LogInstance.LogDebug(json);
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(path, content, cts.Token);
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<T>(jsonString);
                    Logger.LogInstance.LogDebug(jsonString);
                    return data;
                }
                catch(Exception ex)
                {
                    Logger.LogInstance.LogException(ex);
                    return default(T);
                }
            }
        }
        /// <summary>
        /// PUT request
        /// </summary>
        /// <param name="address">API address</param>
        /// <param name="action">Action</param>
        /// <param name="corelationId">CorelationId</param>
        /// <param name="model">Model to send for post request</param>
        /// <param name="token">Authentication token</param>
        /// <returns>APIResponse</returns>
        public async Task<IAPIResponse> Put(string action, object model = null, string token = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInstance.LogDebug($"Put {action}");
            using (client = new HttpClient() { Timeout = TimeSpan.FromSeconds(TimeOutInSeconds) })
            {
                client.DefaultRequestHeaders.Accept.Add(
              new MediaTypeWithQualityHeaderValue("application/json"));


                if (token != null)
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }


                client.DefaultRequestHeaders.Add("accesskey", this.accessKey);

                string path = $"{this.rootUrl}/{action}";

                CancellationToken cToken = cancellationToken;
                if (cancellationToken == default(CancellationToken))
                {
                    var cts = new CancellationTokenSource();
                    cToken = cts.Token;
                }

                string json = JsonConvert.SerializeObject(model);
                Logger.LogInstance.LogDebug(json);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    HttpResponseMessage response = await client.PutAsync(path, content, cToken);

                    var jsonString = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<APIResponse>(jsonString);
                    Logger.LogInstance.LogDebug(jsonString);
                    return data;
                }
                catch (TaskCanceledException ex)
                {
                    Logger.LogInstance.LogException(ex);
                    if (ex.CancellationToken == cToken)
                    {
                        // a real cancellation, triggered by the caller
                        APIResponse invalidResponse = new APIResponse();
                        invalidResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                        invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                        return invalidResponse;
                    }
                    else
                    {
                        // a web request timeout
                        APIResponse invalidResponse = new APIResponse();
                        invalidResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                        invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                        return invalidResponse;
                    }
                }
                catch(Exception ex)
                {
                    Logger.LogInstance.LogException(ex);
                    APIResponse invalidResponse = new APIResponse();
                    invalidResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                    return invalidResponse;
                }
            }
        }

        /// <summary>
        /// Delete request
        /// </summary>
        /// <param name="address">API address</param>
        /// <param name="action">Action</param>
        /// <param name="corelationId">CorelationId</param>
        /// <param name="token">Authentication token</param>
        public async Task<IAPIResponse> Delete(string action, string token = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogInstance.LogDebug($"Delete {action}");
            using (client = new HttpClient() { Timeout = TimeSpan.FromSeconds(TimeOutInSeconds) })
            {
                client.DefaultRequestHeaders.Accept.Add(
              new MediaTypeWithQualityHeaderValue("application/json"));


                if (token != null)
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                }


                client.DefaultRequestHeaders.Add("accesskey", this.accessKey);

                string path = $"{this.rootUrl}/{action}";

                CancellationToken cToken = cancellationToken;
                if (cancellationToken == default(CancellationToken))
                {
                    var cts = new CancellationTokenSource();
                    cToken = cts.Token;
                }

                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    HttpResponseMessage response = await client.DeleteAsync(path, cToken);
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Logger.LogInstance.LogDebug(jsonString);
                    var data = JsonConvert.DeserializeObject<APIResponse>(jsonString);

                    return data;
                }
                catch (TaskCanceledException ex)
                {
                    Logger.LogInstance.LogException(ex);
                    if (ex.CancellationToken == cToken)
                    {
                        // a real cancellation, triggered by the caller
                        APIResponse invalidResponse = new APIResponse();
                        invalidResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                        invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                        return invalidResponse;
                    }
                    else
                    {
                        // a web request timeout
                        APIResponse invalidResponse = new APIResponse();
                        invalidResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                        invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                        return invalidResponse;
                    }
                }
                catch(Exception ex)
                {
                    Logger.LogInstance.LogException(ex);
                    APIResponse invalidResponse = new APIResponse();
                    invalidResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                    invalidResponse.ResponseException = new ApiError() { IsError = true, ExceptionMessage = "Co-relation Id missing" };
                    return invalidResponse;
                }
            }
        }
    }
}
