using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusHome.IoT.Core.Domain
{
    public class DeviceMaintenanceRecord : BaseEntity
    {
        public int SmartHomeDeviceId { get; set; }
        public DateTime MaintenanceDate { get; set; }
        public string MaintenanceType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        
        public virtual SmartHomeDevice SmartHomeDevice { get; set; } = null!;
    }
}
