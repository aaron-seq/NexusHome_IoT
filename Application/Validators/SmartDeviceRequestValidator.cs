using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.Application.DTOs;
using NexusHome.IoT.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace NexusHome.IoT.Application.Validators;

/// <summary>
/// Comprehensive validator for smart device registration and update requests
/// Implements business rules, data integrity checks, and security validations
/// Provides detailed error messages for API consumers and user interfaces
/// </summary>
public class SmartDeviceRequestValidator : AbstractValidator<SmartDeviceRequestDto>
{
    private readonly SmartHomeDbContext _databaseContext;

    // Regex patterns for validation
    private static readonly Regex MacAddressPattern = new(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$", 
        RegexOptions.Compiled);
    private static readonly Regex IpAddressPattern = new(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$|^(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}$", 
        RegexOptions.Compiled);
    private static readonly Regex DeviceIdentifierPattern = new(@"^[a-zA-Z0-9][a-zA-Z0-9-_]{0,98}[a-zA-Z0-9]$", 
        RegexOptions.Compiled);

    // Known device manufacturers and their typical model patterns
    private static readonly Dictionary<string, string[]> KnownManufacturers = new()
    {
        { "Philips", new[] { "Hue", "Wiz" } },
        { "Samsung", new[] { "SmartThings", "Galaxy" } },
        { "Amazon", new[] { "Echo", "Ring", "Alexa" } },
        { "Google", new[] { "Nest", "Home" } },
        { "TP-Link", new[] { "Kasa", "Tapo" } },
        { "Sonos", new[] { "Play", "One", "Arc" } },
        { "Ecobee", new[] { "SmartThermostat", "SmartSensor" } },
        { "Ring", new[] { "Video", "Doorbell", "Spotlight" } },
        { "Arlo", new[] { "Pro", "Ultra", "Essential" } }
    };

    // Realistic power consumption ranges by device category (in watts)
    private static readonly Dictionary<string, (decimal Min, decimal Max)> DevicePowerRanges = new()
    {
        { "lighting", (1m, 100m) },
        { "climate_control", (50m, 5000m) },
        { "security", (5m, 50m) },
        { "entertainment", (20m, 500m) },
        { "appliance", (100m, 3000m) },
        { "sensor", (0.1m, 5m) },
        { "battery_storage", (0m, 10000m) },
        { "solar_panel", (0m, 15000m) },
        { "electric_vehicle_charger", (1000m, 22000m) }
    };

