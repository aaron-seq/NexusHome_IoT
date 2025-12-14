using Microsoft.AspNetCore.SignalR;

namespace NexusHome.IoT.API.Hubs
{
    public class SystemNotificationHub : Hub
    {
        public async Task SendNotification(string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", message);
        }
    }
}
