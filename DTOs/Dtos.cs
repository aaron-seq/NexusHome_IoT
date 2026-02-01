using NexusHome.IoT.Core.Domain;

namespace NexusHome.IoT.DTOs
{
    // DTO Models for API responses
    public class DeviceDto
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DeviceCategory Type { get; set; }
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public DeviceOperationalStatus Status { get; set; }
        public string? Location { get; set; }
        public string? Room { get; set; }
        public decimal PowerRating { get; set; }
        public decimal CurrentPowerConsumption { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class EnergyConsumptionDto
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public decimal PowerConsumption { get; set; }
        public decimal Cost { get; set; }
        public DateTime Timestamp { get; set; }
        public EnergySourceType Source { get; set; }
    }

    public class EnergyDashboardDto
    {
        public decimal TotalConsumption { get; set; }
        public decimal TotalCost { get; set; }
        public decimal SolarGeneration { get; set; }
        public decimal BatteryLevel { get; set; }
        public decimal CostSavings { get; set; }
        public decimal CarbonFootprint { get; set; }
        public List<DeviceConsumptionDto> TopConsumers { get; set; } = new();
        public List<EnergyConsumptionDto> RecentConsumption { get; set; } = new();
    }

    public class DeviceConsumptionDto
    {
        public string DeviceName { get; set; } = string.Empty;
        public DeviceCategory Type { get; set; }
        public decimal PowerConsumption { get; set; }
        public decimal Cost { get; set; }
        public decimal Percentage { get; set; }
    }

    public class DeviceCommand
    {
        public string Command { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}

