using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Notification;

namespace LIS.BusinessLogic.Notifications
{
    public class NotificationEventManager : INotificationEventManager
    {
        public string ResolveEventCode(int paymentStatus)
        {
            if (paymentStatus == (int)PaymentStatusType.Paid)
            {
                return NotificationEventCodes.ReportReadyPaid;
            }

            return NotificationEventCodes.ReportReadyPartialPayment;
        }

        public bool IsKnownEvent(string eventCode)
        {
            return eventCode == NotificationEventCodes.ReportReadyPaid
                || eventCode == NotificationEventCodes.ReportReadyPartialPayment;
        }
    }
}
