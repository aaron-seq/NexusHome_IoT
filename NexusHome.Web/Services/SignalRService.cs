using Microsoft.AspNetCore.SignalR.Client;

namespace NexusHome.Web.Services;

/// <summary>
/// SignalR service for real-time device status and energy updates
/// </summary>
public class SignalRService : IAsyncDisposable
{
    private readonly HubConnection _deviceStatusHub;
    private readonly HubConnection _energyHub;
    private readonly ILogger<SignalRService> _logger;

    public event Action<string, string>? OnDeviceStatusChanged;
    public event Action<object>? OnEnergyDataReceived;
    public event Action<string>? OnNotificationReceived;

    public bool IsConnected => _deviceStatusHub.State == HubConnectionState.Connected;

    public SignalRService(IConfiguration configuration, ILogger<SignalRService> logger)
    {
        _logger = logger;
        var baseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7000";

        _deviceStatusHub = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/deviceStatus")
            .WithAutomaticReconnect()
            .Build();

        _energyHub = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/energy")
            .WithAutomaticReconnect()
            .Build();

        ConfigureEventHandlers();
    }

    private void ConfigureEventHandlers()
    {
        _deviceStatusHub.On<string, string>("ReceiveStatusUpdate", (deviceId, status) =>
        {
            _logger.LogInformation("Device {DeviceId} status changed to {Status}", deviceId, status);
            OnDeviceStatusChanged?.Invoke(deviceId, status);
        });

        _energyHub.On<object>("ReceiveEnergyData", (data) =>
        {
            _logger.LogDebug("Received energy data update");
            OnEnergyDataReceived?.Invoke(data);
        });

        _deviceStatusHub.Reconnecting += (error) =>
        {
            _logger.LogWarning("SignalR reconnecting: {Error}", error?.Message);
            return Task.CompletedTask;
        };

        _deviceStatusHub.Reconnected += (connectionId) =>
        {
            _logger.LogInformation("SignalR reconnected with ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };
    }

    public async Task ConnectAsync()
    {
        try
        {
            if (_deviceStatusHub.State == HubConnectionState.Disconnected)
            {
                await _deviceStatusHub.StartAsync();
                _logger.LogInformation("Connected to device status hub");
            }

            if (_energyHub.State == HubConnectionState.Disconnected)
            {
                await _energyHub.StartAsync();
                _logger.LogInformation("Connected to energy hub");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hubs");
            // Don't throw - allow app to work without real-time updates
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_deviceStatusHub.State != HubConnectionState.Disconnected)
            {
                await _deviceStatusHub.StopAsync();
            }

            if (_energyHub.State != HubConnectionState.Disconnected)
            {
                await _energyHub.StopAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from SignalR hubs");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        await _deviceStatusHub.DisposeAsync();
        await _energyHub.DisposeAsync();
    }
}
