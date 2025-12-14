using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusHome.IoT.Core.Domain
{
    public class DeviceTelemetryReading : BaseEntity
    {
        public int SmartHomeDeviceId { get; set; }
        public string ReadingType { get; set; } = string.Empty; // e.g., "Temperature", "Motion"
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public virtual SmartHomeDevice SmartHomeDevice { get; set; } = null!;
    }
}
