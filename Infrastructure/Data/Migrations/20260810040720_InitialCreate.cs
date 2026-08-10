using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusHome.IoT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnergyOptimizationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RuleType = table.Column<int>(type: "int", nullable: false),
                    PriorityLevel = table.Column<int>(type: "int", nullable: false),
                    TargetSavingsPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ComfortImpactThreshold = table.Column<int>(type: "int", nullable: false),
                    ConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAccumulatedSavings = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergyOptimizationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmartDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniqueDeviceIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceFriendlyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeviceDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeviceType = table.Column<int>(type: "int", nullable: false),
                    ManufacturerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirmwareVersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConnectionProtocol = table.Column<int>(type: "int", nullable: false),
                    CurrentStatus = table.Column<int>(type: "int", nullable: false),
                    PhysicalLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RoomAssignment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaximumPowerRating = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CurrentPowerConsumption = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    IsCurrentlyOnline = table.Column<bool>(type: "bit", nullable: false),
                    LastCommunicationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MqttTopicPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DeviceConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatingTemperature = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    BatteryLevel = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SecurityStatus = table.Column<int>(type: "int", nullable: false),
                    NetworkSignalStrength = table.Column<int>(type: "int", nullable: false),
                    NextScheduledMaintenance = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Preferences = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Temperature = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Humidity = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SmartHomeDeviceId = table.Column<int>(type: "int", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastExecuted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutionCount = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    SerializedConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerializedActions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationRules_SmartDevices_SmartHomeDeviceId",
                        column: x => x.SmartHomeDeviceId,
                        principalTable: "SmartDevices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BatteryStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmartHomeDeviceId = table.Column<int>(type: "int", nullable: false),
                    ChargeLevelPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    VoltageReading = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    CurrentReading = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    TemperatureCelsius = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    OperationMode = table.Column<int>(type: "int", nullable: false),
                    StateOfHealthPercentage = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    CycleCount = table.Column<int>(type: "int", nullable: false),
                    MeasurementTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatteryStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatteryStatuses_SmartDevices_SmartHomeDeviceId",
                        column: x => x.SmartHomeDeviceId,
                        principalTable: "SmartDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmartHomeDeviceId = table.Column<int>(type: "int", nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceAlerts_SmartDevices_SmartHomeDeviceId",
                        column: x => x.SmartHomeDeviceId,
                        principalTable: "SmartDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceTelemetryReading",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmartHomeDeviceId = table.Column<int>(type: "int", nullable: false),
                    ReadingType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTelemetryReading", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceTelemetryReading_SmartDevices_SmartHomeDeviceId",
                        column: x => x.SmartHomeDeviceId,
                        principalTable: "SmartDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnergyConsumptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmartHomeDeviceId = table.Column<int>(type: "int", nullable: false),
                    PowerConsumptionKilowattHours = table.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    VoltageReading = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    CurrentReading = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    PowerFactorReading = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    FrequencyReading = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    CalculatedCostAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    MeasurementTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnergySource = table.Column<int>(type: "int", nullable: false),
                    AppliedTariffRate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CarbonFootprintGrams = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    PowerQualityIndicator = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergyConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnergyConsumptions_SmartDevices_SmartHomeDeviceId",
                        column: x => x.SmartHomeDeviceId,
                        principalTable: "SmartDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmartHomeDeviceId = table.Column<int>(type: "int", nullable: false),
                    MaintenanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaintenanceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_SmartDevices_SmartHomeDeviceId",
                        column: x => x.SmartHomeDeviceId,
                        principalTable: "SmartDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolarGenerations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmartHomeDeviceId = table.Column<int>(type: "int", nullable: false),
                    PowerGeneratedKilowatts = table.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    EfficiencyPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MeasurementTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TemperatureCelsius = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    IrradianceWattsPerSquareMeter = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolarGenerations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolarGenerations_SmartDevices_SmartHomeDeviceId",
                        column: x => x.SmartHomeDeviceId,
                        principalTable: "SmartDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRules_SmartHomeDeviceId",
                table: "AutomationRules",
                column: "SmartHomeDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_BatteryStatuses_SmartHomeDeviceId",
                table: "BatteryStatuses",
                column: "SmartHomeDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAlerts_SmartHomeDeviceId_CreatedAt",
                table: "DeviceAlerts",
                columns: new[] { "SmartHomeDeviceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTelemetryReading_SmartHomeDeviceId",
                table: "DeviceTelemetryReading",
                column: "SmartHomeDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_EnergyConsumptions_SmartHomeDeviceId_MeasurementTimestamp",
                table: "EnergyConsumptions",
                columns: new[] { "SmartHomeDeviceId", "MeasurementTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_SmartHomeDeviceId",
                table: "MaintenanceRecords",
                column: "SmartHomeDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartDevices_UniqueDeviceIdentifier",
                table: "SmartDevices",
                column: "UniqueDeviceIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolarGenerations_SmartHomeDeviceId",
                table: "SolarGenerations",
                column: "SmartHomeDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherData_RecordedAt",
                table: "WeatherData",
                column: "RecordedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomationRules");

            migrationBuilder.DropTable(
                name: "BatteryStatuses");

            migrationBuilder.DropTable(
                name: "DeviceAlerts");

            migrationBuilder.DropTable(
                name: "DeviceTelemetryReading");

            migrationBuilder.DropTable(
                name: "EnergyConsumptions");

            migrationBuilder.DropTable(
                name: "EnergyOptimizationRules");

            migrationBuilder.DropTable(
                name: "MaintenanceRecords");

            migrationBuilder.DropTable(
                name: "SolarGenerations");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "WeatherData");

            migrationBuilder.DropTable(
                name: "SmartDevices");
        }
    }
}
