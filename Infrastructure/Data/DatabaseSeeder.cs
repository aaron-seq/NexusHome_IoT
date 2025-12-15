using NexusHome.IoT.Core.Domain;
using System.Text.Json;

namespace NexusHome.IoT.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(SmartHomeDbContext context, IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            if (!context.SmartDevices.Any())
            {
                await SeedDemoDevicesAsync(context, logger);
            }

            if (!context.AutomationRules.Any())
            {
                await SeedDemoAutomationRulesAsync(context, logger);
            }

            if (!context.Users.Any())
            {
                await SeedDefaultUserAsync(context, logger);
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while seeding database");
            throw;
        }
    }

    private static async Task SeedDemoDevicesAsync(SmartHomeDbContext context, ILogger logger)
    {
        var demoDevices = new[]
        {
            new SmartHomeDevice
            {
                UniqueDeviceIdentifier = "smart-thermostat-01",
                DeviceFriendlyName = "Living Room Thermostat",
                DeviceType = DeviceCategory.Thermostat,
                PhysicalLocation = "Living Room",
                IsCurrentlyOnline = true,
                IsActive = true,
                ManufacturerName = "NexusHome",
                ModelNumber = "TH-2000",
                FirmwareVersion = "2.1.0",
                LastCommunicationTime = DateTime.UtcNow,
                CurrentPowerConsumption = 150
            },
            new SmartHomeDevice
            {
                UniqueDeviceIdentifier = "smart-light-01",
                DeviceFriendlyName = "Kitchen Smart Light",
                DeviceType = DeviceCategory.Lighting,
                PhysicalLocation = "Kitchen",
                IsCurrentlyOnline = true,
                IsActive = true,
                ManufacturerName = "LumiTech",
                ModelNumber = "LT-BULB-V2",
                FirmwareVersion = "1.5.2",
                LastCommunicationTime = DateTime.UtcNow,
                CurrentPowerConsumption = 12
            },
            new SmartHomeDevice
            {
                UniqueDeviceIdentifier = "smart-plug-01",
                DeviceFriendlyName = "Bedroom Smart Plug",
                DeviceType = DeviceCategory.SmartPlug,
                PhysicalLocation = "Bedroom",
                IsCurrentlyOnline = true,
                IsActive = true,
                ManufacturerName = "PowerGuard",
                ModelNumber = "PG-100",
                FirmwareVersion = "1.0.4",
                LastCommunicationTime = DateTime.UtcNow,
                CurrentPowerConsumption = 0
            },
            new SmartHomeDevice
            {
                UniqueDeviceIdentifier = "motion-sensor-01",
                DeviceFriendlyName = "Garage Motion Sensor",
                DeviceType = DeviceCategory.Sensor,
                PhysicalLocation = "Garage",
                IsCurrentlyOnline = true,
                IsActive = true,
                ManufacturerName = "SecureSense",
                ModelNumber = "MSS-500",
                FirmwareVersion = "3.2.1",
                LastCommunicationTime = DateTime.UtcNow,
                CurrentPowerConsumption = 0.5m
            },
            new SmartHomeDevice
            {
                UniqueDeviceIdentifier = "energy-monitor-01",
                DeviceFriendlyName = "Main Power Monitor",
                DeviceType = DeviceCategory.EnergyMeter,
                PhysicalLocation = "Electrical Panel",
                IsCurrentlyOnline = true,
                IsActive = true,
                ManufacturerName = "VoltMaster",
                ModelNumber = "VM-3000",
                FirmwareVersion = "4.0.0",
                LastCommunicationTime = DateTime.UtcNow,
                CurrentPowerConsumption = 1250
            }
        };

        context.SmartDevices.AddRange(demoDevices);
        logger.LogInformation("Added {Count} demo devices", demoDevices.Length);
        
        // Save devices first to generate IDs
        await context.SaveChangesAsync();

        // Add some sample energy readings
        var energyReadings = new List<DeviceEnergyConsumption>();
        var random = new Random();
        var startDate = DateTime.UtcNow.AddDays(-7);

        foreach (var device in demoDevices.Where(d => d.DeviceType != DeviceCategory.Sensor))
        {
            for (int i = 0; i < 168; i++) // 7 days * 24 hours
            {
                var timestamp = startDate.AddHours(i);
                var basePower = device.DeviceType switch
                {
                    DeviceCategory.Thermostat => 150,
                    DeviceCategory.Lighting => 12,
                    DeviceCategory.SmartPlug => 50,
                    DeviceCategory.EnergyMeter => 1200,
                    _ => 25
                };

                var power = basePower + (decimal)(random.NextDouble() * 20 - 10);
                if(power < 0) power = 0;

                energyReadings.Add(new DeviceEnergyConsumption
                {
                    SmartHomeDeviceId = device.Id,
                    PowerConsumptionKilowattHours = power / 1000m,
                    VoltageReading = 220 + (decimal)(random.NextDouble() * 10 - 5),
                    CurrentReading = power / 220,
                    FrequencyReading = 50,
                    PowerFactorReading = 0.95m + (decimal)(random.NextDouble() * 0.04),
                    MeasurementTimestamp = timestamp,
                    CostEstimate = (power / 1000m) * 0.12m
                });
            }
        }

        context.EnergyConsumptions.AddRange(energyReadings);
        logger.LogInformation("Added {Count} sample energy readings", energyReadings.Count);
    }

    private static async Task SeedDemoAutomationRulesAsync(SmartHomeDbContext context, ILogger logger)
    {
        var demoRules = new[]
        {
            new IntelligentAutomationRule
            {
                RuleName = "Evening Lights On",
                Description = "Turn on lights when it gets dark",
                TriggerCondition = "Time == Sunset",
                ActionCommand = "TurnOn Lights",
                IsActive = true,
                ConditionsJson = JsonSerializer.Serialize(new { type = "time", value = "sunset" }),
                ActionsJson = JsonSerializer.Serialize(new { type = "device", command = "turnOn" }),
                PriorityLevel = 1,
                CreatedAt = DateTime.UtcNow
            },
            new IntelligentAutomationRule
            {
                RuleName = "Motion Detected Security",
                Description = "High priority alert",
                TriggerCondition = "Motion == Detected",
                ActionCommand = "Notify User",
                IsActive = true,
                PriorityLevel = 2,
                ConditionsJson = JsonSerializer.Serialize(new { type = "sensor", value = "motion" }),
                ActionsJson = JsonSerializer.Serialize(new { type = "notification", importance = "high" }),
                CreatedAt = DateTime.UtcNow
            }
        };

        context.AutomationRules.AddRange(demoRules);
        logger.LogInformation("Added {Count} demo automation rules", demoRules.Length);
    }

    private static async Task SeedDefaultUserAsync(SmartHomeDbContext context, ILogger logger)
    {
        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@nexushome.local",
            PasswordHash = "admin123", // In real world use BCrypt
            Role = "Administrator",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Users.Add(adminUser);
        logger.LogInformation("Created default admin user");
    }
}
