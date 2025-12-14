using Microsoft.AspNetCore.SignalR;

namespace NexusHome.IoT.API.Hubs
{
    public class SmartDeviceStatusHub : Hub
    {
        public async Task SendStatusUpdate(string deviceId, string status)
        {
            await Clients.All.SendAsync("ReceiveStatusUpdate", deviceId, status);
        }
    }
}
