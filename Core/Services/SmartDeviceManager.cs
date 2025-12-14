using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NexusHome.IoT.Core.Services
{
    /// <summary>
    /// Manages smart device lifecycle, registration, telemetry processing, and state management
    /// </summary>
    public class SmartDeviceManager : ISmartDeviceManager
    {
        private readonly SmartHomeDbContext _context;
        private readonly ILogger<SmartDeviceManager> _logger;

        public SmartDeviceManager(SmartHomeDbContext context, ILogger<SmartDeviceManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessTelemetryDataAsync(DeviceTelemetryRequest request)
        {
            try
            {
                _logger.LogInformation("Processing telemetry for device {DeviceId}", request.DeviceId);

                // Validate device exists
                var device = await _context.Devices.FindAsync(request.DeviceId);
                if (device == null)
                {
                    _logger.LogWarning("Device {DeviceId} not found in database", request.DeviceId);
                    return;
                }

                // Store telemetry data (you can add DeviceTelemetry entity later)
                _logger.LogDebug("Telemetry data: {@SensorData}", request.SensorData);

                // Update device last seen timestamp
                device.LastSeen = DateTime.UtcNow;
                device.IsOnline = true;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully processed telemetry for device {DeviceId}", request.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry for device {DeviceId}", request.DeviceId);
                throw;
            }
        }
    }
}
