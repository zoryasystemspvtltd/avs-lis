using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Lis.Api.App_Start
{

    public interface ILisClient
    {
        Task CallHeartBeat(bool IsAlive);
    }

    [HubName("LisHub")]
    public class LisHub : Hub<ILisClient>
    {
        public async Task CallHeartBeat(bool IsAlive)
        {
            await Clients.All.CallHeartBeat(IsAlive);
        }
    }
}