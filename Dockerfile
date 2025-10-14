# Multi-stage Dockerfile for NexusHome IoT Platform
# Optimized for production deployment on cloud platforms

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-environment

# Set working directory
WORKDIR /application-source

# Copy project files and restore dependencies
COPY ["NexusHome.IoT.csproj", "./"]
RUN dotnet restore "NexusHome.IoT.csproj" --disable-parallel

# Copy source code and build application
COPY . .
RUN dotnet build "NexusHome.IoT.csproj" -c Release -o /application-build

# Publish stage
FROM build-environment AS publish-environment
RUN dotnet publish "NexusHome.IoT.csproj" \
    -c Release \
    -o /application-publish \
    --no-restore \
    --verbosity minimal

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final-runtime

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    iputils-ping \
    && rm -rf /var/lib/apt/lists/*

# Create application directory and user
WORKDIR /application-runtime
RUN addgroup --system --gid 1001 nexusgroup \
    && adduser --system --uid 1001 nexususer

# Create required directories
RUN mkdir -p /application-runtime/logs \
    && mkdir -p /application-runtime/data \
    && mkdir -p /application-runtime/uploads \
    && mkdir -p /application-runtime/certificates

# Copy published application
COPY --from=publish-environment /application-publish .

# Set proper permissions
RUN chown -R nexususer:nexusgroup /application-runtime

# Switch to non-root user
USER nexususer

# Configure environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/ready || exit 1

# Expose port
EXPOSE 8080

# Start application
ENTRYPOINT ["dotnet", "NexusHome.IoT.dll"]
