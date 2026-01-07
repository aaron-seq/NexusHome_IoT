# NexusHome IoT - Developer Guide

## Overview
This guide covers local development setup, testing workflows, and debugging tips for the NexusHome IoT Platform.

---

## Prerequisites

- **.NET 8.0 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Docker Desktop**: [Download](https://www.docker.com/products/docker-desktop)
- **SQL Server**: Local or Docker container
- **IDE**: Visual Studio 2022, VS Code with C# extension, or JetBrains Rider
- **Git**: Version control

---

## Quick Start

### 1. Clone and Setup
```bash
git clone https://github.com/aaron-seq/NexusHome_IoT.git
cd NexusHome_IoT

# Restore dependencies
dotnet restore

# Build the project
dotnet build
```

### 2. Start Dependencies with Docker
```bash
# Start SQL Server, Redis, and MQTT broker
docker-compose up -d sqlserver redis mqtt-broker

# Verify services are healthy
docker-compose ps
```

### 3. Run the Application
```bash
# Development mode
dotnet run

# Access the application
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
# Health: http://localhost:5000/health
```

---

## Project Structure

```
NexusHome_IoT/
├── Controllers/           # API endpoints (DeviceController, EnergyController)
├── Core/
│   ├── Domain/           # Entity models (SmartDevice, User, etc.)
│   └── Services/         # Business logic (EnergyOptimizationService, etc.)
├── Infrastructure/
│   ├── Configuration/    # Settings models (MqttBrokerSettings, etc.)
│   ├── Data/             # EF Core DbContext
│   └── Services/         # External integrations (MQTT, Weather API)
├── Application/
│   ├── DTOs/             # Data Transfer Objects
│   ├── Hubs/             # SignalR real-time hubs
│   └── Validators/       # FluentValidation validators
├── Tests/
│   ├── Unit/             # Unit tests
│   └── Integration/      # API integration tests
└── docs/                  # Documentation
```

---

## Running Tests

### All Tests
```bash
dotnet test
```

### Unit Tests Only
```bash
dotnet test --filter "Category=Unit"
```

### Integration Tests Only
```bash
dotnet test --filter "Category=Integration"
```

### With Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

---

## Docker Development Workflow

### Full Stack (All Services)
```bash
# Build and start everything
docker-compose up -d

# View logs
docker-compose logs -f nexushome-app

# Stop all services
docker-compose down
```

### Build Only the Application
```bash
docker build -t nexushome-iot:dev .
```

---

## Configuration

### Environment Variables
Create a `.env` file in the project root:
```bash
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Server=localhost;Database=NexusHomeIoT;...
MqttBroker__Host=localhost
MqttBroker__Port=1883
JwtAuthentication__SecretKey=your-secret-key-min-32-chars
```

### appsettings.Development.json
For local development, create this file with your settings.

---

## Debugging Tips

### SignalR Hub Connections
1. Open browser DevTools (F12)
2. Go to Network tab, filter by "WS"
3. Look for connections to `/hubs/deviceStatus`

### MQTT Message Debugging
```bash
# Subscribe to all topics
docker exec nexushome-mqtt mosquitto_sub -h localhost -t "#" -v
```

### Database Queries
Enable EF Core logging in `appsettings.Development.json`:
```json
"Logging": {
  "LogLevel": {
    "Microsoft.EntityFrameworkCore.Database.Command": "Information"
  }
}
```

---

## Code Quality

### Format Check
```bash
dotnet format --verify-no-changes
```

### Apply Formatting
```bash
dotnet format
```

---

## Common Tasks

### Add a New Device Type
1. Add enum value to `DeviceCategory` in `Core/Domain/Models.cs`
2. Update `SmartDeviceManager` for type-specific logic
3. Add tests in `Tests/Unit/Services/`

### Add a New API Endpoint
1. Add method to appropriate controller in `Controllers/`
2. Add DTO in `Application/DTOs/`
3. Add validator in `Application/Validators/`
4. Add integration test in `Tests/Integration/Controllers/`

---

## Need Help?
- Check [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for common issues
- Review [ARCHITECTURE.md](ARCHITECTURE.md) for system design
- See [CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines
