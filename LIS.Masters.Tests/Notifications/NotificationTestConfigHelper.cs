using LIS.BusinessLogic.Notifications;
using System.Collections.Generic;
using System.Configuration;

namespace LIS.Masters.Tests.Notifications
{
    internal static class NotificationTestConfigHelper
    {
        private static readonly string[] ManagedKeys =
        {
            NotificationSettings.SmsApiUrlKey,
            NotificationSettings.SmsUserNameKey,
            NotificationSettings.SmsPasswordKey,
            NotificationSettings.SmsSenderIdKey,
            NotificationSettings.WhatsAppApiUrlKey,
            NotificationSettings.WhatsAppTokenKey,
            NotificationSettings.WhatsAppPhoneNumberIdKey,
            NotificationSettings.RetryCountKey,
            NotificationSettings.RetryIntervalKey,
            NotificationSettings.DefaultChannelKey,
            NotificationSettings.OrganizationNameKey,
            NotificationSettings.SecureLinkBaseUrlKey,
            NotificationSettings.UseMockProvidersKey,
            NotificationSettings.ProviderTimeoutSecondsKey
        };

        private static readonly Dictionary<string, string> OriginalValues = new Dictionary<string, string>();

        public static void SetSandboxValues()
        {
            Set(NotificationSettings.SmsApiUrlKey, "https://sms.sandbox.example.com/api/send");
            Set(NotificationSettings.SmsUserNameKey, "sandbox_sms_user");
            Set(NotificationSettings.SmsPasswordKey, "sandbox_sms_pass");
            Set(NotificationSettings.SmsSenderIdKey, "SANDBOX");
            Set(NotificationSettings.WhatsAppApiUrlKey, "https://graph.facebook.com/v18.0/");
            Set(NotificationSettings.WhatsAppTokenKey, "sandbox_wa_token");
            Set(NotificationSettings.WhatsAppPhoneNumberIdKey, "987654321012345");
            Set(NotificationSettings.RetryCountKey, "5");
            Set(NotificationSettings.RetryIntervalKey, "60");
            Set(NotificationSettings.DefaultChannelKey, "WhatsApp");
            Set(NotificationSettings.OrganizationNameKey, "Sandbox Laboratory");
            Set(NotificationSettings.SecureLinkBaseUrlKey, "https://sandbox.portal.example.com/api/report/download");
            Set(NotificationSettings.UseMockProvidersKey, "true");
            Set(NotificationSettings.ProviderTimeoutSecondsKey, "45");
        }

        public static void Set(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (!OriginalValues.ContainsKey(key))
            {
                OriginalValues[key] = ConfigurationManager.AppSettings[key];
            }

            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static void RestoreAll()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            foreach (var key in ManagedKeys)
            {
                if (!OriginalValues.ContainsKey(key))
                {
                    continue;
                }

                var originalValue = OriginalValues[key];
                if (string.IsNullOrEmpty(originalValue))
                {
                    if (config.AppSettings.Settings[key] != null)
                    {
                        config.AppSettings.Settings.Remove(key);
                    }
                }
                else if (config.AppSettings.Settings[key] == null)
                {
                    config.AppSettings.Settings.Add(key, originalValue);
                }
                else
                {
                    config.AppSettings.Settings[key].Value = originalValue;
                }
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            OriginalValues.Clear();
        }
    }
}
