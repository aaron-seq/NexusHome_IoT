# NexusHome IoT Platform - Architecture Documentation

## Overview

NexusHome IoT is a comprehensive smart home energy management platform built on .NET 8.0, leveraging Clean Architecture principles to ensure maintainability, testability, and scalability.

---

## System Context (C4 Level 1)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              External Systems                                │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │   Weather    │  │   Utility    │  │    Azure     │  │   Mobile     │    │
│  │     API      │  │   Provider   │  │   IoT Hub    │  │   App Users  │    │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘    │
│         │                 │                 │                 │            │
└─────────┼─────────────────┼─────────────────┼─────────────────┼────────────┘
          │                 │                 │                 │
          ▼                 ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        NexusHome IoT Platform                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         ASP.NET Core API                            │   │
│  │    ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐   │   │
│  │    │ REST API    │  │ SignalR     │  │ Background Services     │   │   │
│  │    │ Controllers │  │ Hubs        │  │ (MQTT, Energy, ML)      │   │   │
│  │    └─────────────┘  └─────────────┘  └─────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
          │                 │                 │
          ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              IoT Devices                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │   Smart      │  │   Climate    │  │   Security   │  │   Energy     │    │
│  │   Lighting   │  │   Control    │  │   Devices    │  │   Meters     │    │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Container Diagram (C4 Level 2)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        NexusHome IoT Platform                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    API Layer (Port 5000)                             │  │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────────────┐ │  │
│  │  │ Device     │ │ Energy     │ │ Automation │ │ SignalR Hubs       │ │  │
│  │  │ Controller │ │ Controller │ │ Controller │ │ - DeviceStatus     │ │  │
│  │  │            │ │            │ │            │ │ - EnergyMonitoring │ │  │
│  │  │            │ │            │ │            │ │ - Notifications    │ │  │
│  │  └────────────┘ └────────────┘ └────────────┘ └────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                    │                                        │
│                                    ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                    Core Services Layer                               │  │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────┐ │  │
│  │  │ SmartDevice     │ │ EnergyOptim-    │ │ PredictiveMaintenance  │ │  │
│  │  │ Manager         │ │ izationService  │ │ Service                │ │  │
│  │  └─────────────────┘ └─────────────────┘ └─────────────────────────┘ │  │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────┐ │  │
│  │  │ Automation      │ │ EnergyConsump-  │ │ NotificationDispatcher │ │  │
│  │  │ RuleEngine      │ │ tionAnalyzer    │ │                        │ │  │
│  │  └─────────────────┘ └─────────────────┘ └─────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                    │                                        │
│                                    ▼                                        │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                 Infrastructure Layer                                 │  │
│  │  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────┐ │  │
│  │  │ EnhancedMqtt    │ │ SmartHomeDb     │ │ OpenWeatherMap         │ │  │
│  │  │ ClientService   │ │ Context (EF)    │ │ Provider               │ │  │
│  │  └─────────────────┘ └─────────────────┘ └─────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
          │                         │                         │
          ▼                         ▼                         ▼
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│   MQTT Broker    │    │   SQL Server     │    │   Redis Cache    │
│  (Mosquitto)     │    │   (Primary DB)   │    │  (Session/Cache) │
│   Port 1883      │    │   Port 1433      │    │   Port 6379      │
└──────────────────┘    └──────────────────┘    └──────────────────┘
```

---

## Layer Architecture

### API Layer (`Controllers/`, `Application/Hubs/`)
- **Responsibility**: HTTP request handling, SignalR real-time communication
- **Pattern**: Thin controllers delegating to services
- **Dependencies**: Depends on Core Services layer only

### Core Layer (`Core/Services/`, `Core/Domain/`)
- **Responsibility**: Business logic, domain entities, service interfaces
- **Pattern**: Domain-Driven Design with service layer
- **Dependencies**: No external dependencies, defines interfaces

### Infrastructure Layer (`Infrastructure/`)
- **Responsibility**: External integrations, data access, MQTT communication
- **Pattern**: Repository pattern, adapter pattern for external services
- **Dependencies**: Implements Core interfaces, uses external packages

---

## Key Data Flows

### Device Telemetry Flow
```
IoT Device → MQTT Broker → MqttConnectionService → DeviceDataCollectionService
    → SmartDeviceManager → SmartHomeDbContext → SignalR Hub → Web Dashboard
```

### Energy Optimization Flow
```
EnergyMonitoringBackgroundService → EnergyConsumptionAnalyzer
    → EnergyOptimizationService (ML.NET) → AutomationRuleEngine
    → SmartDeviceManager → MQTT Command → IoT Device
```

### Predictive Maintenance Flow
```
DeviceTelemetryReading → PredictiveMaintenanceBackgroundService
    → PredictiveMaintenanceService (ML.NET Model)
    → MaintenanceSchedulingService → NotificationDispatcher
```

---

## Technology Stack Summary

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Framework | .NET 8.0 | Core runtime |
| Web API | ASP.NET Core | HTTP endpoints |
| Real-time | SignalR | WebSocket communication |
| ORM | EF Core 8.0 | SQL Server access |
| IoT Protocol | MQTTnet 4.3 | Device communication |
| ML/AI | ML.NET 3.0 | Predictive analytics |
| Caching | Redis | Session and data cache |
| Logging | Serilog | Structured logging |
| Containers | Docker | Deployment |

---

## Security Architecture

- **Authentication**: JWT Bearer tokens with configurable expiration
- **Authorization**: Role-based access control (Admin, User, Device)
- **Transport**: TLS 1.2+ for all external communication
- **Secrets**: Environment variables for production secrets
- **Rate Limiting**: AspNetCoreRateLimit for API protection

---

## Deployment Architecture

See [docker-compose.yml](../docker-compose.yml) for container orchestration with:
- Health checks on all services
- Volume persistence for data durability
- Network isolation via bridge network
- Environment-based configuration