    public SmartDeviceRequestValidator(SmartHomeDbContext databaseContext)
    {
        _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));

        ConfigureDeviceIdentifierValidation();
        ConfigureFriendlyNameValidation();
        ConfigureDescriptionValidation();
        ConfigureDeviceCategoryValidation();
        ConfigureManufacturerValidation();
        ConfigureModelAndFirmwareValidation();
        ConfigureCommunicationProtocolValidation();
        ConfigureLocationValidation();
        ConfigurePowerRatingValidation();
        ConfigureNetworkConfigurationValidation();
        ConfigureMqttConfigurationValidation();
        ConfigureMetadataValidation();
        ConfigureBusinessRuleValidation();
    }

    private void ConfigureDeviceIdentifierValidation()
    {
        RuleFor(request => request.DeviceIdentifier)
            .NotEmpty()
            .WithMessage("Device identifier is required and cannot be empty")
            .Length(3, 100)
            .WithMessage("Device identifier must be between 3 and 100 characters")
            .Matches(DeviceIdentifierPattern)
            .WithMessage("Device identifier must start and end with alphanumeric characters and contain only letters, numbers, hyphens, and underscores")
            .Must(BeUniqueDeviceIdentifier)
            .WithMessage("A device with this identifier already exists in the system")
            .Must(NotContainReservedKeywords)
            .WithMessage("Device identifier cannot contain reserved system keywords");
    }

    private void ConfigureFriendlyNameValidation()
    {
        RuleFor(request => request.FriendlyName)
            .NotEmpty()
            .WithMessage("Friendly name is required for device identification")
            .Length(2, 200)
            .WithMessage("Friendly name must be between 2 and 200 characters")
            .Must(ContainValidCharacters)
            .WithMessage("Friendly name contains invalid characters. Only letters, numbers, spaces, and basic punctuation are allowed")
            .Must(NotStartOrEndWithWhitespace)
            .WithMessage("Friendly name cannot start or end with whitespace characters");
    }

    private void ConfigureDescriptionValidation()
    {
        RuleFor(request => request.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters")
            .Must(BeValidDescriptionFormat)
            .WithMessage("Description contains invalid formatting or unsafe content");
    }

    private void ConfigureDeviceCategoryValidation()
    {
        RuleFor(request => request.DeviceCategory)
            .NotEmpty()
            .WithMessage("Device category is required for proper classification")
            .Must(BeValidDeviceCategory)
            .WithMessage("Invalid device category. Must be one of: lighting, climate_control, security, entertainment, appliance, sensor, battery_storage, solar_panel, electric_vehicle_charger")
            .Must((request, category) => BeCategoryCompatibleWithManufacturer(request, category))
            .WithMessage("Device category is not typically associated with the specified manufacturer");
    }

    private void ConfigureManufacturerValidation()
    {
        RuleFor(request => request.ManufacturerName)
            .NotEmpty()
            .WithMessage("Manufacturer name is required")
            .Length(2, 100)
            .WithMessage("Manufacturer name must be between 2 and 100 characters")
            .Must(BeValidManufacturerName)
            .WithMessage("Manufacturer name contains invalid characters or format");
    }

    private void ConfigureModelAndFirmwareValidation()
    {
        RuleFor(request => request.ModelNumber)
            .MaximumLength(100)
            .WithMessage("Model number cannot exceed 100 characters")
            .Must(BeValidModelNumber)
            .WithMessage("Model number contains invalid characters");

        RuleFor(request => request.FirmwareVersion)
            .MaximumLength(50)
            .WithMessage("Firmware version cannot exceed 50 characters")
            .Must(BeValidVersionFormat)
            .WithMessage("Firmware version must follow semantic versioning format (e.g., 1.2.3, 1.0.0-beta)");
    }

    private void ConfigureCommunicationProtocolValidation()
    {
        RuleFor(request => request.CommunicationProtocol)
            .MaximumLength(50)
            .WithMessage("Communication protocol cannot exceed 50 characters")
            .Must(BeValidCommunicationProtocol)
            .WithMessage("Invalid communication protocol. Supported protocols: WiFi, Ethernet, Zigbee, Z-Wave, Bluetooth, LoRa, Matter, Thread");
    }

    private void ConfigureLocationValidation()
    {
        RuleFor(request => request.PhysicalLocation)
            .MaximumLength(200)
            .WithMessage("Physical location cannot exceed 200 characters")
            .Must(BeValidLocationFormat)
            .WithMessage("Physical location contains invalid characters or unsafe content");

        RuleFor(request => request.AssignedRoom)
            .MaximumLength(100)
            .WithMessage("Room assignment cannot exceed 100 characters")
            .Must(BeValidRoomName)
            .WithMessage("Room assignment contains invalid characters");
    }

    private void ConfigurePowerRatingValidation()
    {
        RuleFor(request => request.MaximumPowerRatingWatts)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Maximum power rating cannot be negative")
            .LessThanOrEqualTo(100000)
            .WithMessage("Maximum power rating cannot exceed 100,000 watts (unrealistic for residential devices)")
            .Must((request, powerRating) => BeRealisticPowerRatingForCategory(request.DeviceCategory, powerRating))
            .WithMessage("Power rating is unrealistic for the specified device category");
    }

    private void ConfigureNetworkConfigurationValidation()
    {
        RuleFor(request => request.NetworkConfiguration)
            .SetValidator(new DeviceNetworkConfigurationValidator()!)
            .When(request => request.NetworkConfiguration != null);
    }

    private void ConfigureMqttConfigurationValidation()
    {
        RuleFor(request => request.MqttConfiguration)
            .SetValidator(new DeviceMqttConfigurationValidator()!)
            .When(request => request.MqttConfiguration != null);
    }

    private void ConfigureMetadataValidation()
    {
        RuleFor(request => request.AdditionalMetadata)
            .Must(HaveValidMetadataSize)
            .WithMessage("Additional metadata cannot contain more than 50 key-value pairs")
            .Must(HaveValidMetadataKeys)
            .WithMessage("Additional metadata keys must be valid strings without special characters")
            .Must(HaveValidMetadataValues)
            .WithMessage("Additional metadata values must be serializable and reasonable in size")
            .When(request => request.AdditionalMetadata != null && request.AdditionalMetadata.Any());
    }

    private void ConfigureBusinessRuleValidation()
    {
        // Business rule: Battery-powered devices should have appropriate power ratings
        RuleFor(request => request)
            .Must(HaveAppropriateConfigurationForBatteryDevices)
            .WithMessage("Battery-powered devices should have power ratings under 50 watts")
            .When(request => IsBatteryPoweredDevice(request));

        // Business rule: Solar devices should have appropriate power generation ratings
        RuleFor(request => request)
            .Must(HaveAppropriateConfigurationForSolarDevices)
            .WithMessage("Solar devices should have power generation ratings between 100W and 15kW")
            .When(request => IsSolarDevice(request));

        // Business rule: Security devices should have appropriate communication protocols
        RuleFor(request => request)
            .Must(HaveSecureConnectionForSecurityDevices)
            .WithMessage("Security devices should use secure communication protocols (WiFi, Ethernet, or encrypted protocols)")
            .When(request => IsSecurityDevice(request));

        // Cross-field validation: MQTT configuration should match communication protocol
        RuleFor(request => request)
            .Must(HaveMqttConfigurationForMqttDevices)
            .WithMessage("Devices using MQTT communication should have MQTT configuration specified")
            .When(request => UsesMqttCommunication(request));
    }

    // Validation helper methods
    private bool BeUniqueDeviceIdentifier(string deviceIdentifier)
    {
        try
        {
            return !_databaseContext.SmartDevices
                .Any(device => device.UniqueDeviceIdentifier == deviceIdentifier);
        }
        catch
        {
            // If database is unavailable, allow validation to pass (will be caught at save time)
            return true;
        }
    }

    private static bool NotContainReservedKeywords(string deviceIdentifier)
    {
        var reservedKeywords = new[] { "admin", "root", "system", "config", "setup", "test", "debug" };
        var lowerIdentifier = deviceIdentifier.ToLowerInvariant();
        return !reservedKeywords.Any(keyword => lowerIdentifier.Contains(keyword));
    }

    private static bool ContainValidCharacters(string friendlyName)
    {
        // Allow letters, numbers, spaces, and basic punctuation
        return Regex.IsMatch(friendlyName, @"^[a-zA-Z0-9\s\-_.,()!]+$");
    }

    private static bool NotStartOrEndWithWhitespace(string input)
    {
        return input == input.Trim();
    }

    private static bool BeValidDescriptionFormat(string? description)
    {
        if (string.IsNullOrEmpty(description)) return true;
        
        // Check for potentially unsafe content
        var unsafePatterns = new[] { "<script", "javascript:", "<iframe", "<embed", "<object" };
        var lowerDescription = description.ToLowerInvariant();
        return !unsafePatterns.Any(pattern => lowerDescription.Contains(pattern));
    }

    private static bool BeValidDeviceCategory(string category)
    {
        var validCategories = new[] 
        { 
            "lighting", "climate_control", "security", "entertainment", 
            "appliance", "sensor", "battery_storage", "solar_panel", 
            "electric_vehicle_charger", "smart_outlet", "smart_switch", 
            "smart_lock", "camera", "thermostat", "speaker" 
        };
        return validCategories.Contains(category.ToLowerInvariant());
    }

    private static bool BeCategoryCompatibleWithManufacturer(SmartDeviceRequestDto request, string category)
    {
        // Some basic compatibility checks
        var manufacturer = request.ManufacturerName.ToLowerInvariant();
        var deviceCategory = category.ToLowerInvariant();

        // Tesla should not make lighting devices, etc.
        var incompatibleCombinations = new Dictionary<string, string[]>
        {
            { "tesla", new[] { "lighting", "speaker", "camera" } },
            { "sonos", new[] { "lighting", "security", "climate_control" } },
            { "ring", new[] { "climate_control", "entertainment", "appliance" } }
        };

        if (incompatibleCombinations.TryGetValue(manufacturer, out var incompatibleCategories))
        {
            return !incompatibleCategories.Contains(deviceCategory);
        }

        return true; // Allow most combinations by default
    }

    private static bool BeValidManufacturerName(string manufacturerName)
    {
        // Allow letters, numbers, spaces, and basic punctuation for company names
        return Regex.IsMatch(manufacturerName, @"^[a-zA-Z0-9\s\-&.,()]+$");
    }

    private static bool BeValidModelNumber(string? modelNumber)
    {
        if (string.IsNullOrEmpty(modelNumber)) return true;
        
        // Allow alphanumeric, hyphens, underscores, spaces, and dots
        return Regex.IsMatch(modelNumber, @"^[a-zA-Z0-9\s\-_.]+$");
    }

    private static bool BeValidVersionFormat(string? version)
    {
        if (string.IsNullOrEmpty(version)) return true;
        
        // Semantic versioning pattern (major.minor.patch with optional pre-release)
        return Regex.IsMatch(version, @"^\d+\.\d+\.\d+(?:-[a-zA-Z0-9-]+)?(?:\+[a-zA-Z0-9-]+)?$");
    }

    private static bool BeValidCommunicationProtocol(string? protocol)
    {
        if (string.IsNullOrEmpty(protocol)) return true;
        
        var validProtocols = new[] 
        { 
            "wifi", "ethernet", "zigbee", "z-wave", "bluetooth", "lora", 
            "matter", "thread", "mqtt", "coap", "http", "https" 
        };
        
        return validProtocols.Contains(protocol.ToLowerInvariant());
    }

    private static bool BeValidLocationFormat(string? location)
    {
        if (string.IsNullOrEmpty(location)) return true;
        
        // Check for potentially unsafe content and ensure reasonable format
        var unsafePatterns = new[] { "<", ">", "javascript:", "<script" };
        var lowerLocation = location.ToLowerInvariant();
        return !unsafePatterns.Any(pattern => lowerLocation.Contains(pattern));
    }

    private static bool BeValidRoomName(string? roomName)
    {
        if (string.IsNullOrEmpty(roomName)) return true;
        
        // Allow letters, numbers, spaces, and basic punctuation for room names
        return Regex.IsMatch(roomName, @"^[a-zA-Z0-9\s\-_.()]+$");
    }

    private static bool BeRealisticPowerRatingForCategory(string category, decimal powerRating)
    {
        var categoryKey = category.ToLowerInvariant();
        
        if (DevicePowerRanges.TryGetValue(categoryKey, out var range))
        {
            return powerRating >= range.Min && powerRating <= range.Max;
        }
        
        // Default reasonable range if category not found
        return powerRating >= 0.1m && powerRating <= 50000m;
    }

    private static bool HaveValidMetadataSize(Dictionary<string, object>? metadata)
    {
        return metadata == null || metadata.Count <= 50;
    }

    private static bool HaveValidMetadataKeys(Dictionary<string, object>? metadata)
    {
        if (metadata == null) return true;
        
        return metadata.Keys.All(key => 
            !string.IsNullOrWhiteSpace(key) && 
            key.Length <= 100 && 
            Regex.IsMatch(key, @"^[a-zA-Z0-9_.-]+$"));
    }

    private static bool HaveValidMetadataValues(Dictionary<string, object>? metadata)
    {
        if (metadata == null) return true;
        
        foreach (var value in metadata.Values)
        {
            if (value == null) continue;
            
            // Check serialized size is reasonable (approximate)
            var serializedValue = System.Text.Json.JsonSerializer.Serialize(value);
            if (serializedValue.Length > 10000) // 10KB limit per value
            {
                return false;
            }
        }
        
        return true;
    }

    private static bool IsBatteryPoweredDevice(SmartDeviceRequestDto request)
    {
        var batteryKeywords = new[] { "battery", "portable", "wireless", "cordless" };
        var description = (request.Description ?? "").ToLowerInvariant();
        var modelNumber = (request.ModelNumber ?? "").ToLowerInvariant();
        var friendlyName = request.FriendlyName.ToLowerInvariant();
        
        return batteryKeywords.Any(keyword => 
            description.Contains(keyword) || 
            modelNumber.Contains(keyword) || 
            friendlyName.Contains(keyword));
    }

    private static bool IsSolarDevice(SmartDeviceRequestDto request)
    {
        return request.DeviceCategory.ToLowerInvariant() == "solar_panel" ||
               request.FriendlyName.ToLowerInvariant().Contains("solar") ||
               (request.Description ?? "").ToLowerInvariant().Contains("solar");
    }

    private static bool IsSecurityDevice(SmartDeviceRequestDto request)
    {
        return request.DeviceCategory.ToLowerInvariant() == "security" ||
               request.DeviceCategory.ToLowerInvariant() == "camera" ||
               request.DeviceCategory.ToLowerInvariant() == "smart_lock";
    }

    private static bool UsesMqttCommunication(SmartDeviceRequestDto request)
    {
        return request.CommunicationProtocol?.ToLowerInvariant() == "mqtt";
    }

    private static bool HaveAppropriateConfigurationForBatteryDevices(SmartDeviceRequestDto request)
    {
        // Battery devices should typically have lower power consumption
        return request.MaximumPowerRatingWatts <= 50m;
    }

    private static bool HaveAppropriateConfigurationForSolarDevices(SmartDeviceRequestDto request)
    {
        // Solar devices should have appropriate power generation capacity
        return request.MaximumPowerRatingWatts >= 100m && request.MaximumPowerRatingWatts <= 15000m;
    }

    private static bool HaveSecureConnectionForSecurityDevices(SmartDeviceRequestDto request)
    {
        var secureProtocols = new[] { "wifi", "ethernet", "https" };
        var protocol = request.CommunicationProtocol?.ToLowerInvariant();
        return protocol == null || secureProtocols.Contains(protocol);
    }

    private static bool HaveMqttConfigurationForMqttDevices(SmartDeviceRequestDto request)
    {
        // If device uses MQTT, it should have MQTT configuration
        return request.MqttConfiguration != null;
    }
}

