using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Configuration;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NexusHome.IoT.Infrastructure.Services;

/// <summary>
/// Enhanced MQTT client service providing reliable IoT device communication
/// Features connection management, message persistence, automatic reconnection, and structured topic handling
/// </summary>
public class EnhancedMqttClientService : IMqttClientService, IDisposable
{
    private readonly ILogger<EnhancedMqttClientService> _logger;
    private readonly MqttBrokerSettings _mqttConfiguration;
    private readonly IManagedMqttClient _managedMqttClient;
    private readonly ConcurrentDictionary<string, Func<string, string, Task>> _subscriptionHandlers;
    private readonly SemaphoreSlim _connectionSemaphore;
    private bool _isDisposed;

    public EnhancedMqttClientService(
        ILogger<EnhancedMqttClientService> logger,
        IOptions<MqttBrokerSettings> mqttConfiguration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mqttConfiguration = mqttConfiguration.Value ?? throw new ArgumentNullException(nameof(mqttConfiguration));
        _subscriptionHandlers = new ConcurrentDictionary<string, Func<string, string, Task>>();
        _connectionSemaphore = new SemaphoreSlim(1, 1);
        
        var mqttFactory = new MqttFactory();
        _managedMqttClient = mqttFactory.CreateManagedMqttClient();
        
        ConfigureEventHandlers();
    }

    /// <summary>
    /// Current connection status to MQTT broker
    /// </summary>
    public bool IsConnected => _managedMqttClient?.IsConnected ?? false;

    /// <summary>
    /// Unique client identifier for MQTT connection tracking
    /// </summary>
    public string ClientIdentifier => _mqttConfiguration.ClientId ?? "NexusHome-IoT-Platform";

    /// <summary>
    /// Event triggered when MQTT broker connection is established
    /// </summary>
    public event Func<Task>? ConnectionEstablished;

    /// <summary>
    /// Event triggered when MQTT broker connection is lost
    /// </summary>
    public event Func<Task>? ConnectionLost;

    /// <summary>
    /// Establishes managed connection to MQTT broker with automatic reconnection
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for connection timeout</param>
    /// <returns>True if connection established successfully</returns>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            if (IsConnected)
            {
                _logger.LogInformation("MQTT client already connected to broker at {BrokerHost}:{BrokerPort}", 
                    _mqttConfiguration.Host, _mqttConfiguration.Port);
                return true;
            }

            var clientOptions = BuildClientOptions();
            var managedOptions = BuildManagedClientOptions(clientOptions);

            _logger.LogInformation("Attempting to connect to MQTT broker at {BrokerHost}:{BrokerPort} with client ID {ClientId}",
                _mqttConfiguration.Host, _mqttConfiguration.Port, ClientIdentifier);

            await _managedMqttClient.StartAsync(managedOptions);

            // Wait for connection with timeout
            var connectionTimeout = TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;
            
            while (!IsConnected && DateTime.UtcNow - startTime < connectionTimeout)
            {
                await Task.Delay(100, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("MQTT connection attempt was cancelled");
                }
            }

            if (IsConnected)
            {
                _logger.LogInformation("Successfully connected to MQTT broker with client ID {ClientId}", ClientIdentifier);
                return true;
            }
            else
            {
                _logger.LogError("Failed to establish MQTT connection within timeout period of {TimeoutSeconds} seconds", 
                    connectionTimeout.TotalSeconds);
                return false;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while connecting to MQTT broker at {BrokerHost}:{BrokerPort}", 
                _mqttConfiguration.Host, _mqttConfiguration.Port);
            return false;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Gracefully disconnects from MQTT broker, ensuring message delivery
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for disconnect timeout</param>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            if (!IsConnected)
            {
                _logger.LogInformation("MQTT client is already disconnected");
                return;
            }

            _logger.LogInformation("Disconnecting from MQTT broker with client ID {ClientId}", ClientIdentifier);
            
