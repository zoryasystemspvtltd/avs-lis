using LIS.BusinessLogic.Helper;
using LIS.DtoModel.Interfaces;
using LIS.DtoModel.Models.ExternalApi;
using LIS.Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Lis.Api
{
    public static class GlobalScheduler
    {
        private static ILogger _logger;
        private static System.Timers.Timer _timer;
        private static readonly int SchedulerIntervalMinute = Convert.ToInt32(ConfigurationManager.AppSettings["SchedulerIntervalMinute"]);
        private static readonly string HospitalApiUrl = ConfigurationManager.AppSettings["HospitalApiUrl"];
        private static readonly string ExternalAPIBaseUri = ConfigurationManager.AppSettings["ExternalAPIBaseUri"];
        private static bool isRunning = false;
        public static void StartScheduler(ILogger logger)
        {
            _logger = logger;

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = 1000 * 60 * SchedulerIntervalMinute; // One Hour
            _timer.Enabled = true;
            isRunning = false;
            _logger.LogDebug("Synchronization Scheduler Enabled.");
        }

        private static async void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _logger.LogInfo("Ping His Adapter Elapsed Started.");
            await PingHisAdapterAsync(); // 1 Hour
            _logger.LogInfo("Ping His Adapter Elapsed End.");

            // Schedular is replaced by background Strored Procedure
            /*
            _logger.LogInfo("Synchronization Scheduler Elapsed Started.");
            if (!isRunning)
            {
                await SyncTestRequisitionAsync(); // 1 Hour
            }
            _logger.LogInfo("Synchronization Scheduler Elapsed End.");
            */
        }

        private static async Task PingHisAdapterAsync()
        {
            HttpResponseMessage responseMessage = null;
            try
            {
                _logger.LogInfo("Ping His Adapter Started.");

                using (var client = new ApiClient().GetHttpClient())
                {
                    responseMessage = await client.GetAsync($"{ExternalAPIBaseUri}api/ping");
                }

                _logger.LogInfo("Ping His Adapter End.");
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
        }

        private static async Task SyncTestRequisitionAsync()
        {
            HttpResponseMessage responseMessage = null;
            try
            {
                _logger.LogInfo("Synchronization Test Requisition Started.");
                var testDetails = new List<TestDetail>();

                using (var client = new ApiClient().GetHttpClient())
                {
                    isRunning = true;
                    responseMessage = await client.GetAsync(HospitalApiUrl);
                    isRunning = false;
                }

                _logger.LogInfo("Synchronization Test Requisition End.");
            }
            catch (Exception ex)
            {
                isRunning = false;
                _logger.LogException(ex);
            }
        }
    }
}