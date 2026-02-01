using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusHome.IoT.Core.Domain
{
    public class WeatherData : BaseEntity
    {
        public string Location { get; set; } = string.Empty;
        public decimal Temperature { get; set; }
        public decimal Humidity { get; set; }
        public string Condition { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Backward-compatible alias property
        [NotMapped]
        public DateTime Timestamp 
        { 
            get => RecordedAt; 
            set => RecordedAt = value; 
        }
    }
}
