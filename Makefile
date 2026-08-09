.PHONY: help restore build test run clean docker-up docker-down docker-logs format lint audit smoke

SOLUTION      = NexusHome.IoT.sln
PROJECT       = NexusHome.IoT.csproj
DOCKER_COMPOSE = docker compose

help:
	@echo "NexusHome IoT - Available Commands:"
	@echo "  make restore     - Restore NuGet packages"
	@echo "  make build       - Build the solution (Release)"
	@echo "  make test        - Run the test suite"
	@echo "  make run         - Run the API locally (SQLite, no external services)"
	@echo "  make smoke       - Build and probe the health endpoint"
	@echo "  make audit       - Report known vulnerable NuGet packages"
	@echo "  make clean       - Remove build artifacts"
	@echo "  make docker-up   - Start the full stack"
	@echo "  make docker-down - Stop the stack"
	@echo "  make docker-logs - Follow stack logs"
	@echo "  make format      - Format code"
	@echo "  make lint        - Verify formatting"

restore:
	dotnet restore $(SOLUTION)

build: restore
	dotnet build $(SOLUTION) --configuration Release --no-restore

test: build
	dotnet test $(SOLUTION) --configuration Release --no-build

# Development uses SQLite and needs no database, Redis or MQTT broker running.
run:
	ASPNETCORE_ENVIRONMENT=Development dotnet run --project $(PROJECT)

smoke: build
	@echo "Starting the API and probing /health/live..."
	@ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5080 \
		dotnet run --project $(PROJECT) --no-build & \
	APP_PID=$$!; \
	for i in $$(seq 1 30); do \
		sleep 2; \
		if curl -fsS http://127.0.0.1:5080/health/live > /dev/null 2>&1; then \
			echo "OK: application is live"; kill $$APP_PID; exit 0; \
		fi; \
	done; \
	echo "FAILED: application did not become healthy"; kill $$APP_PID; exit 1

audit: restore
	dotnet list $(SOLUTION) package --vulnerable --include-transitive

clean:
	dotnet clean $(SOLUTION) || true
	rm -rf bin/ obj/ Tests/bin/ Tests/obj/ TestResults/ coverage/ *.db

docker-up:
	$(DOCKER_COMPOSE) up -d --build
	$(DOCKER_COMPOSE) ps

docker-down:
	$(DOCKER_COMPOSE) down

docker-logs:
	$(DOCKER_COMPOSE) logs -f

format:
	dotnet format $(SOLUTION)

lint:
	dotnet format $(SOLUTION) --verify-no-changes
