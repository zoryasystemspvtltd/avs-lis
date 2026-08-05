using LIS.BusinessLogic.Helper;
using LIS.DtoModel.Models.Notification;
using System;
using System.Configuration;

namespace LIS.BusinessLogic.Notifications
{
    public static class NotificationSettings
    {
        public const string SmsApiUrlKey = "Notification:Sms:ApiUrl";
        public const string SmsUserNameKey = "Notification:Sms:UserName";
        public const string SmsPasswordKey = "Notification:Sms:Password";
        public const string SmsSenderIdKey = "Notification:Sms:SenderId";
        public const string WhatsAppApiUrlKey = "Notification:WhatsApp:ApiUrl";
        public const string WhatsAppTokenKey = "Notification:WhatsApp:Token";
        public const string WhatsAppPhoneNumberIdKey = "Notification:WhatsApp:PhoneNumberId";
        public const string RetryCountKey = "Notification:RetryCount";
        public const string RetryIntervalKey = "Notification:RetryInterval";
        public const string DefaultChannelKey = "Notification:DefaultChannel";
        public const string OrganizationNameKey = "Notification:OrganizationName";
        public const string SecureLinkBaseUrlKey = "Notification:SecureLinkBaseUrl";
        public const string UseMockProvidersKey = "Notification:UseMockProviders";
        public const string ProviderTimeoutSecondsKey = "Notification:ProviderTimeoutSeconds";

        public static string Get(string key)
        {
            return Config.GetConfigValue(key) ?? string.Empty;
        }

        public static int GetInt(string key, int fallback)
        {
            var raw = Get(key);
            int value;
            return int.TryParse(raw, out value) ? value : fallback;
        }

        public static NotificationChannelType GetDefaultChannel()
        {
            var raw = Get(DefaultChannelKey);
            if (string.Equals(raw, "WhatsApp", StringComparison.OrdinalIgnoreCase))
            {
                return NotificationChannelType.WhatsApp;
            }
            return NotificationChannelType.Sms;
        }

        public static bool UseMockProviders()
        {
            var raw = Get(UseMockProvidersKey);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }

            return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
                || raw == "1";
        }
    }
}