/// <summary>
/// Validator for device network configuration
/// </summary>
public class DeviceNetworkConfigurationValidator : AbstractValidator<DeviceNetworkConfigurationDto>
{
    public DeviceNetworkConfigurationValidator()
    {
        RuleFor(config => config.IpAddress)
            .Must(BeValidIpAddress)
            .WithMessage("Invalid IP address format. Must be valid IPv4 or IPv6 address")
            .When(config => !string.IsNullOrEmpty(config.IpAddress));

        RuleFor(config => config.MacAddress)
            .Matches(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$")
            .WithMessage("Invalid MAC address format. Must be in XX:XX:XX:XX:XX:XX format")
            .When(config => !string.IsNullOrEmpty(config.MacAddress));

        RuleFor(config => config.SubnetMask)
            .Must(BeValidSubnetMask)
            .WithMessage("Invalid subnet mask format")
            .When(config => !string.IsNullOrEmpty(config.SubnetMask));

        RuleFor(config => config.GatewayAddress)
            .Must(BeValidIpAddress)
            .WithMessage("Invalid gateway address format")
            .When(config => !string.IsNullOrEmpty(config.GatewayAddress));

        RuleFor(config => config.DnsServers)
            .Must(AllBeValidIpAddresses)
            .WithMessage("All DNS server addresses must be valid IP addresses")
            .When(config => config.DnsServers != null && config.DnsServers.Any());
    }

    private static bool BeValidIpAddress(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return true;
        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }

    private static bool BeValidSubnetMask(string? subnetMask)
    {
        if (string.IsNullOrEmpty(subnetMask)) return true;
        
        // Validate subnet mask format (e.g., 255.255.255.0 or /24)
        if (subnetMask.StartsWith('/') && int.TryParse(subnetMask[1..], out var cidr))
        {
            return cidr >= 0 && cidr <= 32;
        }
        
        return System.Net.IPAddress.TryParse(subnetMask, out var addr) && IsValidSubnetMask(addr);
    }

    private static bool IsValidSubnetMask(System.Net.IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        var mask = System.BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
        
        // Check if it's a valid subnet mask (contiguous 1s followed by 0s)
        var inverted = ~mask;
        return (inverted & (inverted + 1)) == 0;
    }

    private static bool AllBeValidIpAddresses(List<string>? dnsServers)
    {
        if (dnsServers == null || !dnsServers.Any()) return true;
        return dnsServers.All(dns => System.Net.IPAddress.TryParse(dns, out _));
    }
}

/// <summary>
/// Validator for device MQTT configuration
/// </summary>
public class DeviceMqttConfigurationValidator : AbstractValidator<DeviceMqttConfigurationDto>
{
    public DeviceMqttConfigurationValidator()
    {
        RuleFor(config => config.TopicPath)
            .NotEmpty()
            .WithMessage("MQTT topic path is required")
            .Length(1, 300)
            .WithMessage("MQTT topic path must be between 1 and 300 characters")
            .Must(BeValidMqttTopic)
            .WithMessage("Invalid MQTT topic format. Topics cannot contain wildcards and must follow MQTT naming conventions");

        RuleFor(config => config.QualityOfServiceLevel)
            .InclusiveBetween(0, 2)
            .WithMessage("QoS level must be 0 (at most once), 1 (at least once), or 2 (exactly once)");

        RuleFor(config => config.CustomClientId)
            .MaximumLength(100)
            .WithMessage("MQTT client ID cannot exceed 100 characters")
            .Must(BeValidMqttClientId)
            .WithMessage("Invalid MQTT client ID format")
            .When(config => !string.IsNullOrEmpty(config.CustomClientId));

        RuleFor(config => config.HeartbeatIntervalSeconds)
            .InclusiveBetween(10, 3600)
            .WithMessage("Heartbeat interval must be between 10 seconds and 1 hour");
    }

