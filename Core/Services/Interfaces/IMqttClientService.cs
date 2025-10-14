using System.Threading;
using System.Threading.Tasks;

namespace NexusHome.IoT.Core.Services.Interfaces;

/// <summary>
/// Provides comprehensive MQTT messaging capabilities for IoT device communication
/// Supports publish/subscribe patterns, connection management, and message persistence
/// </summary>
public interface IMqttClientService
{
    /// <summary>
    /// Establishes connection to the MQTT broker with authentication and keep-alive settings
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for connection timeout</param>
    /// <returns>True if connection established successfully, false otherwise</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gracefully disconnects from MQTT broker, ensuring all pending messages are delivered
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for disconnect timeout</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message to the specified MQTT topic with quality of service options
    /// </summary>
    /// <param name="topicName">MQTT topic path for message routing</param>
    /// <param name="messagePayload">JSON or plain text message content</param>
    /// <param name="qualityOfService">Message delivery guarantee level (0=At most once, 1=At least once, 2=Exactly once)</param>
    /// <param name="retainMessage">Whether broker should retain message for new subscribers</param>
    /// <param name="cancellationToken">Cancellation token for publish timeout</param>
    Task PublishAsync(string topicName, string messagePayload, int qualityOfService = 1, 
                     bool retainMessage = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to MQTT topic with callback handler for incoming messages
    /// </summary>
    /// <param name="topicPattern">MQTT topic pattern supporting wildcards (+ for single level, # for multi-level)</param>
    /// <param name="messageHandler">Async callback function to process received messages</param>
    /// <param name="qualityOfService">Subscription quality of service level</param>
    /// <param name="cancellationToken">Cancellation token for subscription timeout</param>
    Task SubscribeAsync(string topicPattern, Func<string, string, Task> messageHandler, 
                       int qualityOfService = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from previously subscribed MQTT topic pattern
    /// </summary>
    /// <param name="topicPattern">MQTT topic pattern to unsubscribe from</param>
    /// <param name="cancellationToken">Cancellation token for unsubscribe timeout</param>
    Task UnsubscribeAsync(string topicPattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks current connection status to MQTT broker
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the unique client identifier used for MQTT connection
    /// </summary>
    string ClientIdentifier { get; }

    /// <summary>
    /// Event triggered when connection to MQTT broker is established
    /// </summary>
    event Func<Task> ConnectionEstablished;

    /// <summary>
    /// Event triggered when connection to MQTT broker is lost
    /// </summary>
    event Func<Task> ConnectionLost;

    /// <summary>
    /// Publishes device telemetry data to structured topic hierarchy
    /// </summary>
    /// <param name="deviceIdentifier">Unique device identifier</param>
    /// <param name="telemetryData">Structured telemetry data object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishDeviceTelemetryAsync(string deviceIdentifier, object telemetryData, 
                                   CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends command to specific device through MQTT command topic
    /// </summary>
    /// <param name="deviceIdentifier">Target device unique identifier</param>
    /// <param name="commandType">Command type (power_on, power_off, set_temperature, etc.)</param>
    /// <param name="commandParameters">Command-specific parameters as object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendDeviceCommandAsync(string deviceIdentifier, string commandType, object commandParameters,
                               CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes system-wide alert notification to all subscribed clients
    /// </summary>
    /// <param name="alertType">Classification of alert (security, maintenance, energy, etc.)</param>
    /// <param name="alertMessage">Human-readable alert description</param>
    /// <param name="alertSeverity">Severity level (info, warning, error, critical)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishSystemAlertAsync(string alertType, string alertMessage, string alertSeverity,
                                CancellationToken cancellationToken = default);
}