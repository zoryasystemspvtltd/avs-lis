using Lis.Api.Client;
using LIS.DtoModel;
using LIS.DtoModel.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LIS.Com.Businesslogic
{
    public class LisContext
    {
        private static LisContext context;
        public static LisContext LisDOM
        {
            get
            {
                if (context == null)
                {
                    context = new LisContext();
                }
                return context;
            }
        }
        System.Timers.Timer timer;
        private ICommunicationChannel api;
        private LisContext()
        {
            timer = new System.Timers.Timer();
            timer.Elapsed += Timer_Elapsed; ;
            timer.Interval = 1000 * 60; // One Minute
            timer.Enabled = true;
        }

        private async void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await SendHeartBeat(true);
        }

        public Token User { get; private set; }

        public async Task<bool> Login(string userName, string password)
        {
            bool isValid = false;

            string authRequest = $"grant_type=password&username={userName}&password={password}";

            this.User = await api.Authenticate("Token", authRequest);

            if (this.User.AccessToken != null)
            {
                //var roles = await api.Get($"api/UserAccess/{accessToken}", null, this.User.AccessToken);
                isValid = true;
            }

            return isValid;
        }

        public bool IsCommandReady
        {
            get
            {
                bool isReady = false;
                if (this.Command != null)
                {
                    isReady = true;
                }
                return isReady;
            }
        }
        public SerialCommand Command { get; private set; }
        public TCPIPCommand TcpIpCommand { get; private set; }
        public void InitSerialCommand(PortSettings settings, EquipmentType type)
        {
            switch (type)
            {
                //case EquipmentType.DxI800:
                //    this.Command = new DxI800SerialCommand(settings);
                //    break;
                //case EquipmentType.DxH800:
                //    this.Command = new DxH800SerialCommand(settings);
                //    break;
                //case EquipmentType.DxC700:
                //    this.Command = new DxC700SerialCommand(settings);
                //    break;
                //case EquipmentType.E411:
                //    this.Command = new E411SerialCommand(settings);
                //    break;
                //case EquipmentType.D10:
                //    this.Command = new D10SerialCommand(settings);
                //    break;
                //case EquipmentType.XN1000:
                //    this.Command = new XN1000SerialCommand(settings);
                //    break;
                //case EquipmentType.CA600:
                //    this.Command = new CA600SerialCommand(settings);
                //    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void InitTCPIPCommand(TCPIPSettings settings, EquipmentType type)
        {
            this.TcpIpCommand = new Metis6000TCPIPCommand(settings);
        }

        public void InitAPI(string serverUrl, string apiKey)
        {
            this.api = new CommunicationChannel(serverUrl, apiKey);
        }

        public async Task<bool> IsPanelTest(string sampleNo, string lisHostCode)
        {
            bool isPanel = false;

            var response = await api.Get($"api/Lis/?SampleNo={sampleNo}&LisHostCode={lisHostCode}", null, null);

            if (response.StatusCode == 200 && response.Result != null)
            {
                isPanel = (bool)response.Result;
            }
            return isPanel;
        }

        public async Task<IEnumerable<TestRequestDetail>> GetTestRequestDetails(string sampleNo)
        {
            Logger.Logger.LogInstance.LogDebug("LISContext GetTestRequestDetails method started.");
            Logger.Logger.LogInstance.LogDebug("LISContext GetTestRequestDetails method started for SampleNo: '{0}'", sampleNo);
            string apiName = $"Lis/{sampleNo}";
            var response = await api.Get($"api/{apiName}", null, null);
            var jsonModel = JsonConvert.SerializeObject(response.Result);
            Logger.Logger.LogInstance.LogDebug("LISContext GetTestRequestDetails get data: '{0}'", jsonModel);
            IEnumerable<TestRequestDetail> items = null;
            if (jsonModel.Length > 0)
            {
                items = JsonConvert.DeserializeObject<IEnumerable<TestRequestDetail>>(jsonModel);
            }
            Logger.Logger.LogInstance.LogDebug("LISContext GetTestRequestDetails method completed.");
            return items;
        }

        public async Task SaveTestResult(Result result)
        {
            Logger.Logger.LogInstance.LogDebug("LISContext SaveTestResult method started.");
            var jsonModel = JsonConvert.SerializeObject(result);
            Logger.Logger.LogInstance.LogDebug("LISContext SaveTestResult method posted data:" + jsonModel);
            string apiName = "Lis";
            await api.Post($"api/{apiName}", result, null);
            Logger.Logger.LogInstance.LogDebug("LISContext SaveTestResult method completed.");
        }

        public async Task<bool> PingAPI()
        {
            Logger.Logger.LogInstance.LogDebug("LISContext PingAPI method started.");
            string apiName = $"Lis";
            var response = await api.Get($"api/{apiName}", null, null);
            var jsonModel = JsonConvert.SerializeObject(response.Result);
            bool isValid = false;
            if (jsonModel.Length > 0)
            {
                isValid = JsonConvert.DeserializeObject<bool>(jsonModel);
            }
            Logger.Logger.LogInstance.LogDebug("LISContext PingAPI method completed.");
            return isValid;
        }

        public async Task SendHeartBeat(bool IsActive)
        {
            Logger.Logger.LogInstance.LogDebug("LISContext SendHeartBeat method started.");
            string apiName = $"heartbeat";
            var bitStatus = new HeartBeatStatus()
            {
                IsAlive = IsActive
            };
            await api.Post($"api/{apiName}", bitStatus, null);
            Logger.Logger.LogInstance.LogDebug("LISContext SendHeartBeat method completed.");
        }

        public async Task<bool> AcknowledgeSample(long SampleId)
        {
            Logger.Logger.LogInstance.LogDebug("LISContext AcknowledgeSample method started for SampleNo:" + SampleId);
            string apiName = $"Lis/{SampleId}";
            var response = await api.Put($"api/{apiName}", null, null);

            bool isValid = (bool)response.Result;

            Logger.Logger.LogInstance.LogDebug("LISContext AcknowledgeSample method completed.");
            return isValid;
        }
    }

}
