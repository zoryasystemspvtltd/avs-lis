using LIS.BusinessLogic.Notifications;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.Notification;

namespace LIS.Masters.Tests.Notifications
{
    public sealed class MockNotificationHttpTransport : INotificationHttpTransport
    {
        public NotificationHttpResponse NextResponse { get; set; } = new NotificationHttpResponse
        {
            Success = true,
            StatusCode = 200,
            ResponseBody = "{\"status\":\"MOCK_OK\"}"
        };

        public NotificationHttpRequest LastRequest { get; private set; }

        public NotificationHttpResponse Send(NotificationHttpRequest request)
        {
            LastRequest = request;
            return NextResponse;
        }
    }
}
