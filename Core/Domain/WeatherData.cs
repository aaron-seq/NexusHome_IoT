using System;
using System.ComponentModel.DataAnnotations;

namespace NexusHome.IoT.Core.Domain
{
    public class WeatherData : BaseEntity
    {
        public string Location { get; set; } = string.Empty;
        public decimal Temperature { get; set; }
        public decimal Humidity { get; set; }
        public string Condition { get; set; } = string.Empty;
    }
}