    private static bool BeValidMqttTopic(string topicPath)
    {
        // MQTT topic validation: no wildcards (+, #), no null characters, valid structure
        if (string.IsNullOrEmpty(topicPath)) return false;
        if (topicPath.Contains('\0')) return false;
        if (topicPath.Contains('+') || topicPath.Contains('#')) return false;
        if (topicPath.StartsWith('/') || topicPath.EndsWith('/')) return false;
        
        // Check for valid characters and structure
        return Regex.IsMatch(topicPath, @"^[a-zA-Z0-9/_-]+(?:/[a-zA-Z0-9/_-]+)*$");
    }

    private static bool BeValidMqttClientId(string? clientId)
    {
        if (string.IsNullOrEmpty(clientId)) return true;
        
        // MQTT client ID should contain only printable ASCII characters except space
        return Regex.IsMatch(clientId, @"^[!-~]+$");
    }
}

/// <summary>
/// Validator for device telemetry submission requests
/// </summary>
public class DeviceTelemetrySubmissionValidator : AbstractValidator<DeviceTelemetrySubmissionDto>
{
    public DeviceTelemetrySubmissionValidator()
    {
        RuleFor(telemetry => telemetry.DeviceIdentifier)
            .NotEmpty()
            .WithMessage("Device identifier is required")
            .Length(3, 100)
            .WithMessage("Device identifier must be between 3 and 100 characters");

        RuleFor(telemetry => telemetry.TelemetryTimestamp)
            .NotEmpty()
            .WithMessage("Telemetry timestamp is required")
            .Must(BeReasonableTimestamp)
            .WithMessage("Telemetry timestamp must be within the last 24 hours and not in the future");

        RuleFor(telemetry => telemetry.SensorReadings)
            .NotEmpty()
            .WithMessage("At least one sensor reading is required")
            .Must(HaveValidSensorData)
            .WithMessage("Sensor readings contain invalid data types or exceed size limits");

        RuleFor(telemetry => telemetry.CurrentPowerConsumptionWatts)
            .InclusiveBetween(0, 100000)
            .WithMessage("Power consumption must be between 0 and 100,000 watts")
            .When(telemetry => telemetry.CurrentPowerConsumptionWatts.HasValue);

        RuleFor(telemetry => telemetry.OperatingTemperatureCelsius)
            .InclusiveBetween(-50, 200)
            .WithMessage("Operating temperature must be between -50°C and 200°C")
            .When(telemetry => telemetry.OperatingTemperatureCelsius.HasValue);

        RuleFor(telemetry => telemetry.BatteryLevelPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Battery level must be between 0% and 100%")
            .When(telemetry => telemetry.BatteryLevelPercentage.HasValue);

        RuleFor(telemetry => telemetry.NetworkSignalStrength)
            .InclusiveBetween(0, 100)
            .WithMessage("Network signal strength must be between 0 and 100")
            .When(telemetry => telemetry.NetworkSignalStrength.HasValue);

        RuleFor(telemetry => telemetry.DataQualityIndicator)
            .Must(BeValidDataQuality)
            .WithMessage("Data quality indicator must be one of: Poor, Fair, Good, Excellent");
    }

    private static bool BeReasonableTimestamp(DateTime timestamp)
    {
        var now = DateTime.UtcNow;
        var oneDayAgo = now.AddDays(-1);
        var fiveMinutesFromNow = now.AddMinutes(5); // Allow small clock skew
        
        return timestamp >= oneDayAgo && timestamp <= fiveMinutesFromNow;
    }

    private static bool HaveValidSensorData(Dictionary<string, object> sensorReadings)
    {
        if (sensorReadings.Count > 100) return false; // Reasonable limit
        
        foreach (var reading in sensorReadings)
        {
            if (string.IsNullOrWhiteSpace(reading.Key) || reading.Key.Length > 100)
                return false;
            
            // Check if value can be serialized and is reasonable size
            try
            {
                var serialized = System.Text.Json.JsonSerializer.Serialize(reading.Value);
                if (serialized.Length > 5000) return false; // 5KB per reading
            }
            catch
            {
                return false;
            }
        }
        
        return true;
    }

    private static bool BeValidDataQuality(string dataQuality)
    {
        var validQualities = new[] { "Poor", "Fair", "Good", "Excellent" };
        return validQualities.Contains(dataQuality);
    }
}
