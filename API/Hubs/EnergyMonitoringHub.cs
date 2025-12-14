using Microsoft.AspNetCore.SignalR;

namespace NexusHome.IoT.API.Hubs
{
    public class EnergyMonitoringHub : Hub
    {
        public async Task SendEnergyData(object data)
        {
            await Clients.All.SendAsync("ReceiveEnergyData", data);
        }
    }
}
