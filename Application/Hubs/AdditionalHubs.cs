using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace NexusHome.IoT.Application.Hubs
{
    [Authorize(Policy = "UserAccess")]
    public class EnergyMonitoringHub : Hub { }

    [Authorize(Policy = "UserAccess")]
    public class SystemNotificationHub : Hub { }

    [Authorize(Policy = "UserAccess")]
    public class MaintenanceAlertHub : Hub { }
}
