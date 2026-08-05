using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Notification;
using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace LIS.BusinessLogic.Notifications
{
    public class SmsNotificationProvider : INotificationProvider
    {
        private readonly ILogger logger;
        private readonly INotificationHttpTransport httpTransport;

        public SmsNotificationProvider(ILogger logger, INotificationHttpTransport httpTransport)
        {
            this.logger = logger;
            this.httpTransport = httpTransport;
        }

        public NotificationChannelType Channel
        {
            get { return NotificationChannelType.Sms; }
        }

        public string ProviderName
        {
            get { return "Sms"; }
        }

        public NotificationProviderResult Send(NotificationProviderRequest request)
        {
            if (request == null)
            {
                return Fail("Request is required.");
            }

            var apiUrl = NotificationSettings.Get(NotificationSettings.SmsApiUrlKey);
            var userName = NotificationSettings.Get(NotificationSettings.SmsUserNameKey);
            var password = NotificationSettings.Get(NotificationSettings.SmsPasswordKey);
            var senderId = NotificationSettings.Get(NotificationSettings.SmsSenderIdKey);

            var payload = JsonConvert.SerializeObject(new
            {
                sender = senderId,
                to = request.RecipientPhone,
                message = request.MessageBody,
                correlationId = request.CorrelationId,
                invoiceNo = request.InvoiceNo
            });

            var httpRequest = new NotificationHttpRequest
            {
                Url = string.IsNullOrWhiteSpace(apiUrl) ? "mock://sms/send" : apiUrl,
                Method = "POST",
                Body = payload,
                ContentType = "application/json",
                UserName = userName,
                Password = password,
                AuthorizationHeader = BuildBasicAuth(userName, password),
                TimeoutSeconds = NotificationSettings.GetInt(NotificationSettings.ProviderTimeoutSecondsKey, 30)
            };

            var response = httpTransport.Send(httpRequest);
            if (response.Success)
            {
                var responseText = string.IsNullOrWhiteSpace(response.ResponseBody)
                    ? string.Format("SMS_SUCCESS to={0}", request.RecipientPhone)
                    : response.ResponseBody;
                logger.LogInfo("Notification SMS send: " + responseText);
                return Success(responseText);
            }

            return Fail(response.IsTimeout ? "SMS provider timeout." : response.ErrorMessage);
        }

        private static string BuildBasicAuth(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + (password ?? string.Empty)));
            return "Basic " + token;
        }

        private static NotificationProviderResult Success(string response)
        {
            return new NotificationProviderResult { Success = true, ProviderResponse = response };
        }

        private static NotificationProviderResult Fail(string message)
        {
            return new NotificationProviderResult { Success = false, ErrorMessage = message };
        }
    }

    public class WhatsAppNotificationProvider : INotificationProvider
    {
        private readonly ILogger logger;
        private readonly INotificationHttpTransport httpTransport;

        public WhatsAppNotificationProvider(ILogger logger, INotificationHttpTransport httpTransport)
        {
            this.logger = logger;
            this.httpTransport = httpTransport;
        }

        public NotificationChannelType Channel
        {
            get { return NotificationChannelType.WhatsApp; }
        }

        public string ProviderName
        {
            get { return "WhatsApp"; }
        }

        public NotificationProviderResult Send(NotificationProviderRequest request)
        {
            if (request == null)
            {
                return Fail("Request is required.");
            }

            var apiUrl = NotificationSettings.Get(NotificationSettings.WhatsAppApiUrlKey);
            var token = NotificationSettings.Get(NotificationSettings.WhatsAppTokenKey);
            var phoneNumberId = NotificationSettings.Get(NotificationSettings.WhatsAppPhoneNumberIdKey);

            var payload = JsonConvert.SerializeObject(new
            {
                messaging_product = "whatsapp",
                to = request.RecipientPhone,
                type = "text",
                text = new { body = request.MessageBody }
            });

            var endpoint = string.IsNullOrWhiteSpace(apiUrl)
                ? "mock://whatsapp/messages"
                : apiUrl.TrimEnd('/') + "/" + phoneNumberId + "/messages";

            var httpRequest = new NotificationHttpRequest
            {
                Url = endpoint,
                Method = "POST",
                Body = payload,
                ContentType = "application/json",
                AuthorizationHeader = string.IsNullOrWhiteSpace(token) ? null : "Bearer " + token,
                TimeoutSeconds = NotificationSettings.GetInt(NotificationSettings.ProviderTimeoutSecondsKey, 30)
            };

            var response = httpTransport.Send(httpRequest);
            if (response.Success)
            {
                var responseText = string.IsNullOrWhiteSpace(response.ResponseBody)
                    ? string.Format("WHATSAPP_SUCCESS to={0}", request.RecipientPhone)
                    : response.ResponseBody;
                logger.LogInfo("Notification WhatsApp send: " + responseText);
                return Success(responseText);
            }

            return Fail(response.IsTimeout ? "WhatsApp provider timeout." : response.ErrorMessage);
        }

        private static NotificationProviderResult Success(string response)
        {
            return new NotificationProviderResult { Success = true, ProviderResponse = response };
        }

        private static NotificationProviderResult Fail(string message)
        {
            return new NotificationProviderResult { Success = false, ErrorMessage = message };
        }
    }

    public class NotificationProviderFactory : INotificationProviderFactory
    {
        private readonly Dictionary<NotificationChannelType, INotificationProvider> providers;

        public NotificationProviderFactory(ILogger logger, INotificationHttpTransport httpTransport)
        {
            providers = new Dictionary<NotificationChannelType, INotificationProvider>
            {
                { NotificationChannelType.Sms, new SmsNotificationProvider(logger, httpTransport) },
                { NotificationChannelType.WhatsApp, new WhatsAppNotificationProvider(logger, httpTransport) }
            };
        }

        public INotificationProvider GetProvider(NotificationChannelType channel)
        {
            INotificationProvider provider;
            if (providers.TryGetValue(channel, out provider))
            {
                return provider;
            }

            throw new InvalidOperationException("Unsupported notification channel: " + channel);
        }

        public IEnumerable<INotificationProvider> GetProvidersForConfiguration(NotificationConfiguration config)
        {
            if (config == null || !config.IsEnabled)
            {
                yield break;
            }

            var mode = (NotificationChannelMode)config.ChannelMode;
            if (mode == NotificationChannelMode.Both)
            {
                if (config.SmsEnabled)
                {
                    yield return GetProvider(NotificationChannelType.Sms);
                }
                if (config.WhatsAppEnabled)
                {
                    yield return GetProvider(NotificationChannelType.WhatsApp);
                }
                yield break;
            }

            if (mode == NotificationChannelMode.WhatsApp && config.WhatsAppEnabled)
            {
                yield return GetProvider(NotificationChannelType.WhatsApp);
                yield break;
            }

            if (mode == NotificationChannelMode.Sms && config.SmsEnabled)
            {
                yield return GetProvider(NotificationChannelType.Sms);
                yield break;
            }

            var fallback = config.DefaultChannel == (int)NotificationChannelType.WhatsApp
                ? NotificationChannelType.WhatsApp
                : NotificationChannelType.Sms;

            if ((fallback == NotificationChannelType.Sms && config.SmsEnabled)
                || (fallback == NotificationChannelType.WhatsApp && config.WhatsAppEnabled))
            {
                yield return GetProvider(fallback);
            }
        }
    }
}
