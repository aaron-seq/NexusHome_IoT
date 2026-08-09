using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexusHome.IoT.Core.Domain;

namespace NexusHome.IoT.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework context for the NexusHome smart home platform.
    /// </summary>
    public class SmartHomeDbContext : DbContext
    {
        public SmartHomeDbContext(DbContextOptions<SmartHomeDbContext> options) : base(options)
        {
        }

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

            modelBuilder.Entity<User>(entity =>
            {
                // Logins look users up by name, and duplicates would make the
                // lookup ambiguous.
                entity.HasIndex(user => user.Username).IsUnique();
                entity.HasIndex(user => user.Email).IsUnique();
            });

            modelBuilder.Entity<SmartHomeDevice>(entity =>
            {
                entity.HasIndex(device => device.UniqueDeviceIdentifier).IsUnique();
            });

            // Energy and alert queries are almost always time-ranged per device,
            // so index the pair rather than the columns separately.
            modelBuilder.Entity<DeviceEnergyConsumption>(entity =>
            {
                entity.HasIndex(reading => new { reading.SmartHomeDeviceId, reading.MeasurementTimestamp });
            });

            modelBuilder.Entity<DeviceAlert>(entity =>
            {
                entity.HasIndex(alert => new { alert.SmartHomeDeviceId, alert.CreatedAt });
            });

            ConfigureSqliteDecimalSupport(modelBuilder);
        }

        /// <summary>
        /// SQLite has no native decimal type and refuses to translate aggregate
        /// operators such as Sum over decimal columns. Storing them as double
        /// keeps the local development provider usable while SQL Server keeps
        /// exact decimal semantics.
        /// </summary>
        private void ConfigureSqliteDecimalSupport(ModelBuilder modelBuilder)
        {
            if (!Database.IsSqlite())
            {
                return;
            }

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(decimal))
                    {
                        property.SetValueConverter(new ValueConverter<decimal, double>(
                            value => (double)value,
                            value => (decimal)value));
                    }
                    else if (property.ClrType == typeof(decimal?))
                    {
                        property.SetValueConverter(new ValueConverter<decimal?, double?>(
                            value => value.HasValue ? (double)value.Value : null,
                            value => value.HasValue ? (decimal)value.Value : null));
                    }
                }
            }
        }
    }
}
