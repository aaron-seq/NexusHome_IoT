.PHONY: help restore build test run clean docker-up docker-down docker-logs format lint audit smoke

SOLUTION = NexusHome.IoT.sln
PROJECT = NexusHome.IoT.csproj
CONFIGURATION ?= Release
DOCKER_COMPOSE ?= docker compose

help:
	@echo "restore / build / test - solution checks ($(CONFIGURATION))"
	@echo "run / smoke           - development API or POSIX health probe"
	@echo "clean                 - build output; retains databases and caches"
	@echo "audit / format / lint - dependency report or SDK formatting"
	@echo "docker-up / docker-down / docker-logs - Compose services"

restore:
	dotnet restore $(SOLUTION)

build: restore
	dotnet build $(SOLUTION) --configuration $(CONFIGURATION) --no-restore

test: build
	dotnet test $(SOLUTION) --configuration $(CONFIGURATION) --no-build

run:
	dotnet run --project $(PROJECT) --configuration $(CONFIGURATION) -- --environment Development

# Requires a POSIX shell. Use the same configuration built by the prerequisite.
smoke: build
	@dotnet run --project $(PROJECT) --configuration $(CONFIGURATION) --no-build -- --environment Development --urls http://127.0.0.1:5080 & \
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
	dotnet clean $(SOLUTION) --configuration $(CONFIGURATION)

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
