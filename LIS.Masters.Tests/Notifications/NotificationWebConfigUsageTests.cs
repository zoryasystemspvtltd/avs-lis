using LIS.BusinessLogic.Notifications;
using LIS.DtoModel.Models.Notification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LIS.Masters.Tests.Notifications
{
    [TestClass]
    public class NotificationWebConfigUsageTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            NotificationTestConfigHelper.SetSandboxValues();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            NotificationTestConfigHelper.RestoreAll();
        }

        [TestMethod]
        public void NotificationSettings_Reads_All_WebConfig_Keys()
        {
            Assert.AreEqual("https://sms.sandbox.example.com/api/send", NotificationSettings.Get(NotificationSettings.SmsApiUrlKey));
            Assert.AreEqual("sandbox_sms_user", NotificationSettings.Get(NotificationSettings.SmsUserNameKey));
            Assert.AreEqual("sandbox_sms_pass", NotificationSettings.Get(NotificationSettings.SmsPasswordKey));
            Assert.AreEqual("SANDBOX", NotificationSettings.Get(NotificationSettings.SmsSenderIdKey));
            Assert.AreEqual("https://graph.facebook.com/v18.0/", NotificationSettings.Get(NotificationSettings.WhatsAppApiUrlKey));
            Assert.AreEqual("sandbox_wa_token", NotificationSettings.Get(NotificationSettings.WhatsAppTokenKey));
            Assert.AreEqual("987654321012345", NotificationSettings.Get(NotificationSettings.WhatsAppPhoneNumberIdKey));
            Assert.AreEqual(5, NotificationSettings.GetInt(NotificationSettings.RetryCountKey, 0));
            Assert.AreEqual(60, NotificationSettings.GetInt(NotificationSettings.RetryIntervalKey, 0));
            Assert.AreEqual(NotificationChannelType.WhatsApp, NotificationSettings.GetDefaultChannel());
            Assert.AreEqual("Sandbox Laboratory", NotificationSettings.Get(NotificationSettings.OrganizationNameKey));
            Assert.AreEqual("https://sandbox.portal.example.com/api/report/download", NotificationSettings.Get(NotificationSettings.SecureLinkBaseUrlKey));
            Assert.IsTrue(NotificationSettings.UseMockProviders());
            Assert.AreEqual(45, NotificationSettings.GetInt(NotificationSettings.ProviderTimeoutSecondsKey, 0));
        }

        [TestMethod]
        public void SmsProvider_Uses_All_Sms_WebConfig_Keys_In_Api_Request()
        {
            var transport = new MockNotificationHttpTransport();
            var provider = new SmsNotificationProvider(LIS.Logger.Logger.LogInstance, transport);
            var request = BuildProviderRequest();

            var result = provider.Send(request);

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(transport.LastRequest);
            Assert.AreEqual("https://sms.sandbox.example.com/api/send", transport.LastRequest.Url);
            Assert.AreEqual("POST", transport.LastRequest.Method);
            Assert.AreEqual("application/json", transport.LastRequest.ContentType);
            Assert.AreEqual("sandbox_sms_user", transport.LastRequest.UserName);
            Assert.AreEqual("sandbox_sms_pass", transport.LastRequest.Password);
            Assert.AreEqual(BuildBasicAuth("sandbox_sms_user", "sandbox_sms_pass"), transport.LastRequest.AuthorizationHeader);
            Assert.AreEqual(45, transport.LastRequest.TimeoutSeconds);

            var body = JObject.Parse(transport.LastRequest.Body);
            Assert.AreEqual("SANDBOX", body.Value<string>("sender"));
            Assert.AreEqual("9999999999", body.Value<string>("to"));
            Assert.AreEqual("Report ready", body.Value<string>("message"));
            Assert.AreEqual("corr-sms-1", body.Value<string>("correlationId"));
            Assert.AreEqual("INV-SMS-1", body.Value<string>("invoiceNo"));
        }

        [TestMethod]
        public void WhatsAppProvider_Uses_All_WhatsApp_WebConfig_Keys_In_Api_Request()
        {
            var transport = new MockNotificationHttpTransport();
            var provider = new WhatsAppNotificationProvider(LIS.Logger.Logger.LogInstance, transport);
            var request = BuildProviderRequest();

            var result = provider.Send(request);

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(transport.LastRequest);
            Assert.AreEqual(
                "https://graph.facebook.com/v18.0/987654321012345/messages",
                transport.LastRequest.Url);
            Assert.AreEqual("POST", transport.LastRequest.Method);
            Assert.AreEqual("application/json", transport.LastRequest.ContentType);
            Assert.AreEqual("Bearer sandbox_wa_token", transport.LastRequest.AuthorizationHeader);
            Assert.AreEqual(45, transport.LastRequest.TimeoutSeconds);

            var body = JObject.Parse(transport.LastRequest.Body);
            Assert.AreEqual("whatsapp", body.Value<string>("messaging_product"));
            Assert.AreEqual("9999999999", body.Value<string>("to"));
            Assert.AreEqual("text", body.Value<string>("type"));
            Assert.AreEqual("Report ready", body["text"]?["body"]?.ToString());
        }

        [TestMethod]
        public void SecureLink_And_Organization_Are_Composed_From_WebConfig()
        {
            var baseUrl = NotificationSettings.Get(NotificationSettings.SecureLinkBaseUrlKey);
            var organization = NotificationSettings.Get(NotificationSettings.OrganizationNameKey);
            var secureToken = "secure-token-123";

            var downloadLink = string.Format("{0}/{1}", baseUrl.TrimEnd('/'), secureToken);

            Assert.AreEqual("Sandbox Laboratory", organization);
            Assert.AreEqual("https://sandbox.portal.example.com/api/report/download/secure-token-123", downloadLink);
        }

        [TestMethod]
        public void NotificationHttpTransport_Uses_Mock_When_UseMockProviders_Is_True()
        {
            NotificationTestConfigHelper.Set(NotificationSettings.UseMockProvidersKey, "true");

            var transport = new NotificationHttpTransport(LIS.Logger.Logger.LogInstance);
            var response = transport.Send(new NotificationHttpRequest
            {
                Url = "https://invalid.example.com/should-not-be-called",
                Method = "POST",
                Body = "{}",
                TimeoutSeconds = 30
            });

            Assert.IsTrue(response.Success);
            StringAssert.Contains(response.ResponseBody, "MOCK_OK");
        }

        [TestMethod]
        public void NotificationHttpTransport_Posts_To_Configured_Url_When_UseMockProviders_Is_False()
        {
            NotificationTestConfigHelper.Set(NotificationSettings.UseMockProvidersKey, "false");

            var listener = StartHttpListener(out var prefix);
            try
            {
                var received = new ManualResetEventSlim(false);
                string receivedBody = null;
                string receivedAuth = null;

                listener.BeginGetContext(ar =>
                {
                    var context = listener.EndGetContext(ar);
                    receivedAuth = context.Request.Headers["Authorization"];
                    using (var reader = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                    {
                        receivedBody = reader.ReadToEnd();
                    }

                    var buffer = Encoding.UTF8.GetBytes("{\"status\":\"OK\"}");
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Close();
                    received.Set();
                }, null);

                var transport = new NotificationHttpTransport(LIS.Logger.Logger.LogInstance);
                var response = transport.Send(new NotificationHttpRequest
                {
                    Url = prefix.TrimEnd('/') + "/messages",
                    Method = "POST",
                    Body = "{\"channel\":\"whatsapp\"}",
                    ContentType = "application/json",
                    AuthorizationHeader = "Bearer sandbox_wa_token",
                    TimeoutSeconds = 45
                });

                Assert.IsTrue(received.Wait(TimeSpan.FromSeconds(5)), "HTTP listener did not receive request.");
                Assert.IsTrue(response.Success);
                Assert.AreEqual(200, response.StatusCode);
                Assert.AreEqual("Bearer sandbox_wa_token", receivedAuth);
                Assert.AreEqual("{\"channel\":\"whatsapp\"}", receivedBody);
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }
        }

        private static NotificationProviderRequest BuildProviderRequest()
        {
            return new NotificationProviderRequest
            {
                RecipientPhone = "9999999999",
                MessageBody = "Report ready",
                EventCode = NotificationEventCodes.ReportReadyPaid,
                InvoiceNo = "INV-SMS-1",
                CorrelationId = "corr-sms-1",
                NotificationRequest = new NotificationRequest { Event = NotificationEventCodes.ReportReadyPaid }
            };
        }

        private static string BuildBasicAuth(string userName, string password)
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + (password ?? string.Empty)));
            return "Basic " + token;
        }

        private static HttpListener StartHttpListener(out string prefix)
        {
            var port = GetFreeTcpPort();
            prefix = "http://127.0.0.1:" + port + "/";
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
            return listener;
        }

        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
