using LIS.DataAccess;
using LIS.DataAccess.Repo;
using LIS.DtoModel;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Notification;
using LIS.Logger;
using System;
using System.Threading;

namespace LIS.BusinessLogic.Notifications
{
    public static class NotificationBackgroundDispatcher
    {
        private static readonly object Sync = new object();

        public static void ScheduleProcessing(long auditId)
        {
            ThreadPool.QueueUserWorkItem(_ => ProcessAuditInNewScope(auditId));
        }

        public static void SchedulePendingQueueProcessing()
        {
            ThreadPool.QueueUserWorkItem(_ => ProcessQueueInNewScope());
        }

        private static void ProcessAuditInNewScope(long auditId)
        {
            lock (Sync)
            {
                try
                {
                    using (var db = ApplicationDBContext.Create())
                    using (var uow = new GenericUnitOfWork(db))
                    {
                        var processor = BuildProcessor(uow);
                        processor.ProcessAudit(auditId);
                    }
                }
                catch (Exception ex)
                {
                    LIS.Logger.Logger.LogInstance.LogException(ex);
                }
            }
        }

        private static void ProcessQueueInNewScope()
        {
            lock (Sync)
            {
                try
                {
                    using (var db = ApplicationDBContext.Create())
                    using (var uow = new GenericUnitOfWork(db))
                    {
                        var processor = BuildProcessor(uow);
                        processor.ProcessPendingQueue();
                    }
                }
                catch (Exception ex)
                {
                    LIS.Logger.Logger.LogInstance.LogException(ex);
                }
            }
        }

        private static NotificationProcessor BuildProcessor(GenericUnitOfWork uow)
        {
            var logger = LIS.Logger.Logger.LogInstance;
            var identity = new SystemModuleIdentity();
            var configManager = new NotificationConfigurationManager(logger, identity, uow);
            var httpTransport = new NotificationHttpTransport(logger);
            var providerFactory = new NotificationProviderFactory(logger, httpTransport);
            return new NotificationProcessor(logger, identity, uow, configManager, providerFactory);
        }

        private sealed class SystemModuleIdentity : IModuleIdentity
        {
            public string ActivityMember { get { return "notification-processor"; } }
            public string AccessKey { get { return string.Empty; } }
        }
    }
}
