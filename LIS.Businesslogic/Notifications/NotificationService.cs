using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Notification;

namespace LIS.BusinessLogic.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationManager notificationManager;

        public NotificationService(INotificationManager notificationManager)
        {
            this.notificationManager = notificationManager;
        }

        public void EnqueueReportReleasedNotification(ReportReleasedNotificationContext context)
        {
            notificationManager.RaiseNotificationEvent(context);
        }
    }
}
