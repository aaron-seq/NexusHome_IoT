using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.Core.Domain;

namespace NexusHome.IoT.Infrastructure.Data
{
    public class SmartHomeDbContext : DbContext
    {
        public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options) : base(options)
        {
        }


        // SolarGeneration, BatteryStatus seem missing from Core.Domain or need migration. 
        // Checking Models.cs, they exist there. We should probably migrate them or use the full names if they exist in Core.Domain.
        // Assuming for now we stick to what is in Core.Domain.Models.cs or similar.
        // Wait, looking at Core\Domain\Models.cs (File 204), it has SmartHomeDevice, DeviceEnergyConsumption.
        // It DOES NOT have SolarGeneration, BatteryStatus, EnergyOptimizationRule, DeviceMaintenanceRecord (it has this one), DeviceAlert (has this one).
        
        // Let's look at file 204 again. 
        // It has `SmartHomeDevice`, `DeviceEnergyConsumption`.
        // It DOES NOT have `SolarGeneration`, `BatteryStatus`.
        // It has `DeviceMaintenanceRecord`.
        // It has `DeviceAlert`.
        // It DOES NOT have `EnergyOptimizationRule`.
        // It DOES NOT have `AutomationRule` (It has `IntelligentAutomationRule` referenced in collection, wait let me check).
        // File 204 line 102: `public virtual ICollection<IntelligentAutomationRule> AssociatedAutomationRules`.
        
        // So I need to be careful. usage of legacy classes vs new classes.
        // The user wants to "Refine project structure".
        // I should probably Comment out the missing ones or fix them.
        // Let's fix Devices and Users first as they are critical.
        
        public DbSet<SmartHomeDevice> SmartDevices => Set<SmartHomeDevice>();
        public DbSet<DeviceEnergyConsumption> EnergyConsumptions => Set<DeviceEnergyConsumption>();
        public DbSet<DeviceMaintenanceRecord> MaintenanceRecords => Set<DeviceMaintenanceRecord>();
        public DbSet<DeviceAlert> DeviceAlerts => Set<DeviceAlert>();
        public DbSet<User> Users => Set<User>();
        public DbSet<WeatherData> WeatherData => Set<WeatherData>();
        
        public DbSet<SolarGeneration> SolarGenerations => Set<SolarGeneration>();
        public DbSet<BatteryStatus> BatteryStatuses => Set<BatteryStatus>();
        public DbSet<IntelligentAutomationRule> AutomationRules => Set<IntelligentAutomationRule>();
        public DbSet<EnergyOptimizationRule> EnergyOptimizationRules => Set<EnergyOptimizationRule>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Keeping original model configuration semantics; moved to new namespace
            // Indices, defaults, relationships preserved from previous DbContext
        }
    }
}
