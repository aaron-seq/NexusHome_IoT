.PHONY: help build test clean dev docker-up docker-down format lint restore migrate

# Variables
PROJECT = NexusHome.IoT
DOCKER_COMPOSE = docker-compose

help:
	@echo "NexusHome IoT - Available Commands:"
	@echo "  make build       - Build the application"
	@echo "  make test        - Run all tests"
	@echo "  make clean       - Clean build artifacts"
	@echo "  make dev         - Start development server"
	@echo "  make docker-up   - Start Docker services"
	@echo "  make docker-down - Stop Docker services"
	@echo "  make format      - Format code"
	@echo "  make lint        - Lint code"
	@echo "  make restore     - Restore NuGet packages"
	@echo "  make migrate     - Run database migrations"

restore:
	@echo "Restoring NuGet packages..."
	dotnet restore

build: restore
	@echo "Building project..."
	dotnet build --configuration Release

test:
	@echo "Running tests..."
	dotnet test --configuration Release --logger "console;verbosity=detailed"

clean:
	@echo "Cleaning build artifacts..."
	dotnet clean
	rm -rf bin/ obj/ TestResults/ coverage/

dev:
	@echo "Starting development server..."
	dotnet watch run --project $(PROJECT).csproj

docker-up:
	@echo "Starting Docker services..."
	$(DOCKER_COMPOSE) up -d
	@echo "Waiting for services to be healthy..."
	sleep 10
	$(DOCKER_COMPOSE) ps

docker-down:
	@echo "Stopping Docker services..."
	$(DOCKER_COMPOSE) down

docker-logs:
	@echo "Showing Docker logs..."
	$(DOCKER_COMPOSE) logs -f

format:
	@echo "Formatting code..."
	dotnet format

lint:
	@echo "Checking code format..."
	dotnet format --verify-no-changes

migrate:
	@echo "Running database migrations..."
	dotnet ef database update

migrate-reset:
	@echo "Resetting database..."
	dotnet ef database drop -f
	dotnet ef database update

db-seed:
	@echo "Seeding database..."
	dotnet run -- --seed

watch-test:
	@echo "Running tests in watch mode..."
	dotnet watch test

install-tools:
	@echo "Installing development tools..."
	dotnet tool install --global dotnet-ef
	dotnet tool install --global dotnet-format

update-tools:
	@echo "Updating development tools..."
	dotnet tool update --global dotnet-ef
	dotnet tool update --global dotnet-format
