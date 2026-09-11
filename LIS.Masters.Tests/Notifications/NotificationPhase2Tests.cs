using LIS.BusinessLogic.Notifications;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models;
using LIS.DtoModel.Models.Notification;
using LIS.Masters.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LIS.Masters.Tests.Notifications
{
    [TestClass]
    public class NotificationPhase2Tests
    {
        [TestMethod]
        public void ProviderFactory_Respects_Sms_And_WhatsApp_Enabled_Flags()
        {
            var factory = new NotificationProviderFactory(LIS.Logger.Logger.LogInstance, new MockNotificationHttpTransport());
            var config = new NotificationConfiguration
            {
                IsEnabled = true,
                ChannelMode = (int)NotificationChannelMode.Both,
                SmsEnabled = true,
                WhatsAppEnabled = false
            };

            var providers = factory.GetProvidersForConfiguration(config).ToList();
            Assert.AreEqual(1, providers.Count);
            Assert.AreEqual(NotificationChannelType.Sms, providers[0].Channel);
        }

        [TestMethod]
        public void SmsProvider_Uses_Mock_Transport_When_Configured()
        {
            var transport = new MockNotificationHttpTransport();
            var provider = new SmsNotificationProvider(LIS.Logger.Logger.LogInstance, transport);
            var result = provider.Send(new NotificationProviderRequest
            {
                RecipientPhone = "9999999999",
                MessageBody = "Hello",
                EventCode = NotificationEventCodes.ReportReadyPaid,
                InvoiceNo = "INV-1",
                CorrelationId = "corr-1",
                NotificationRequest = new NotificationRequest { Event = NotificationEventCodes.ReportReadyPaid }
            });

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public void WhatsAppProvider_Returns_Failure_On_Transport_Error()
        {
            var transport = new MockNotificationHttpTransport
            {
                NextResponse = new NotificationHttpResponse
                {
                    Success = false,
                    ErrorMessage = "Provider unavailable"
                }
            };
            var provider = new WhatsAppNotificationProvider(LIS.Logger.Logger.LogInstance, transport);
            var result = provider.Send(new NotificationProviderRequest
            {
                RecipientPhone = "9999999999",
                MessageBody = "Hello",
                EventCode = NotificationEventCodes.ReportReadyPartialPayment,
                InvoiceNo = "INV-2"
            });

            Assert.IsFalse(result.Success);
            StringAssert.Contains(result.ErrorMessage, "Provider unavailable");
        }

        [TestMethod]
        public void TemplateEngine_Supports_Versioned_Active_Template_Fields()
        {
            var template = NotificationTemplateStore.GetDefaultTemplate(
                NotificationEventCodes.ReportReadyPaid,
                NotificationChannelType.Sms);
            Assert.IsNotNull(template);
            Assert.IsTrue(template.Version >= 1);
        }

        [TestMethod]
        public void Partial_Payment_Template_Does_Not_Require_Download_Link()
        {
            var template = NotificationTemplateStore.GetDefaultTemplate(
                NotificationEventCodes.ReportReadyPartialPayment,
                NotificationChannelType.Sms);
            StringAssert.Contains(template.Body, "{OutstandingAmount}");
            Assert.IsFalse(template.Body.Contains("{ReportDownloadLink}"));
        }

        [TestMethod]
        public void SecureDownload_Rejects_Invalid_Token()
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
                var manager = new NotificationSecureDownloadManager(
                    factory.Logger,
                    factory.Identity,
                    factory.Uow,
                    new TestReportManagerStub());
                var result = manager.ValidateAndGetReport("invalid-token-value");
                Assert.IsFalse(result.Success);
            }
        }

        [TestMethod]
        public void Processor_Does_Not_Retry_Successful_Notification()
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
                var configManager = new NotificationConfigurationManager(factory.Logger, factory.Identity, factory.Uow);
                configManager.SaveConfiguration(new NotificationConfigurationDto
                {
                    IsEnabled = true,
                    SmsEnabled = true,
                    WhatsAppEnabled = true,
                    ChannelMode = (int)NotificationChannelMode.Sms,
                    RetryCount = 2,
                    RetryIntervalSeconds = 1,
                    DefaultChannel = (int)NotificationChannelType.Sms
                });

                var transport = new MockNotificationHttpTransport();
                var providerFactory = new NotificationProviderFactory(factory.Logger, transport);
                var processor = new NotificationProcessor(
                    factory.Logger,
                    factory.Identity,
                    factory.Uow,
                    configManager,
                    providerFactory);

                var audit = new NotificationAudit
                {
                    EventCode = NotificationEventCodes.ReportReadyPaid,
                    Channel = (int)NotificationChannelType.Sms,
                    RecipientPhone = "9999999999",
                    MessageBody = "Test",
                    InvoiceNo = "INV-PROC-1",
                    Status = (int)NotificationStatusType.Pending,
                    RetryCount = 0,
                    CorrelationId = Guid.NewGuid().ToString("N")
                };
                var auditId = factory.NotificationManager.Enqueue(audit);
                processor.ProcessAudit(auditId);

                var history = factory.NotificationManager.GetAuditHistory(10).ToList();
                var processed = history.FirstOrDefault(h => h.Id == auditId);
                Assert.IsNotNull(processed);
                Assert.AreEqual("Sent", processed.Status);
                Assert.AreEqual(0, processed.RetryCount);
            }
        }

        private sealed class TestReportManagerStub : ITestReportManager
        {
            public DiagnosticTestReportDto GetDiagnosticTestReport(string labNo, string invoiceNo, long? testRequestDetailId = null)
            {
                return new DiagnosticTestReportDto();
            }

            public System.Collections.Generic.IEnumerable<TestReportLabNoOption> GetPrintableLabNumbers()
            {
                return new TestReportLabNoOption[0];
            }
        }
    }
}
