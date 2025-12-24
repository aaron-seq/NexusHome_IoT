using NexusHome.IoT.Core.Domain;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using BCrypt.Net;

namespace NexusHome.IoT.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        SmartHomeDbContext context, 
        IServiceProvider serviceProvider, 
        ILogger logger,
        IHostEnvironment environment)
    {
        try
        {
            logger.LogInformation("Starting database seeding for environment: {Environment}", environment.EnvironmentName);

            if (!context.SmartDevices.Any())
            {
                await SeedDemoDevicesAsync(context, logger);
            }
            else
            {
                logger.LogInformation("Devices already seeded, skipping");
            }

            if (!context.AutomationRules.Any())
            {
                await SeedDemoAutomationRulesAsync(context, logger);
            }
            else
            {
                logger.LogInformation("Automation rules already seeded, skipping");
            }

            // Only seed default user in Development environment
            if (environment.IsDevelopment() && !context.Users.Any())
            {
                await SeedDefaultUserAsync(context, logger);
            }
            else if (context.Users.Any())
            {
                logger.LogInformation("Users already exist, skipping user seeding");
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
        var baseTimestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
                LastCommunicationTime = baseTimestamp,
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
                LastCommunicationTime = baseTimestamp,
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
                LastCommunicationTime = baseTimestamp,
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
                LastCommunicationTime = baseTimestamp,
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
                LastCommunicationTime = baseTimestamp,
                CurrentPowerConsumption = 1250
            }
        };

        context.SmartDevices.AddRange(demoDevices);
        logger.LogInformation("Added {Count} demo devices", demoDevices.Length);
        
        await context.SaveChangesAsync();

        // Add deterministic sample energy readings
        var energyReadings = new List<DeviceEnergyConsumption>();
        var startDate = baseTimestamp.AddDays(-7);

        foreach (var device in demoDevices.Where(d => d.DeviceType != DeviceCategory.Sensor))
        {
            for (int i = 0; i < 168; i++) // 7 days * 24 hours
            {
                var timestamp = startDate.AddHours(i);
                var basePower = device.DeviceType switch
                {
                    DeviceCategory.Thermostat => 150m,
                    DeviceCategory.Lighting => 12m,
                    DeviceCategory.SmartPlug => 50m,
                    DeviceCategory.EnergyMeter => 1200m,
                    _ => 25m
                };

                // Deterministic variation based on hour
                var hourVariation = (i % 24) * 0.5m;
                var power = basePower + hourVariation;

                energyReadings.Add(new DeviceEnergyConsumption
                {
                    SmartHomeDeviceId = device.Id,
                    PowerConsumptionKilowattHours = power / 1000m,
                    VoltageReading = 220m,
                    CurrentReading = power / 220m,
                    FrequencyReading = 50m,
                    PowerFactorReading = 0.95m,
                    MeasurementTimestamp = timestamp,
                    CostEstimate = (power / 1000m) * 0.12m
                });
            }
        }

        context.EnergyConsumptions.AddRange(energyReadings);
        logger.LogInformation("Added {Count} deterministic energy readings", energyReadings.Count);
    }

    private static async Task SeedDemoAutomationRulesAsync(SmartHomeDbContext context, ILogger logger)
    {
        var baseTimestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
                CreatedAt = baseTimestamp
            },
            new IntelligentAutomationRule
            {
                RuleName = "Motion Detected Security",
                Description = "Send notification when motion detected",
                TriggerCondition = "Motion == Detected",
                ActionCommand = "Notify User",
                IsActive = true,
                PriorityLevel = 2,
                ConditionsJson = JsonSerializer.Serialize(new { type = "sensor", value = "motion" }),
                ActionsJson = JsonSerializer.Serialize(new { type = "notification", importance = "high" }),
                CreatedAt = baseTimestamp
            },
            new IntelligentAutomationRule
            {
                RuleName = "Energy Saving Mode",
                Description = "Reduce power consumption during peak hours",
                TriggerCondition = "Time >= 14:00 AND Time <= 18:00",
                ActionCommand = "Optimize Energy",
                IsActive = true,
                PriorityLevel = 3,
                ConditionsJson = JsonSerializer.Serialize(new { type = "time", range = new { start = "14:00", end = "18:00" } }),
                ActionsJson = JsonSerializer.Serialize(new { type = "optimization", action = "reduce_consumption" }),
                CreatedAt = baseTimestamp
            }
        };

        context.AutomationRules.AddRange(demoRules);
        logger.LogInformation("Added {Count} demo automation rules", demoRules.Length);
    }

    private static async Task SeedDefaultUserAsync(SmartHomeDbContext context, ILogger logger)
    {
        // WARNING: This is for DEVELOPMENT ONLY
        // In production, users should be created through registration API
        const string defaultPassword = "Admin123!";
        
        logger.LogWarning("Creating default admin user for DEVELOPMENT environment");
        logger.LogWarning("Default credentials - Username: admin, Password: {Password}", defaultPassword);
        logger.LogWarning("CHANGE THIS PASSWORD IMMEDIATELY IN PRODUCTION!");

        var passwordHash = BCrypt.HashPassword(defaultPassword, workFactor: 12);

        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@nexushome.local",
            PasswordHash = passwordHash,
            Role = "Administrator",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true
        };

        context.Users.Add(adminUser);
        logger.LogInformation("Created default admin user (DEVELOPMENT ONLY)");
    }
}
