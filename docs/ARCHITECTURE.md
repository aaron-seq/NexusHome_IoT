# Architecture

## Current structure

[NexusHome.IoT.csproj](../NexusHome.IoT.csproj) builds one ASP.NET Core assembly. Core, Application, Infrastructure, and API are folders in that assembly; the code has direct EF/ASP.NET dependencies across these folders. [ADR-001](adr/ADR-001-clean-architecture.md) records the intended layering, not enforced assembly boundaries.

| Area | Responsibility and entry points |
| --- | --- |
| API | [Program.cs](../Program.cs) wires DI, JWT policies, CORS, middleware, controllers, hubs, and hosted services. Controllers exist in both `API/Controllers/` and `Controllers/`. |
| Application | [DTOs](../Application/DTOs), [validators](../Application/Validators), and [device status hub](../Application/Hubs/SmartDeviceStatusHub.cs). |
| Core | [Domain entities](../Core/Domain), [services and interfaces](../Core/Services) for automation, energy, shadows, security, and maintenance. |
| Infrastructure | [SmartHomeDbContext](../Infrastructure/Data/SmartHomeDbContext.cs), MQTT/weather/utility services, Matter clusters, and device adapters. |
| Client | [NexusHome.Web](../NexusHome.Web/NexusHome.Web.csproj) is a separate Blazor WebAssembly app, outside the API/test solution and CI. |
| Tests | [Tests](../Tests/NexusHome.IoT.Tests.csproj) contains unit tests and HTTP integration fixtures. |

`NexusHome.WebApp` and `NexusHome.DeviceSimulator` contain source fragments without project files and are excluded from the API project. Preserve them until their owners decide whether to integrate or retire them ([#39](https://github.com/aaron-seq/NexusHome_IoT/issues/39)).

## HTTP and real-time contracts

Consult development Swagger and controller attributes for exact models and operations. The API currently has two route families:

| Controller | Base route |
| --- | --- |
| [AuthController](../API/Controllers/AuthController.cs) | `/api/v1/Auth` (`POST login`) |
| [SmartDevicesController](../API/Controllers/SmartDevicesController.cs) | `/api/v1/SmartDevices` |
| [DeviceController](../Controllers/DeviceController.cs) | `/api/Device` |
| [EnergyController](../Controllers/EnergyController.cs) | `/api/Energy` |
| [AutomationController](../Controllers/AutomationController.cs) | `/api/Automation` |
| [PredictionController](../API/Controllers/PredictionController.cs) | `/api/Prediction` |

The legacy Device/Energy/Automation controllers require `UserAccess`; do not assume every controller is authenticated. Login verifies BCrypt passwords; token/session limitations remain. [Program.cs](../Program.cs) maps `/hubs/deviceStatus`, `/hubs/energy`, and `/hubs/notifications`. Without Redis, SignalR remains local and device-shadow persistence is disabled. It also maps `/hubs/maintenance`, `/health/live` (independent of external services), and `/health/ready` (database check); see [setup details](../SETUP.md).

## Device and background work

[SmartDeviceManager](../Infrastructure/Services/SmartDeviceManager.cs) uses the EF context and protocol bridge. [UnifiedProtocolBridge](../Infrastructure/Services/UnifiedProtocolBridge.cs) selects adapters using protocol/device mappings; [UniversalDeviceBridge](../Core/Services/UniversalDeviceBridge.cs) separately updates desired shadow state and uses device-ID prefixes to choose an adapter. Both are registered. Their overlapping responsibilities need consumer/behavior evidence before consolidation.

[ADR-002](adr/ADR-002-mqtt-protocol.md) records the MQTT choice and trade-offs. [EnhancedMqttClientService](../Infrastructure/Services/EnhancedMqttClientService.cs) implements the client, [MqttConnectionService](../Infrastructure/Services/MqttConnectionService.cs) owns startup connection work, and [MqttBrokerSettings](../Infrastructure/Configuration/MqttBrokerSettings.cs) defines topic defaults. Matter handlers and adapters are implementation scaffolding, not certification or evidence of hardware interoperability.

Hosted services cover data collection, energy monitoring/optimization, automation, maintenance, and predictions. Inspect registrations and implementations before changing lifetimes or removing seemingly unused types. The separate predictive-maintenance contracts in [AI](../AI/PredictiveMaintenanceService.cs) and [Core](../Core/Services/Interfaces/IPredictiveMaintenanceService.cs) need a caller/contract decision ([#36](https://github.com/aaron-seq/NexusHome_IoT/issues/36)). Both contracts are consumed; characterize their callers before migrating them.

## Evidence limits

[UtilityPriceProvider](../Infrastructure/Services/UtilityPriceProvider.cs) supplies mock time-of-day prices, and [OpenWeatherMapProvider](../Infrastructure/Services/OpenWeatherMapProvider.cs) returns fixed sample weather. Predictive/UI paths include simulations and fallbacks. No checked-in reproducible benchmark supports historical claims about response latency, throughput, energy savings, predictive accuracy, or coverage percentages. Package references for Azure, schedulers, logging, or rate limiting do not establish active integrations; verify runtime calls in the host and services.

See [SETUP.md](../SETUP.md#containers-and-hosting) for container configuration and additional cloud-manifest gaps, [SECURITY.md](../SECURITY.md) for reporting/operational guidance, and [CHANGELOG.md](../CHANGELOG.md) for historical release notes. The changelog and ADRs retain their historical claims and decisions; this guide describes the current checkout.
