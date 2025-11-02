using Microsoft.AspNetCore.Mvc;
using NexusHome.IoT.Core.Services.Interfaces;

namespace NexusHome.IoT.API.Controllers
{
    [ApiController]
    [Route("api/devices")]
    public class DevicesController : ControllerBase
    {
        private readonly ISmartDeviceManager _deviceManager;
        public DevicesController(ISmartDeviceManager deviceManager) => _deviceManager = deviceManager;

        [HttpGet]
        public IActionResult GetDevices() => Ok(Array.Empty<object>());

        [HttpGet("{deviceId}")]
        public IActionResult GetDevice(string deviceId) => Ok(new { deviceId });

        [HttpPost("{deviceId}/toggle")]
        public IActionResult Toggle(string deviceId) => Accepted(new { deviceId, toggled = true });

        public record TelemetryDto(string DeviceId, Dictionary<string, object> SensorData, DateTime Timestamp);

        [HttpPost("telemetry")]
        public async Task<IActionResult> Telemetry([FromBody] TelemetryDto dto)
        {
            await _deviceManager.ProcessTelemetryDataAsync(new DeviceTelemetryRequest(dto.DeviceId, dto.SensorData, dto.Timestamp));
            return Accepted();
        }
    }
}
