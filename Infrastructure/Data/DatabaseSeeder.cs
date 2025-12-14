using NexusHome.IoT.Core.Domain;

namespace NexusHome.IoT.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(SmartHomeDbContext context, IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            // Seed demo devices if none exist
            if (!context.SmartDevices.Any())
            {
                await SeedDemoDevicesAsync(context, logger);
            }

            // Seed automation rules if none exist
            if (!context.AutomationRules.Any())
            {
                await SeedDemoAutomationRulesAsync(context, logger);
            }

            // Seed default user if none exist
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
            new SmartDevice
            {
                DeviceId = "smart-thermostat-01",
                Name = "Living Room Thermostat",
                DeviceType = "Thermostat",
                Location = "Living Room",
                RoomId = 1,
                IsOnline = true,
                Properties = """{"temperature": 22.5, "humidity": 45, "targetTemp": 23}""",
                Configuration = """{"minTemp": 16, "maxTemp": 30, "autoMode": true}"""
            },
            new SmartDevice
            {
                DeviceId = "smart-light-01",
                Name = "Kitchen Smart Light",
                DeviceType = "SmartLight",
                Location = "Kitchen",
                RoomId = 2,
                IsOnline = true,
                Properties = """{"brightness": 80, "color": "#FFFFFF", "isOn": true}""",
                Configuration = """{"dimmable": true, "colorChangeable": true, "maxBrightness": 100}"""
            },
            new SmartDevice
            {
                DeviceId = "smart-plug-01",
                Name = "Bedroom Smart Plug",
                DeviceType = "SmartPlug",
                Location = "Bedroom",
                RoomId = 3,
                IsOnline = true,
                Properties = """{"isOn": false, "powerConsumption": 0}""",
                Configuration = """{"maxPower": 2000, "scheduleEnabled": true}"""
            },
            new SmartDevice
            {
                DeviceId = "motion-sensor-01",
                Name = "Garage Motion Sensor",
                DeviceType = "MotionSensor",
                Location = "Garage",
                RoomId = 5,
                IsOnline = true,
                Properties = """{"motionDetected": false, "batteryLevel": 85}""",
                Configuration = """{"sensitivity": "medium", "timeout": 300}"""
            },
            new SmartDevice
            {
                DeviceId = "energy-monitor-01",
                Name = "Main Power Monitor",
                DeviceType = "EnergyMonitor",
                Location = "Electrical Panel",
                IsOnline = true,
                Properties = """{"totalPower": 1250, "voltage": 240, "frequency": 50}""",
                Configuration = """{"alertThreshold": 2000, "reportingInterval": 60}"""
            }
        };

        context.SmartDevices.AddRange(demoDevices);
        logger.LogInformation("Added {Count} demo devices", demoDevices.Length);

        // Add some sample energy readings
        var energyReadings = new List<EnergyReading>();
        var random = new Random();
        var startDate = DateTime.UtcNow.AddDays(-7);

        foreach (var device in demoDevices.Where(d => d.DeviceType != "MotionSensor"))
        {
            for (int i = 0; i < 168; i++) // 7 days * 24 hours
            {
                var timestamp = startDate.AddHours(i);
                var basePower = device.DeviceType switch
                {
                    "Thermostat" => 150,
                    "SmartLight" => 12,
                    "SmartPlug" => 50,
                    "EnergyMonitor" => 1200,
                    _ => 25
                };

                energyReadings.Add(new EnergyReading
                {
                    DeviceId = device.DeviceId,
                    PowerConsumption = basePower + (decimal)(random.NextDouble() * 20 - 10),
                    Voltage = 240 + (decimal)(random.NextDouble() * 10 - 5),
                    Current = (basePower + (decimal)(random.NextDouble() * 20 - 10)) / 240,
                    Cost = (basePower / 1000m) * 0.12m,
                    Timestamp = timestamp
                });
            }
        }

        context.EnergyReadings.AddRange(energyReadings);
        logger.LogInformation("Added {Count} sample energy readings", energyReadings.Count);
    }

    private static async Task SeedDemoAutomationRulesAsync(SmartHomeDbContext context, ILogger logger)
    {
        var demoRules = new[]
        {
            new AutomationRule
            {
                Name = "Evening Lights On",
                Description = "Turn on lights when it gets dark",
                TriggerCondition = """{"type": "time", "value": "sunset", "offset": 0}""",
                Action = """{"type": "device", "devices": ["smart-light-01"], "command": "turnOn", "parameters": {"brightness": 70}}""",
                IsActive = true
            },
            new AutomationRule
            {
                Name = "Motion Detected Security",
                Description = "Turn on lights when motion detected in garage",
                TriggerCondition = """{"type": "device", "deviceId": "motion-sensor-01", "property": "motionDetected", "value": true}""",
                Action = """{"type": "notification", "message": "Motion detected in garage", "priority": "high"}""",
                IsActive = true
            },
            new AutomationRule
            {
                Name = "Energy Savings Night Mode",
                Description = "Reduce power consumption at night",
                TriggerCondition = """{"type": "time", "value": "23:00", "days": ["weekday"]}""",
                Action = """{"type": "device", "devices": ["smart-thermostat-01"], "command": "setTemperature", "parameters": {"temperature": 18}}""",
                IsActive = false
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
