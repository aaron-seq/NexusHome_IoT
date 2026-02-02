using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services;

namespace NexusHome.IoT.Infrastructure.Services;

/// <summary>
/// Simulates the Bluetooth Low Energy (BLE) commissioning flow for Matter devices.
/// Since we lack physical BLE hardware, this services provides a 'virtual' scanner/commissioner.
/// </summary>
public class BleSimulationService
{
    private readonly ILogger<BleSimulationService> _logger;
    private readonly MatterDiscoveryService _discoveryService;

    public BleSimulationService(ILogger<BleSimulationService> logger, MatterDiscoveryService discoveryService)
    {
        _logger = logger;
        _discoveryService = discoveryService;
    }

    /// <summary>
    /// Simulates scanning for nearby Matter devices over BLE.
    /// </summary>
    public async Task<List<BleDeviceCandidate>> ScanForDevicesAsync()
    {
        _logger.LogInformation("Starting simulated BLE scan...");
        await Task.Delay(2000); // Simulate radio scan time

        // Return a mock uncommissioned device
        return new List<BleDeviceCandidate>
        {
            new BleDeviceCandidate 
            { 
                Discriminator = 1234,
                VendorId = 0xFFF1,
                ProductId = 0x8001,
                SignalStrength = -45,
                DeviceName = "Matter Light (Simulated)"
            }
        };
    }

    /// <summary>
    /// Simulates the PASE (Passcode Authenticated Session Establishment) flow.
    /// </summary>
    public async Task<bool> EstablishPaseSessionAsync(BleDeviceCandidate device, int pincode)
    {
        _logger.LogInformation("Attempting PASE session with {DeviceName} (Pin: {Pin})", device.DeviceName, pincode);
        await Task.Delay(1500); // Simulate handshake

        if (pincode == 20202021) // Standard Matter Test Pin
        {
            _logger.LogInformation("PASE Session Established!");
            return true;
        }
        else
        {
            _logger.LogWarning("PASE Failed: Invalid Pin");
            return false;
        }
    }

    /// <summary>
    /// Simulates sending Wi-Fi credentials to the device via BLE.
    /// After this, the device should join Wi-Fi and be discoverable via mDNS.
    /// </summary>
    public async Task CommissionDeviceAsync(BleDeviceCandidate device, string ssid, string password)
    {
        _logger.LogInformation("Sending Wi-Fi credentials ({Ssid}) to device...", ssid);
        await Task.Delay(3000); // Simulate network join

        _logger.LogInformation("Device successfully commissioned! It should now appear via mDNS.");
        
        // Logical handover: The simulated device "joins" the network.
        // In a real simulation, we might trigger a mock mDNS announcement here.
        // For now, we assume the user's manual 'Add Device' flow in Phase 4 handles the rest.
    }
}

public class BleDeviceCandidate
{
    public string DeviceName { get; set; } = "";
    public int Discriminator { get; set; }
    public int VendorId { get; set; }
    public int ProductId { get; set; }
    public int SignalStrength { get; set; }
}
