using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public int Priority { get; set; } = 5;
        
        // Stores JSON representation of conditions
        public string? SerializedConditions { get; set; }
        
        // Stores JSON representation of actions
        public string? SerializedActions { get; set; }
        
        public virtual SmartHomeDevice? SmartHomeDevice { get; set; }

        // Backward-compatible alias properties
        [NotMapped]
        public string RuleName 
        { 
            get => Name; 
            set => Name = value; 
        }

        [NotMapped]
        public string Description 
        { 
            get => Condition; 
            set => Condition = value; 
        }

        [NotMapped]
        public string TriggerCondition 
        { 
            get => Condition; 
            set => Condition = value; 
        }

        [NotMapped]
        public string ActionCommand 
        { 
            get => Action; 
            set => Action = value; 
        }

        [NotMapped]
        public int PriorityLevel 
        { 
            get => Priority; 
            set => Priority = value; 
        }

        [NotMapped]
        public string? ConditionsJson 
        { 
            get => SerializedConditions; 
            set => SerializedConditions = value; 
        }

        [NotMapped]
        public string? ActionsJson 
        { 
            get => SerializedActions; 
            set => SerializedActions = value; 
        }

        [NotMapped]
        public DateTime? LastExecutionTimestamp 
        { 
            get => LastExecuted; 
            set => LastExecuted = value; 
        }

        // Phase 4: Proactive AI Properties (Aliased to JSON or new logic)
        [NotMapped]
        public string TriggerValue { get; set; } = "80"; // Default threshold
        
        [NotMapped]
        public string TriggerSource 
        { 
            get => SmartHomeDeviceId?.ToString() ?? "0"; 
            set { if (int.TryParse(value, out int id)) SmartHomeDeviceId = id; }
        }

        [NotMapped]
        public string ActionType 
        { 
            get => Action.Contains("Notify") ? "Notify" : "Control";
            set { /* No-op or update Action string */ }
        }
    }
}
