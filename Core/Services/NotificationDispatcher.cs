using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.API.Hubs;

namespace NexusHome.IoT.Core.Services;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly ILogger<NotificationDispatcher> _logger;
    private readonly IHubContext<SystemNotificationHub> _hubContext;

    public NotificationDispatcher(
        ILogger<NotificationDispatcher> logger,
        IHubContext<SystemNotificationHub> hubContext)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public async Task SendNotificationAsync(string userId, string title, string message, string priority = "normal")
    {
        _logger.LogInformation("Sending {Priority} notification to user {UserId}: {Title}", priority, userId, title);
        
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new 
            {
                Type = "Notification",
                Title = title, 
                Message = message, 
                Priority = priority,
                Timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending signalr notification");
        }
    }

    public async Task SendAlertAsync(string userId, string alertType, string message)
    {
        _logger.LogWarning("Sending alert {AlertType} to user {UserId}", alertType, userId);
        
        try
        {
             await _hubContext.Clients.All.SendAsync("ReceiveAlert", new 
            { 
                Type = "Alert",
                AlertType = alertType,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error sending signalr alert");
        }
    }
}
