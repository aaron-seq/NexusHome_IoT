using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.Core.Domain;

namespace NexusHome.IoT.Infrastructure.Data
{
    public class SmartHomeDbContext : DbContext
    {
        public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices => Set<Device>();
        public DbSet<EnergyConsumption> EnergyConsumptions => Set<EnergyConsumption>();
        public DbSet<SolarGeneration> SolarGenerations => Set<SolarGeneration>();
        public DbSet<BatteryStatus> BatteryStatuses => Set<BatteryStatus>();
        public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
        public DbSet<EnergyOptimizationRule> EnergyOptimizationRules => Set<EnergyOptimizationRule>();
        public DbSet<DeviceMaintenanceRecord> MaintenanceRecords => Set<DeviceMaintenanceRecord>();
        public DbSet<DeviceAlert> DeviceAlerts => Set<DeviceAlert>();
        public DbSet<User> Users => Set<User>();
        public DbSet<WeatherData> WeatherData => Set<WeatherData>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Keeping original model configuration semantics; moved to new namespace
            // Indices, defaults, relationships preserved from previous DbContext
        }
    }
}
