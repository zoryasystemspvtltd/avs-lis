using LIS.Com.Businesslogic;
using LIS.Logger;
using LisTCPIPConsole.Properties;
using Microsoft.AspNet.SignalR.Client;

namespace LisTCPIPConsole
{
    public class HeartBeatProxy
    {
        public  void WatchHeartBeat()
        {
            var hubUrl = $"{Settings.Default.SERVER_URL}/lis";
            var hubConnection = new HubConnection(hubUrl);
            IHubProxy lisHubProxy = hubConnection.CreateHubProxy("LisHub");
            hubConnection.Start().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Logger.LogInstance.LogDebug($"Failed to connect LisHub");
                }
                else
                {
                    Logger.LogInstance.LogDebug($"LisHub connected");
                }
            });
            lisHubProxy.On<bool>("CallHeartBeat", async hearbeat => await LisContext.LisDOM.SendHeartBeat(hearbeat));

        }
    }
}
