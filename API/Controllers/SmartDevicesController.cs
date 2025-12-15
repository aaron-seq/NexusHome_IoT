using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusHome.IoT.Core.Domain;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Core.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace NexusHome.IoT.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SmartDevicesController : ControllerBase
{
    private readonly ISmartDeviceManager _deviceManager;
    private readonly ILogger<SmartDevicesController> _logger;

    public SmartDevicesController(
        ISmartDeviceManager deviceManager,
        ILogger<SmartDevicesController> logger)
    {
        _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SmartHomeDevice>>> GetAllDevicesAsync()
    {
        var devices = await _deviceManager.GetAllDevicesAsync();
        return Ok(devices);
    }

    [HttpGet("{deviceId}")]
    public async Task<ActionResult<SmartHomeDevice>> GetDeviceByIdAsync(string deviceId)
    {
        var device = await _deviceManager.GetDeviceByIdAsync(deviceId);
        if (device == null) return NotFound();
        return Ok(device);
    }

    [HttpPost]
    public async Task<ActionResult<SmartHomeDevice>> RegisterDeviceAsync([FromBody] SmartHomeDevice device)
    {
        var created = await _deviceManager.AddDeviceAsync(device);
        return CreatedAtAction(nameof(GetDeviceByIdAsync), new { deviceId = created.UniqueDeviceIdentifier }, created);
    }

    [HttpPut("{deviceId}")]
    public async Task<ActionResult<SmartHomeDevice>> UpdateDeviceAsync(string deviceId, [FromBody] SmartHomeDevice device)
    {
        if (device.UniqueDeviceIdentifier != deviceId) return BadRequest("ID mismatch");
        try
        {
            // Note: Service relies on ID (Int). We need to map or ensure device has ID.
            // If device only has String ID, we first Get it to find Int ID.
            var existing = await _deviceManager.GetDeviceByIdAsync(deviceId);
            if (existing == null) return NotFound();
            
            device.Id = existing.Id; // Ensure PK is set
            var updated = await _deviceManager.UpdateDeviceAsync(device);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{deviceId}")]
    public async Task<IActionResult> DeleteDeviceAsync(string deviceId)
    {
        var existing = await _deviceManager.GetDeviceByIdAsync(deviceId);
        if (existing == null) return NotFound();
        
        await _deviceManager.DeleteDeviceAsync(existing.Id);
        return NoContent();
    }
    
    [HttpPost("{deviceId}/toggle")]
    public async Task<IActionResult> ToggleDeviceAsync(string deviceId)
    {
        var existing = await _deviceManager.GetDeviceByIdAsync(deviceId);
        if (existing == null) return NotFound();

        await _deviceManager.ToggleDeviceAsync(existing.Id);
        return Accepted();
    }
}
