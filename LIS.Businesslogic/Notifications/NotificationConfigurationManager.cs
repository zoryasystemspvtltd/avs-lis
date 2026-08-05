using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Notification;
using LIS.Logger;
using System;
using System.Linq;

namespace LIS.BusinessLogic.Notifications
{
    public class NotificationConfigurationManager : INotificationConfigurationManager
    {
        private readonly ModuleRepo<NotificationConfiguration> configRepo;
        private readonly IModuleIdentity identity;

        public NotificationConfigurationManager(ILogger logger, IModuleIdentity identity, GenericUnitOfWork unitOfWork)
        {
            configRepo = new ModuleRepo<NotificationConfiguration>(logger, identity, unitOfWork);
            this.identity = identity;
        }

        public NotificationConfiguration GetConfiguration()
        {
            var config = configRepo.Get().OrderBy(c => c.Id).FirstOrDefault();
            if (config == null)
            {
                return BuildDefaultConfiguration();
            }

            return config;
        }

        public NotificationConfigurationDto GetConfigurationDto()
        {
            return ToDto(GetConfiguration());
        }

        public void SaveConfiguration(NotificationConfigurationDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            var config = configRepo.Get().OrderBy(c => c.Id).FirstOrDefault();
            var now = DateTime.Now;
            var user = identity?.ActivityMember ?? "system";

            if (config == null)
            {
                config = new NotificationConfiguration
                {
                    CreatedOn = now,
                    CreatedBy = user
                };
            }

            config.IsEnabled = dto.IsEnabled;
            config.SmsEnabled = dto.SmsEnabled;
            config.WhatsAppEnabled = dto.WhatsAppEnabled;
            config.ChannelMode = dto.ChannelMode;
            config.RetryCount = dto.RetryCount > 0 ? dto.RetryCount : NotificationSettings.GetInt(NotificationSettings.RetryCountKey, 3);
            config.RetryIntervalSeconds = dto.RetryIntervalSeconds > 0 ? dto.RetryIntervalSeconds : NotificationSettings.GetInt(NotificationSettings.RetryIntervalKey, 30);
            config.DefaultChannel = dto.DefaultChannel > 0 ? dto.DefaultChannel : (int)NotificationSettings.GetDefaultChannel();
            config.ModifiedOn = now;
            config.ModifiedBy = user;

            if (config.Id > 0)
            {
                configRepo.Update(config);
            }
            else
            {
                configRepo.Add(config);
            }
        }

        public bool IsEnabled()
        {
            return GetConfiguration().IsEnabled;
        }

        private static NotificationConfiguration BuildDefaultConfiguration()
        {
            return new NotificationConfiguration
            {
                IsEnabled = false,
                SmsEnabled = true,
                WhatsAppEnabled = true,
                ChannelMode = (int)NotificationChannelMode.Both,
                RetryCount = NotificationSettings.GetInt(NotificationSettings.RetryCountKey, 3),
                RetryIntervalSeconds = NotificationSettings.GetInt(NotificationSettings.RetryIntervalKey, 30),
                DefaultChannel = (int)NotificationSettings.GetDefaultChannel()
            };
        }

        private static NotificationConfigurationDto ToDto(NotificationConfiguration config)
        {
            return new NotificationConfigurationDto
            {
                Id = config.Id,
                IsEnabled = config.IsEnabled,
                SmsEnabled = config.SmsEnabled,
                WhatsAppEnabled = config.WhatsAppEnabled,
                ChannelMode = config.ChannelMode,
                RetryCount = config.RetryCount,
                RetryIntervalSeconds = config.RetryIntervalSeconds,
                DefaultChannel = config.DefaultChannel
            };
        }
    }
}
