using System;
using System.ComponentModel.DataAnnotations;

namespace NexusHome.IoT.Core.Domain
{
    public class IntelligentAutomationRule : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string TriggerType { get; set; } = string.Empty; // e.g., "Time", "Sensor"
        public string Condition { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int? SmartHomeDeviceId { get; set; }
        
        public bool IsEnabled { get; set; } = true;
        public DateTime? LastExecuted { get; set; }
        public int ExecutionCount { get; set; }
        public virtual SmartHomeDevice? SmartHomeDevice { get; set; }
    }
}
