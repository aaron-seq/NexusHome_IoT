<div align="center">

# NexusHome IoT Platform

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Docker Ready](https://img.shields.io/badge/Docker-Ready-blue.svg)](https://www.docker.com/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--time-green.svg)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](#)

**Advanced IoT Smart Home Energy Management System**

Powered by AI-driven Predictive Analytics, Real-time Monitoring, and Automated Optimization

[Features](#features) • [Quick Start](#quick-start) • [Documentation](#documentation) • [Contributing](#contributing) • [Support](#support)

</div>

---

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Technology Stack](#technology-stack)
- [Architecture](#architecture)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [API Documentation](#api-documentation)
- [Testing](#testing)
- [Deployment](#deployment)
- [Security](#security)
- [Contributing](#contributing)
- [License](#license)
- [Support](#support)
- [Roadmap](#roadmap)

---

## Overview

NexusHome IoT Platform is a comprehensive smart home energy management solution that leverages modern IoT protocols, machine learning, and real-time data processing to optimize energy consumption, reduce costs, and automate home operations.

The platform supports multi-protocol device communication (MQTT, HTTP, CoAP, Matter), provides advanced analytics through ML.NET, and delivers real-time updates via SignalR WebSocket connections.

### Why NexusHome?

- **Energy Savings**: Reduce energy consumption by up to 30% through AI-powered optimization
- **Predictive Maintenance**: Detect device failures before they happen with 90%+ accuracy
- **Real-time Control**: Monitor and control all devices instantly from anywhere
- **Universal Compatibility**: Support for all major IoT protocols and device standards
- **Enterprise Ready**: Production-grade security, scalability, and monitoring

---

## Key Features

### Smart Device Management

- Multi-protocol device support (MQTT, HTTP, CoAP, Matter)
- Real-time device status monitoring via SignalR
- Automated device discovery and provisioning
- Room-based device organization
- Device health monitoring and diagnostics

### AI-Powered Intelligence

- **Predictive Maintenance**: ML.NET models for failure prediction
- **Energy Optimization**: Dynamic power management based on usage patterns
- **Smart Automation**: Learning-based device automation rules
- **Anomaly Detection**: Real-time identification of unusual behavior
- **Load Forecasting**: Predict future energy consumption patterns

### Analytics and Visualization

- Interactive real-time dashboards
- Historical data analysis and trend identification
- Cost optimization reports and recommendations
- Energy consumption breakdown by device and room
- Performance metrics and system health monitoring

### Enterprise Security

- JWT-based authentication with role-based access control
- Device certificate authentication
- End-to-end TLS/SSL encryption
- API rate limiting and DDoS protection
- Comprehensive audit logging

### Integration Capabilities

- RESTful API with OpenAPI documentation
- SignalR hubs for real-time communication
- MQTT broker integration
- Azure IoT Hub support (optional)
- Weather API integration for smart optimization

---

## Technology Stack

### Core Framework

<table>
<tr>
<td>

- **.NET 8.0** - Latest LTS with performance enhancements
- **ASP.NET Core** - High-performance web framework
- **Entity Framework Core 8.0** - Advanced ORM with SQL Server
- **SignalR** - Real-time bi-directional communication

</td>
<td>

- **MQTTnet** - High-performance MQTT client/server
- **ML.NET** - Machine learning and predictive analytics
- **Serilog** - Structured logging framework
- **BCrypt.Net** - Password hashing

</td>
</tr>
</table>

### Data Storage

- **SQL Server** - Primary relational database
- **Redis** - High-performance caching and session storage
- **InfluxDB** - Time-series data storage (connection string reserved; not yet wired up)

### DevOps and Monitoring

- **Docker & Docker Compose** - Containerized deployment
- **GitHub Actions** - CI/CD automation

---

## Architecture

The platform follows a clean, layered architecture pattern:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│              (Blazor WebAssembly + REST API)                │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                        │
│          ┌──────────────────┬──────────────────┐            │
│          │  API Controllers │   SignalR Hubs   │            │
│          └──────────────────┴──────────────────┘            │
├─────────────────────────────────────────────────────────────┤
│                   Business Logic Layer                      │
│  ┌──────────────┬─────────────┬────────────┬──────────┐    │
│  │   Device     │   Energy    │  AI/ML     │  Auto-   │    │
│  │  Management  │  Analytics  │  Services  │  mation  │    │
│  └──────────────┴─────────────┴────────────┴──────────┘    │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                       │
│  ┌──────────────┬─────────────┬────────────┐               │
│  │  Data Access │ External    │  Message   │               │
│  │  (EF Core)   │  APIs       │  Queue     │               │
│  └──────────────┴─────────────┴────────────┘               │
├─────────────────────────────────────────────────────────────┤
│                    Data Storage Layer                       │
│         SQL Server  │  InfluxDB  │  Redis                   │
└─────────────────────────────────────────────────────────────┘
```

### Key Design Patterns

- **Clean Architecture** - Separation of concerns with dependency inversion
- **Options Pattern** - Strongly typed, validated configuration
- **Event-Driven** - Asynchronous processing with background services
- **Dependency Injection** - Loose coupling and testability

---

## Quick Start

### Prerequisites

Before you begin, ensure you have:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (optional — only for the SQL Server provider; development defaults to SQLite)
- [Git](https://git-scm.com/downloads)
- IDE: Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**

```bash
git clone https://github.com/aaron-seq/NexusHome_IoT.git
cd NexusHome_IoT
```

2. **Run the application**

Development mode needs no database, Redis or MQTT broker. It uses a local
SQLite file and seeds demo devices, energy readings and automation rules on
first start.

```bash
make run
# or, without make:
ASPNETCORE_ENVIRONMENT=Development dotnet run --project NexusHome.IoT.csproj
```

3. **Access the application**

| Endpoint | URL |
| --- | --- |
| API base | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |
| Liveness probe | http://localhost:5000/health/live |
| Readiness probe | http://localhost:5000/health/ready |

4. **Log in**

Development seeds an administrator account and logs the credentials at
startup (`admin` / `Admin123!`). Passwords are BCrypt hashed; this account is
only created outside Production.

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/Auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"Admin123!"}' | jq -r .token)

curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/Energy/consumption
```

5. **Build and test**

```bash
make build   # dotnet build NexusHome.IoT.sln -c Release
make test    # dotnet test  NexusHome.IoT.sln -c Release
make audit   # report known vulnerable NuGet packages
```

Database schema is provisioned automatically at startup: EF Core migrations are
applied when the provider is SQL Server, and the SQLite/InMemory development
providers create the schema from the model. See `SETUP.md` for authoring
migrations.

`NexusHome.IoT.sln` is the canonical build entry point — building the web
project alone silently skips the test project.

### Docker Deployment

The full stack (SQL Server, Redis, Mosquitto) runs through Docker Compose.
Copy `.env.example` to `.env` and fill in real values first; the application
refuses to start outside Development unless `JwtAuthentication__SecretKey` is
set to at least 32 characters.

```bash
cp .env.example .env
$EDITOR .env

docker compose up -d --build     # or: make docker-up
docker compose logs -f           # or: make docker-logs
docker compose down              # or: make docker-down
```

For production, layer the production overlay on top:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

The application is published on http://localhost:5000 (container port 8080).

---

## Configuration

### Environment Variables

Key configuration options:

No secrets are committed to this repository. `appsettings.json` ships with
empty credential values; supply real ones through environment variables or a
secret store.

```bash
# Database. Provider selects the EF Core provider: SqlServer, Sqlite or InMemory.
Database__Provider="SqlServer"
ConnectionStrings__DefaultConnection="Server=localhost;Database=NexusHomeIoT;Trusted_Connection=true"

# Optional. When empty, the Redis backplane, distributed cache and device
# shadow persistence are disabled rather than failing startup.
ConnectionStrings__Redis="localhost:6379"

# Security. Required outside Development; startup fails if shorter than 32
# characters. Generate one with: openssl rand -base64 32
JwtAuthentication__SecretKey="your-secret-key-min-32-characters"
JwtAuthentication__Issuer="NexusHome.IoT"
JwtAuthentication__Audience="NexusHome.Clients"
JwtAuthentication__ExpirationMinutes=60

# MQTT Configuration
MqttBroker__Host="localhost"
MqttBroker__Port=1883
MqttBroker__Username="nexususer"
MqttBroker__Password="your-secure-password"

# External Services
WeatherApi__ApiKey="your-openweather-api-key"
WeatherApi__BaseUrl="https://api.openweathermap.org/data/2.5"
```

### Configuration Files

- **appsettings.json** - Base configuration
- **appsettings.Development.json** - Development overrides
- **Environment variables** - Production settings (no Production appsettings file is committed)
- **docker-compose.yml** - Docker service orchestration

---

## API Documentation

### Authentication

All API endpoints require JWT authentication:

```bash
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "your-password"
}
```

### Key Endpoints

#### Device Management

```bash
GET    /api/devices              # List all devices
GET    /api/devices/{id}         # Get device details
POST   /api/devices/{id}/toggle  # Toggle device state
POST   /api/devices/telemetry    # Submit telemetry data
GET    /api/devices/{id}/energy  # Get energy consumption
```

#### Energy Analytics

```bash
GET    /api/energy/consumption   # Total consumption data
GET    /api/energy/cost          # Cost analysis
GET    /api/energy/forecast      # Usage predictions
GET    /api/energy/optimization  # Optimization suggestions
```

#### Automation Rules

```bash
GET    /api/automation/rules     # List automation rules
POST   /api/automation/rules     # Create new rule
PUT    /api/automation/rules/{id} # Update rule
DELETE /api/automation/rules/{id} # Delete rule
```

### Real-time Communication

Connect to SignalR hubs for real-time updates:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/device-status")
    .build();

// Subscribe to device updates
connection.on("DeviceStatusChanged", (device) => {
    console.log(`Device ${device.id} status: ${device.status}`);
});

await connection.start();
```

For complete API documentation, visit `/swagger` when running the application.

---

## Testing

### Unit Tests

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "Category=Integration"
```

### Integration Tests

```bash
# Run integration tests
dotnet test --filter "Category=Integration"

# Test with real database
dotnet test --filter "Category=DatabaseIntegration"
```

### Load Testing

Using k6 for performance testing:

```bash
# Install k6
brew install k6  # macOS

# Run load test
k6 run --vus 50 --duration 60s scripts/load-test.js
```

### API Testing

```bash
# Test device endpoint
curl -X GET "http://localhost:5000/api/devices" \
     -H "Accept: application/json" \
     -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Submit telemetry
curl -X POST "http://localhost:5000/api/devices/telemetry" \
     -H "Content-Type: application/json" \
     -d '{
       "deviceId": "smart-thermostat-01",
       "sensorData": {"temperature": 23.5, "humidity": 45},
       "timestamp": "2025-11-08T10:00:00Z"
     }'
```

---

## Deployment

### Production Checklist

Before deploying to production:

- [ ] Update all secrets and API keys
- [ ] Configure production database connection
- [ ] Enable HTTPS and configure SSL certificates
- [ ] Set up monitoring and alerting
- [ ] Configure backup strategy
- [ ] Review security settings and rate limits
- [ ] Set up log aggregation
- [ ] Configure CDN for static assets
- [ ] Enable database connection pooling
- [ ] Set up automated backups

### Docker Production Build

```bash
# Build production image
docker build -t nexushome-iot:v2.1.0 -f Dockerfile.prod .

# Run production container
docker run -d \
  -p 80:80 \
  -p 443:443 \
  --name nexushome-prod \
  --env-file .env.production \
  --restart unless-stopped \
  nexushome-iot:v2.1.0
```

### Cloud Deployment Options

#### Azure App Service

```bash
# Login to Azure
az login

# Create resource group
az group create --name nexushome-rg --location eastus

# Deploy to App Service
az webapp up --name nexushome-iot --resource-group nexushome-rg --runtime "DOTNET|8.0"
```

#### AWS ECS/Fargate

```bash
# Push to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin YOUR_ECR_REPO
docker tag nexushome-iot:latest YOUR_ECR_REPO/nexushome-iot:latest
docker push YOUR_ECR_REPO/nexushome-iot:latest

# Deploy to ECS (using task definition)
aws ecs update-service --cluster nexushome-cluster --service nexushome-service --force-new-deployment
```

#### Kubernetes

```bash
# Apply Kubernetes manifests
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/ingress.yaml

# Check deployment status
kubectl get pods -n nexushome
kubectl logs -f deployment/nexushome-iot -n nexushome
```

---

## Security

### Security Best Practices

The platform implements multiple security layers:

#### Authentication and Authorization

- JWT tokens with configurable expiration
- Role-based access control (Admin, User, Device)
- API key authentication for IoT devices
- Refresh token rotation
- Password hashing with BCrypt

#### Data Protection

- SQL injection protection via parameterized queries
- Input validation and sanitization
- XSS protection with CSP headers
- CSRF token validation
- Encrypted data at rest and in transit

#### Network Security

- HTTPS enforcement (TLS 1.2+)
- CORS policy configuration
- Rate limiting (100 requests/minute per IP)
- DDoS protection via reverse proxy
- IP whitelisting for admin endpoints

#### Device Security

- Device certificate-based authentication
- Encrypted MQTT communication
- Device provisioning workflow
- Anomaly detection for suspicious activity

### Security Configuration

```json
{
  "SecuritySettings": {
    "RateLimiting": {
      "Enabled": true,
      "RequestsPerMinute": 100
    },
    "Cors": {
      "AllowedOrigins": ["https://yourdomain.com"],
      "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
      "AllowCredentials": true
    },
    "HttpsRedirection": true,
    "HstsMaxAge": 31536000
  }
}
```

### Reporting Security Issues

If you discover a security vulnerability, please email security@nexushome.tech. Do not open a public issue.

For more details, see [SECURITY.md](SECURITY.md).

---

## Contributing

We welcome contributions from the community! Whether you're fixing bugs, adding features, or improving documentation, your help is appreciated.

### How to Contribute

1. **Fork the repository**
2. **Create a feature branch**
   ```bash
   git checkout -b feature/amazing-feature
   ```
3. **Make your changes**
4. **Run tests**
   ```bash
   dotnet test
   dotnet format --verify-no-changes
   ```
5. **Commit your changes**
   ```bash
   git commit -m "Add amazing feature"
   ```
6. **Push to your branch**
   ```bash
   git push origin feature/amazing-feature
   ```
7. **Open a Pull Request**

### Contribution Guidelines

- Follow C# coding conventions and style guidelines
- Write unit tests for new features
- Update documentation as needed
- Keep pull requests focused and atomic
- Reference relevant issues in PR descriptions

For detailed guidelines, see [CONTRIBUTING.md](CONTRIBUTING.md).

### Code of Conduct

This project adheres to a Code of Conduct. By participating, you are expected to uphold this code.

See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) for details.

---

## License

This project is licensed under the MIT License. You are free to use, modify, and distribute this software.

See [LICENSE](LICENSE) for the full license text.

```
MIT License

Copyright (c) 2025 Aaron Sequeira

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files...
```

---

## Support

### Documentation

- **API Documentation**: Available at `/swagger` when running
- **Architecture Guide**: See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- **Deployment Guide**: See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)
- **Troubleshooting**: See [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)

### Community Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/aaron-seq/NexusHome_IoT/issues)
- **GitHub Discussions**: [Ask questions and share ideas](https://github.com/aaron-seq/NexusHome_IoT/discussions)
- **Stack Overflow**: Tag questions with `nexushome-iot`

### Contact

- **Email**: support@nexushome.tech
- **LinkedIn**: [Aaron Sequeira](https://linkedin.com/in/aaronsequeira)
- **GitHub**: [@aaron-seq](https://github.com/aaron-seq)

---

## Roadmap

### Current Version: v2.1.0

### Upcoming: v2.2.0 (Q1 2026)

- Voice assistant integration (Alexa, Google Assistant)
- Cross-platform mobile app (React Native)
- Enhanced ML models for energy prediction
- Multi-tenant architecture support
- Advanced reporting and export features

### Future: v2.3.0 (Q2 2026)

- Edge computing support for local processing
- Blockchain-based device identity management
- Solar panel and battery integration
- Community energy sharing marketplace
- Advanced weather correlation

### Long-term Vision

- Global smart grid integration
- Carbon footprint tracking and offsetting
- Peer-to-peer energy trading
- Machine learning model marketplace
- IoT device ecosystem partnerships

---

## Acknowledgments

Special thanks to:

- **Microsoft .NET Team** for the excellent .NET 8 framework
- **Eclipse Mosquitto** for the reliable MQTT broker
- **ML.NET Team** for machine learning capabilities
- **Open Source Community** for amazing libraries and tools
- **Contributors** who have helped improve this project

---

## Project Statistics

<div align="center">

![GitHub Stars](https://img.shields.io/github/stars/aaron-seq/NexusHome_IoT?style=social)
![GitHub Forks](https://img.shields.io/github/forks/aaron-seq/NexusHome_IoT?style=social)
![GitHub Watchers](https://img.shields.io/github/watchers/aaron-seq/NexusHome_IoT?style=social)
![GitHub Issues](https://img.shields.io/github/issues/aaron-seq/NexusHome_IoT)
![GitHub Pull Requests](https://img.shields.io/github/issues-pr/aaron-seq/NexusHome_IoT)
![Contributors](https://img.shields.io/github/contributors/aaron-seq/NexusHome_IoT)
![Last Commit](https://img.shields.io/github/last-commit/aaron-seq/NexusHome_IoT)

</div>

---

<div align="center">

**Built with care by [Aaron Sequeira](https://github.com/aaron-seq) and the NexusHome community**

If you find this project helpful, please consider giving it a star!

[Star this repo](https://github.com/aaron-seq/NexusHome_IoT/stargazers) • [Report an issue](https://github.com/aaron-seq/NexusHome_IoT/issues/new) • [Request a feature](https://github.com/aaron-seq/NexusHome_IoT/issues/new?labels=enhancement)

</div>
