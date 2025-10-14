using System.Threading;
using System.Threading.Tasks;

namespace NexusHome.IoT.Core.Services.Interfaces;

/// <summary>
/// Provides multi-channel notification dispatch capabilities for system alerts and user communications
/// Supports email, SMS, push notifications, and real-time web notifications
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Sends email notification to specified recipient with template rendering support
    /// </summary>
    /// <param name="recipientEmailAddress">Destination email address</param>
    /// <param name="emailSubjectLine">Email subject line</param>
    /// <param name="emailBodyContent">HTML or plain text email body</param>
    /// <param name="notificationPriority">Priority level affecting delivery urgency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email queued successfully, false otherwise</returns>
    Task<bool> SendEmailNotificationAsync(string recipientEmailAddress, string emailSubjectLine, 
                                        string emailBodyContent, NotificationPriority notificationPriority = NotificationPriority.Normal,
                                        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends SMS text message notification to specified phone number
    /// </summary>
    /// <param name="recipientPhoneNumber">Destination phone number in E.164 format</param>
    /// <param name="messageContent">SMS message text (max 160 characters recommended)</param>
    /// <param name="notificationPriority">Priority level affecting delivery urgency</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if SMS queued successfully, false otherwise</returns>
    Task<bool> SendSmsNotificationAsync(string recipientPhoneNumber, string messageContent,
                                       NotificationPriority notificationPriority = NotificationPriority.Normal,
                                       CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends push notification to mobile app or web browser
    /// </summary>
    /// <param name="userIdentifier">Target user unique identifier</param>
    /// <param name="notificationTitle">Push notification title</param>
    /// <param name="notificationBody">Push notification body text</param>
    /// <param name="additionalData">Optional key-value pairs for app-specific data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if push notification queued successfully, false otherwise</returns>
    Task<bool> SendPushNotificationAsync(string userIdentifier, string notificationTitle, string notificationBody,
                                        Dictionary<string, string>? additionalData = null,
                                        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts real-time notification to all connected web clients via SignalR
    /// </summary>
    /// <param name="notificationMessage">Broadcast message content</param>
    /// <param name="notificationType">Classification of notification (alert, info, success, warning, error)</param>
    /// <param name="targetUserGroups">Optional user groups to target (null = broadcast to all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BroadcastRealTimeNotificationAsync(string notificationMessage, string notificationType,
                                          IEnumerable<string>? targetUserGroups = null,
                                          CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends device-specific alert notification using user preferences for delivery channels
    /// </summary>
    /// <param name="deviceIdentifier">Device that generated the alert</param>
    /// <param name="alertType">Classification of device alert</param>
    /// <param name="alertMessage">Human-readable alert description</param>
    /// <param name="alertSeverityLevel">Severity level determining notification urgency</param>
    /// <param name="affectedUserIds">Users to notify about this device alert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendDeviceAlertNotificationAsync(string deviceIdentifier, string alertType, string alertMessage,
                                        AlertSeverity alertSeverityLevel, IEnumerable<int> affectedUserIds,
                                        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends energy consumption threshold alert with consumption data
    /// </summary>
    /// <param name="currentPowerUsageWatts">Current power consumption in watts</param>
    /// <param name="thresholdWatts">Configured threshold that was exceeded</param>
    /// <param name="estimatedMonthlyCost">Projected monthly cost based on current usage</param>
    /// <param name="affectedUserIds">Users to notify about energy usage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEnergyThresholdAlertAsync(decimal currentPowerUsageWatts, decimal thresholdWatts,
                                     decimal estimatedMonthlyCost, IEnumerable<int> affectedUserIds,
                                     CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends maintenance reminder notification for scheduled device maintenance
    /// </summary>
    /// <param name="deviceIdentifier">Device requiring maintenance</param>
    /// <param name="maintenanceType">Type of maintenance required</param>
    /// <param name="scheduledMaintenanceDate">When maintenance is scheduled</param>
    /// <param name="estimatedDurationHours">Expected maintenance duration</param>
    /// <param name="technicianContactInfo">Contact information for assigned technician</param>
    /// <param name="affectedUserIds">Users to notify about maintenance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendMaintenanceReminderAsync(string deviceIdentifier, string maintenanceType,
                                     DateTime scheduledMaintenanceDate, int estimatedDurationHours,
                                     string? technicianContactInfo, IEnumerable<int> affectedUserIds,
                                     CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification delivery statistics for monitoring and analytics
    /// </summary>
    /// <param name="startDateRange">Statistics start date</param>
    /// <param name="endDateRange">Statistics end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification delivery statistics grouped by channel and status</returns>
    Task<NotificationStatistics> GetNotificationStatisticsAsync(DateTime startDateRange, DateTime endDateRange,
                                                               CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents notification priority levels affecting delivery urgency and user interruption
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority - delivered when convenient, no user interruption
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority - standard delivery timing
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority - faster delivery, may interrupt user activities
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - immediate delivery, override user do-not-disturb settings
    /// </summary>
    Critical = 3
}

/// <summary>
/// Represents alert severity levels for device and system notifications
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Informational message requiring no action
    /// </summary>
    Information = 0,

    /// <summary>
    /// Warning about potential issues requiring attention
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error condition requiring corrective action
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical system failure requiring immediate intervention
    /// </summary>
    Critical = 3
}

/// <summary>
/// Contains notification delivery statistics for monitoring and performance analysis
/// </summary>
public class NotificationStatistics
{
    /// <summary>
    /// Total number of notifications sent across all channels
    /// </summary>
    public int TotalNotificationsSent { get; set; }

    /// <summary>
    /// Number of successfully delivered notifications
    /// </summary>
    public int SuccessfulDeliveries { get; set; }

    /// <summary>
    /// Number of failed notification deliveries
    /// </summary>
    public int FailedDeliveries { get; set; }

    /// <summary>
    /// Average delivery time in milliseconds
    /// </summary>
    public double AverageDeliveryTimeMilliseconds { get; set; }

    /// <summary>
    /// Delivery statistics broken down by notification channel
    /// </summary>
    public Dictionary<string, ChannelStatistics> ChannelStatistics { get; set; } = new();

    /// <summary>
    /// Time period these statistics cover
    /// </summary>
    public TimeSpan StatisticsPeriod { get; set; }
}

/// <summary>
/// Contains delivery statistics for a specific notification channel
/// </summary>
public class ChannelStatistics
{
    /// <summary>
    /// Channel name (Email, SMS, Push, RealTime)
    /// </summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// Number of notifications sent through this channel
    /// </summary>
    public int NotificationCount { get; set; }

    /// <summary>
    /// Number of successful deliveries for this channel
    /// </summary>
    public int SuccessfulCount { get; set; }

    /// <summary>
    /// Number of failed deliveries for this channel
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Average delivery time for this channel in milliseconds
    /// </summary>
    public double AverageDeliveryTimeMilliseconds { get; set; }
}
