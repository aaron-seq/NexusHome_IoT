# NexusHome IoT - Local Development Setup

This guide will help you set up the NexusHome IoT platform for local development.

## Prerequisites

- .NET 8.0 SDK or later
- Docker and Docker Compose
- SQL Server (or use Docker)
- Redis (or use Docker)
- MQTT Broker (or use Docker)
- Git

### Optional Tools

- Visual Studio 2022 or VS Code
- Azure Data Studio or SQL Server Management Studio
- Redis CLI
- MQTT Explorer

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/aaron-seq/NexusHome_IoT.git
cd NexusHome_IoT
```

### 2. Configure Environment Variables

```bash
# Copy the example environment file
cp .env.example .env

# Edit .env and update the following REQUIRED values:
# - SQLSERVER_SA_PASSWORD (strong password for SQL Server)
# - REDIS_PASSWORD (password for Redis)
# - JwtAuthentication__SecretKey (generate with: openssl rand -base64 32)
# - MqttBroker__Password (password for MQTT broker)
```

**Important Security Note:** Never commit the `.env` file. It's already in `.gitignore`.

### 3. Start Infrastructure Services

```bash
# Start all services using Docker Compose
make docker-up

# Or manually:
docker-compose up -d

# Check service health
docker-compose ps
```

Wait for all services to show "healthy" status (may take 30-60 seconds).

### 4. Run Database Migrations

```bash
# Install EF Core tools (first time only)
make install-tools

# Run migrations
make migrate

# The database will be automatically seeded with demo data
```

### 5. Start the Application

```bash
# Development mode with hot reload
make dev

# Or standard run:
dotnet run
```

The application will start at `http://localhost:5000`

### 6. Verify Installation

Open your browser and navigate to:

- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health/ready
- **API Status**: http://localhost:5000/api/v2/system/status

## Default Credentials (Development Only)

**WARNING**: These credentials are for DEVELOPMENT ONLY and must be changed in production.

- **Username**: `admin`
- **Password**: `Admin123!`

## Development Workflow

### Running Tests

```bash
# Run all tests
make test

# Run tests in watch mode
make watch-test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Code Formatting

```bash
# Format code
make format

# Check formatting (CI uses this)
make lint
```

### Database Operations

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Update database
make migrate

# Reset database (WARNING: deletes all data)
make migrate-reset

# Seed database manually
make db-seed
```

### Docker Operations

```bash
# Start services
make docker-up

# Stop services
make docker-down

# View logs
make docker-logs

# Rebuild and restart
docker-compose up -d --build
```

## Configuration

### Environment Variables

All configuration is managed through environment variables. See `.env.example` for the complete list.

#### Required Variables

- `ConnectionStrings__DefaultConnection` - Database connection string
- `SQLSERVER_SA_PASSWORD` - SQL Server SA password
- `REDIS_PASSWORD` - Redis password
- `JwtAuthentication__SecretKey` - JWT signing key (256-bit)
- `MqttBroker__Password` - MQTT broker password

#### Optional Variables

- `WEATHER_API_KEY` - OpenWeatherMap API key
- `Seq__ApiKey` - Seq logging API key
- `CORS__AllowedOrigins` - Allowed CORS origins

### Configuration Files

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `.env` - Environment-specific secrets (not committed)
- `Configuration/mosquitto.conf` - MQTT broker settings

## API Documentation

### Available Endpoints

#### Device Management

- `GET /api/device` - List all devices
- `GET /api/device/{deviceId}` - Get device details
- `POST /api/device/{deviceId}/toggle` - Toggle device state
- `POST /api/device/telemetry` - Submit telemetry data
- `GET /api/device/{deviceId}/energy` - Get energy consumption

#### Energy Analytics

- `GET /api/energy/consumption` - Total consumption summary
- `GET /api/energy/cost` - Cost analysis and breakdown
- `GET /api/energy/forecast` - Energy usage predictions

#### Automation

- `GET /api/automation/rules` - List automation rules
- `POST /api/automation/rules` - Create new rule
- `GET /api/automation/rules/{id}` - Get rule details
- `PUT /api/automation/rules/{id}` - Update rule
- `DELETE /api/automation/rules/{id}` - Delete rule

#### Health & Status

- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe
- `GET /api/v2/system/status` - System status

### Authentication

All API endpoints (except health checks) require JWT authentication.

```bash
# Example: Get JWT token (dev only)
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}'

# Use token in subsequent requests
curl -X GET http://localhost:5000/api/device \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Troubleshooting

### Database Connection Issues

```bash
# Check SQL Server is running
docker ps | grep sqlserver

# Check SQL Server logs
docker logs nexushome-sqlserver

# Test connection
docker exec -it nexushome-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SQLSERVER_SA_PASSWORD" -Q "SELECT 1"
```

### Redis Connection Issues

```bash
# Check Redis is running
docker ps | grep redis

# Test Redis connection
docker exec -it nexushome-redis redis-cli -a "$REDIS_PASSWORD" ping
```

### MQTT Connection Issues

```bash
# Check MQTT broker is running
docker ps | grep mqtt

# Test MQTT connectivity
mosquitto_pub -h localhost -t "nexushome/test" -m "Hello" -u nexususer -P "$MQTT_PASSWORD"
mosquitto_sub -h localhost -t "nexushome/#" -v -u nexususer -P "$MQTT_PASSWORD"
```

### Application Won't Start

1. Check all required environment variables are set
2. Verify database migrations are up to date: `dotnet ef migrations list`
3. Check logs: `docker logs nexushome-app`
4. Verify port 5000 is not in use: `lsof -i :5000`

### Docker Issues

```bash
# Remove all containers and volumes (WARNING: deletes data)
docker-compose down -v

# Rebuild images from scratch
docker-compose build --no-cache

# Check disk space
docker system df

# Prune unused resources
docker system prune -a
```

## Testing MQTT

### Using Mosquitto CLI

```bash
# Subscribe to all topics
mosquitto_sub -h localhost -t "nexushome/#" -v

# Publish a test message
mosquitto_pub -h localhost -t "nexushome/devices/test-01/state" -m '{"power":"on"}'
```

### Using MQTT Explorer (GUI)

1. Download from: http://mqtt-explorer.com/
2. Connect to `localhost:1883`
3. Username: `nexususer` (or as configured)
4. Password: From your `.env` file

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Before Submitting a PR

1. Run tests: `make test`
2. Format code: `make format`
3. Check linting: `make lint`
4. Update documentation if needed
5. Add/update tests for new features

## Additional Resources

- [API Documentation](http://localhost:5000/swagger) (when running)
- [Architecture Documentation](ARCHITECTURE.md)
- [Security Guidelines](SECURITY.md)
- [Contributing Guidelines](CONTRIBUTING.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)

## Getting Help

- **Issues**: https://github.com/aaron-seq/NexusHome_IoT/issues
- **Discussions**: https://github.com/aaron-seq/NexusHome_IoT/discussions
- **Email**: support@nexushome.tech

## License

See [LICENSE](LICENSE) for details.
