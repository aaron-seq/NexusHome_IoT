using System;
using System.ComponentModel.DataAnnotations;

namespace NexusHome.IoT.Core.Domain
{
    public class DeviceAlert : BaseEntity
    {
        public int SmartHomeDeviceId { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info";
        public bool IsAcknowledged { get; set; }
        
        public virtual SmartHomeDevice SmartHomeDevice { get; set; } = null!;
    }
}
