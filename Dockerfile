# Multi-stage Dockerfile for the NexusHome IoT Platform.

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first against the project file alone so the layer is reused whenever
# only source files change.
COPY ["NexusHome.IoT.csproj", "./"]
RUN dotnet restore "NexusHome.IoT.csproj"

COPY . .
RUN dotnet publish "NexusHome.IoT.csproj" -c Release -o /app/publish --no-restore

# ---------------------------------------------------------------------------
# Runtime
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# curl is not present in the aspnet runtime image but the container health
# check and docker compose both rely on it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN addgroup --system --gid 1001 nexusgroup \
    && adduser --system --uid 1001 --ingroup nexusgroup nexususer

COPY --from=build /app/publish .

# Writable paths must be owned by the runtime user; the application writes logs
# and SQLite/dev data here and would otherwise fail with permission errors.
RUN mkdir -p /app/logs /app/data /app/uploads /app/certificates \
    && chown -R nexususer:nexusgroup /app

USER nexususer

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "NexusHome.IoT.dll"]
