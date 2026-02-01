using Microsoft.AspNetCore.SignalR;

namespace NexusHome.IoT.Application.Hubs;

public class SmartDeviceStatusHub : Hub
{
    private readonly ILogger<SmartDeviceStatusHub> _logger;

    public SmartDeviceStatusHub(ILogger<SmartDeviceStatusHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinDeviceGroup(string deviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        _logger.LogDebug("Client {ConnectionId} joined device group {DeviceId}", Context.ConnectionId, deviceId);
    }

    public async Task LeaveDeviceGroup(string deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        _logger.LogDebug("Client {ConnectionId} left device group {DeviceId}", Context.ConnectionId, deviceId);
    }

    public async Task JoinRoomGroup(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomName}");
        _logger.LogDebug("Client {ConnectionId} joined room group {RoomName}", Context.ConnectionId, roomName);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to device status hub", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from device status hub", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