            await _managedMqttClient.StopAsync();
            _subscriptionHandlers.Clear();
            
            _logger.LogInformation("Successfully disconnected from MQTT broker");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while disconnecting from MQTT broker");
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Publishes message to MQTT topic with configurable quality of service
    /// </summary>
    /// <param name="topicName">Target MQTT topic path</param>
    /// <param name="messagePayload">Message content (JSON or plain text)</param>
    /// <param name="qualityOfService">QoS level (0=At most once, 1=At least once, 2=Exactly once)</param>
    /// <param name="retainMessage">Whether broker should retain message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task PublishAsync(string topicName, string messagePayload, int qualityOfService = 1, 
                                  bool retainMessage = false, CancellationToken cancellationToken = default)
    {
        ValidateConnectionState();
        ValidateTopicName(topicName);
        
        if (string.IsNullOrEmpty(messagePayload))
        {
            throw new ArgumentException("Message payload cannot be null or empty", nameof(messagePayload));
        }

        try
        {
            var mqttMessage = new ManagedMqttApplicationMessageBuilder()
                .WithTopic(topicName)
                .WithPayload(Encoding.UTF8.GetBytes(messagePayload))
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qualityOfService)
                .WithRetainFlag(retainMessage)
                .Build();

            await _managedMqttClient.EnqueueAsync(mqttMessage);
            
            _logger.LogDebug("Published message to topic {TopicName} with QoS {QualityOfService}, payload length: {PayloadLength} bytes",
                topicName, qualityOfService, messagePayload.Length);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to publish message to topic {TopicName}", topicName);
            throw;
        }
    }

    /// <summary>
    /// Subscribes to MQTT topic pattern with message handler callback
    /// </summary>
    /// <param name="topicPattern">MQTT topic pattern with wildcard support</param>
    /// <param name="messageHandler">Async callback for processing received messages</param>
    /// <param name="qualityOfService">Subscription QoS level</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SubscribeAsync(string topicPattern, Func<string, string, Task> messageHandler, 
                                    int qualityOfService = 1, CancellationToken cancellationToken = default)
    {
        ValidateConnectionState();
        ValidateTopicName(topicPattern);
        
        if (messageHandler == null)
        {
            throw new ArgumentNullException(nameof(messageHandler));
        }

        try
        {
            // Store message handler for this topic pattern
            _subscriptionHandlers.AddOrUpdate(topicPattern, messageHandler, (key, existingHandler) => messageHandler);

            var topicFilter = new MqttTopicFilterBuilder()
                .WithTopic(topicPattern)
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qualityOfService)
                .Build();

            await _managedMqttClient.SubscribeAsync(topicFilter);
            
            _logger.LogInformation("Successfully subscribed to topic pattern {TopicPattern} with QoS {QualityOfService}",
                topicPattern, qualityOfService);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to subscribe to topic pattern {TopicPattern}", topicPattern);
            throw;
        }
    }

    /// <summary>
    /// Unsubscribes from previously subscribed MQTT topic pattern
    /// </summary>
    /// <param name="topicPattern">Topic pattern to unsubscribe from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UnsubscribeAsync(string topicPattern, CancellationToken cancellationToken = default)
    {
        ValidateTopicName(topicPattern);
        
        try
        {
            await _managedMqttClient.UnsubscribeAsync(topicPattern);
            _subscriptionHandlers.TryRemove(topicPattern, out _);
            
            _logger.LogInformation("Successfully unsubscribed from topic pattern {TopicPattern}", topicPattern);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to unsubscribe from topic pattern {TopicPattern}", topicPattern);
            throw;
        }
    }

    /// <summary>
    /// Publishes structured device telemetry data
    /// </summary>
    /// <param name="deviceIdentifier">Unique device identifier</param>
    /// <param name="telemetryData">Structured telemetry data object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task PublishDeviceTelemetryAsync(string deviceIdentifier, object telemetryData, 
                                                 CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(deviceIdentifier))
        {
            throw new ArgumentException("Device identifier cannot be null or empty", nameof(deviceIdentifier));
        }

        if (telemetryData == null)
        {
            throw new ArgumentNullException(nameof(telemetryData));
        }

        var topicName = $"nexushome/devices/{deviceIdentifier}/telemetry";
        var jsonPayload = JsonSerializer.Serialize(new
        {
            deviceId = deviceIdentifier,
            timestamp = DateTime.UtcNow,
            data = telemetryData
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await PublishAsync(topicName, jsonPayload, qualityOfService: 1, retainMessage: false, cancellationToken);
        
        _logger.LogDebug("Published telemetry data for device {DeviceId} to topic {TopicName}", 
            deviceIdentifier, topicName);
    }

    /// <summary>
    /// Sends command to specific device through structured command topic
    /// </summary>
    /// <param name="deviceIdentifier">Target device identifier</param>
    /// <param name="commandType">Command type identifier</param>
    /// <param name="commandParameters">Command-specific parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SendDeviceCommandAsync(string deviceIdentifier, string commandType, object commandParameters,
                                           CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(deviceIdentifier))
        {
            throw new ArgumentException("Device identifier cannot be null or empty", nameof(deviceIdentifier));
        }

        if (string.IsNullOrEmpty(commandType))
        {
            throw new ArgumentException("Command type cannot be null or empty", nameof(commandType));
        }

        var topicName = $"nexushome/devices/{deviceIdentifier}/commands";
        var commandPayload = JsonSerializer.Serialize(new
        {
            commandId = Guid.NewGuid().ToString(),
            deviceId = deviceIdentifier,
            commandType = commandType,
            parameters = commandParameters,
            timestamp = DateTime.UtcNow,
            expiresAt = DateTime.UtcNow.AddMinutes(5)
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await PublishAsync(topicName, commandPayload, qualityOfService: 1, retainMessage: false, cancellationToken);
        
        _logger.LogInformation("Sent command {CommandType} to device {DeviceId}", commandType, deviceIdentifier);
    }

    /// <summary>
    /// Publishes system-wide alert notification
    /// </summary>
    /// <param name="alertType">Classification of alert</param>
    /// <param name="alertMessage">Human-readable alert message</param>
    /// <param name="alertSeverity">Severity level</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task PublishSystemAlertAsync(string alertType, string alertMessage, string alertSeverity,
                                            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(alertType))
        {
            throw new ArgumentException("Alert type cannot be null or empty", nameof(alertType));
        }

        if (string.IsNullOrEmpty(alertMessage))
        {
            throw new ArgumentException("Alert message cannot be null or empty", nameof(alertMessage));
        }

        var topicName = "nexushome/system/alerts";
        var alertPayload = JsonSerializer.Serialize(new
        {
            alertId = Guid.NewGuid().ToString(),
            alertType = alertType,
            message = alertMessage,
            severity = alertSeverity ?? "info",
            timestamp = DateTime.UtcNow,
            source = "NexusHome-IoT-Platform"
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await PublishAsync(topicName, alertPayload, qualityOfService: 1, retainMessage: true, cancellationToken);
        
        _logger.LogInformation("Published system alert: {AlertType} - {AlertMessage}", alertType, alertMessage);
    }

    private MqttClientOptions BuildClientOptions()
    {
        var clientOptionsBuilder = new MqttClientOptionsBuilder()
            .WithClientId(ClientIdentifier)
            .WithTcpServer(_mqttConfiguration.Host, _mqttConfiguration.Port)
            .WithCleanSession(_mqttConfiguration.CleanSession)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(_mqttConfiguration.KeepAlivePeriod))
            .WithTimeout(TimeSpan.FromSeconds(30));

        // Add authentication if configured
        if (!string.IsNullOrEmpty(_mqttConfiguration.Username))
        {
            clientOptionsBuilder.WithCredentials(_mqttConfiguration.Username, _mqttConfiguration.Password);
        }

        return clientOptionsBuilder.Build();
    }

    private ManagedMqttClientOptions BuildManagedClientOptions(MqttClientOptions clientOptions)
    {
        return new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(clientOptions)
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(10))
            .WithMaxPendingMessages(10000)
            .WithPendingMessagesOverflowStrategy(MQTTnet.Server.MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
            .Build();
    }

    private void ConfigureEventHandlers()
    {
        _managedMqttClient.ConnectedAsync += OnConnectedAsync;
        _managedMqttClient.DisconnectedAsync += OnDisconnectedAsync;
        _managedMqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _managedMqttClient.ConnectingFailedAsync += OnConnectingFailedAsync;
    }

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs eventArgs)
    {
        _logger.LogInformation("MQTT client successfully connected to broker. Result code: {ResultCode}", 
            eventArgs.ConnectResult?.ResultCode);
        
        if (ConnectionEstablished != null)
        {
            await ConnectionEstablished.Invoke();
        }
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
    {
        _logger.LogWarning("MQTT client disconnected from broker. Reason: {Reason}, Exception: {Exception}", 
            eventArgs.Reason, eventArgs.Exception?.Message);
        
        if (ConnectionLost != null)
        {
            await ConnectionLost.Invoke();
        }
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        try
        {
            var topic = eventArgs.ApplicationMessage.Topic;
            var payloadString = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.PayloadSegment);
            
            _logger.LogDebug("Received MQTT message on topic {Topic}, payload length: {PayloadLength} bytes", 
                topic, payloadString.Length);

            // Find matching subscription handlers
            foreach (var subscription in _subscriptionHandlers)
            {
                if (DoesTopicMatchPattern(topic, subscription.Key))
                {
                    try
                    {
                        await subscription.Value(topic, payloadString);
                    }
                    catch (Exception handlerException)
                    {
                        _logger.LogError(handlerException, "Error in message handler for topic pattern {TopicPattern}", 
                            subscription.Key);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error processing received MQTT message");
        }
    }

    private Task OnConnectingFailedAsync(ConnectingFailedEventArgs eventArgs)
    {
        _logger.LogError(eventArgs.Exception, "MQTT client failed to connect to broker");
        return Task.CompletedTask;
    }

    private static bool DoesTopicMatchPattern(string topic, string pattern)
    {
        // Simple topic pattern matching implementation
        // Supports + for single level wildcard and # for multi-level wildcard
        if (pattern.Contains('#'))
        {
            var prefixPattern = pattern.Substring(0, pattern.IndexOf('#'));
            return topic.StartsWith(prefixPattern);
        }
        
        if (pattern.Contains('+'))
        {
            var patternParts = pattern.Split('/');
            var topicParts = topic.Split('/');
            
            if (patternParts.Length != topicParts.Length)
            {
                return false;
            }
            
            for (int i = 0; i < patternParts.Length; i++)
            {
                if (patternParts[i] != "+" && patternParts[i] != topicParts[i])
                {
                    return false;
                }
            }
            
            return true;
        }
        
        return topic == pattern;
    }

    private void ValidateConnectionState()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("MQTT client is not connected to broker. Call ConnectAsync() first.");
        }
    }

    private static void ValidateTopicName(string topicName)
    {
        if (string.IsNullOrEmpty(topicName))
        {
            throw new ArgumentException("Topic name cannot be null or empty", nameof(topicName));
        }

        if (topicName.Contains("\0"))
        {
            throw new ArgumentException("Topic name cannot contain null characters", nameof(topicName));
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        try
        {
            _managedMqttClient?.StopAsync().GetAwaiter().GetResult();
            _managedMqttClient?.Dispose();
            _connectionSemaphore?.Dispose();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred during MQTT client disposal");
        }
        finally
        {
            _isDisposed = true;
        }
    }
}
