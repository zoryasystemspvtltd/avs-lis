using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Notification;
using LIS.Logger;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LIS.BusinessLogic.Notifications
{
    public class NotificationHttpTransport : INotificationHttpTransport
    {
        private readonly ILogger logger;

        public NotificationHttpTransport(ILogger logger)
        {
            this.logger = logger;
        }

        public NotificationHttpResponse Send(NotificationHttpRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
            {
                return new NotificationHttpResponse
                {
                    Success = false,
                    ErrorMessage = "HTTP request URL is required."
                };
            }

            if (NotificationSettings.UseMockProviders())
            {
                return BuildMockResponse(request);
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 30);

                    if (!string.IsNullOrWhiteSpace(request.AuthorizationHeader))
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", request.AuthorizationHeader);
                    }

                    HttpResponseMessage response;
                    var method = (request.Method ?? "POST").ToUpperInvariant();
                    if (method == "GET")
                    {
                        response = client.GetAsync(request.Url).Result;
                    }
                    else
                    {
                        var content = new StringContent(request.Body ?? string.Empty, Encoding.UTF8, request.ContentType ?? "application/json");
                        response = client.PostAsync(request.Url, content).Result;
                    }

                    var body = response.Content.ReadAsStringAsync().Result;
                    return new NotificationHttpResponse
                    {
                        Success = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        ResponseBody = body,
                        ErrorMessage = response.IsSuccessStatusCode ? null : body
                    };
                }
            }
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException || ex.InnerException is TimeoutException)
            {
                logger.LogError("Notification HTTP timeout: " + request.Url);
                return new NotificationHttpResponse
                {
                    Success = false,
                    IsTimeout = true,
                    ErrorMessage = "Provider timeout."
                };
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
                return new NotificationHttpResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private static NotificationHttpResponse BuildMockResponse(NotificationHttpRequest request)
        {
            return new NotificationHttpResponse
            {
                Success = true,
                StatusCode = 200,
                ResponseBody = "{\"status\":\"MOCK_OK\"}"
            };
        }
    }
}
