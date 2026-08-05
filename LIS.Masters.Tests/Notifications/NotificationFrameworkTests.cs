using LIS.BusinessLogic.Notifications;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Notification;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LIS.Masters.Tests.Notifications
{
    [TestClass]
    public class NotificationFrameworkTests
    {
        [TestMethod]
        public void TemplateEngine_Replaces_All_Placeholders()
        {
            var context = new NotificationPlaceholderContext
            {
                PatientName = "John Doe",
                PatientNo = "PAT001",
                MRNo = "MR001",
                VisitId = "VIS001",
                InvoiceNo = "INV-20260805-0001",
                OutstandingAmount = "250.00",
                ReportType = "Laboratory",
                OrganizationName = "Zorya Laboratory",
                ReportDownloadLink = "https://example.com/secure/token",
                CurrentDate = new DateTime(2026, 8, 5),
                CurrentTime = new DateTime(2026, 8, 5, 14, 30, 0)
            };

            var body = "Dear {PatientName}, Invoice {InvoiceNo}, Due {OutstandingAmount}, Link {ReportDownloadLink}, Org {OrganizationName}, Date {CurrentDate}, Time {CurrentTime}";
            var resolved = NotificationTemplateEngine.ReplacePlaceholders(body, context);

            StringAssert.Contains(resolved, "John Doe");
            StringAssert.Contains(resolved, "INV-20260805-0001");
            StringAssert.Contains(resolved, "250.00");
            StringAssert.Contains(resolved, "https://example.com/secure/token");
            StringAssert.Contains(resolved, "Zorya Laboratory");
            StringAssert.Contains(resolved, "05-Aug-2026");
            StringAssert.Contains(resolved, "02:30 PM");
        }

        [TestMethod]
        public void EventManager_Resolves_Paid_And_Partial_Events()
        {
            var manager = new NotificationEventManager();

            Assert.AreEqual(NotificationEventCodes.ReportReadyPaid, manager.ResolveEventCode((int)PaymentStatusType.Paid));
            Assert.AreEqual(NotificationEventCodes.ReportReadyPartialPayment, manager.ResolveEventCode((int)PaymentStatusType.Partial));
            Assert.AreEqual(NotificationEventCodes.ReportReadyPartialPayment, manager.ResolveEventCode((int)PaymentStatusType.Unpaid));
            Assert.IsTrue(manager.IsKnownEvent(NotificationEventCodes.ReportReadyPaid));
            Assert.IsFalse(manager.IsKnownEvent("UnknownEvent"));
        }

        [TestMethod]
        public void ProviderFactory_Returns_Sms_And_WhatsApp_Providers()
        {
            var factory = new NotificationProviderFactory(LIS.Logger.Logger.LogInstance, new MockNotificationHttpTransport());

            var sms = factory.GetProvider(NotificationChannelType.Sms);
            var whatsApp = factory.GetProvider(NotificationChannelType.WhatsApp);

            Assert.AreEqual(NotificationChannelType.Sms, sms.Channel);
            Assert.AreEqual(NotificationChannelType.WhatsApp, whatsApp.Channel);

            var both = factory.GetProvidersForConfiguration(new NotificationConfiguration
            {
                IsEnabled = true,
                ChannelMode = (int)NotificationChannelMode.Both,
                SmsEnabled = true,
                WhatsAppEnabled = true
            });
            Assert.AreEqual(2, System.Linq.Enumerable.Count(both));
        }

        [TestMethod]
        public void MockSmsProvider_Returns_Success_And_Logs()
        {
            var provider = new SmsNotificationProvider(LIS.Logger.Logger.LogInstance, new MockNotificationHttpTransport());
            var result = provider.Send(new NotificationProviderRequest
            {
                RecipientPhone = "9999999999",
                MessageBody = "Test message",
                EventCode = NotificationEventCodes.ReportReadyPaid,
                InvoiceNo = "INV-TEST"
            });

            Assert.IsTrue(result.Success);
            StringAssert.Contains(result.ProviderResponse, "MOCK_OK");
        }

        [TestMethod]
        public void MockWhatsAppProvider_Returns_Success()
        {
            var provider = new WhatsAppNotificationProvider(LIS.Logger.Logger.LogInstance, new MockNotificationHttpTransport());
            var result = provider.Send(new NotificationProviderRequest
            {
                RecipientPhone = "9999999999",
                MessageBody = "Test message",
                EventCode = NotificationEventCodes.ReportReadyPartialPayment,
                InvoiceNo = "INV-TEST"
            });

            Assert.IsTrue(result.Success);
            StringAssert.Contains(result.ProviderResponse, "MOCK_OK");
        }

        [TestMethod]
        public void TemplateStore_Provides_Default_Templates_For_Both_Events()
        {
            var paidSms = NotificationTemplateStore.GetDefaultTemplate(NotificationEventCodes.ReportReadyPaid, NotificationChannelType.Sms);
            var partialWhatsApp = NotificationTemplateStore.GetDefaultTemplate(NotificationEventCodes.ReportReadyPartialPayment, NotificationChannelType.WhatsApp);

            StringAssert.Contains(paidSms.Body, "{ReportDownloadLink}");
            StringAssert.Contains(partialWhatsApp.Body, "{OutstandingAmount}");
        }

        [TestMethod]
        public void ConfigurationManager_Default_Is_Disabled()
        {
            if (!IntegrationTestBase.DatabaseAvailable)
            {
                Assert.Inconclusive("Integration database unavailable.");
            }

            if (!TestServiceFactory.TryCreate(out var factory, out var error))
            {
                Assert.Inconclusive(error);
            }

            using (factory)
            {
                var manager = new NotificationConfigurationManager(factory.Logger, factory.Identity, factory.Uow);
                var config = manager.GetConfiguration();
                Assert.IsNotNull(config);
            }
        }
    }
}
