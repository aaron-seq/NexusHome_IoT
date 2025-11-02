# Multi-stage Dockerfile for NexusHome IoT Platform (aligned to 8080)

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-environment
WORKDIR /application-source
COPY ["NexusHome.IoT.csproj", "./"]
RUN dotnet restore "NexusHome.IoT.csproj" --disable-parallel
COPY . .
RUN dotnet build "NexusHome.IoT.csproj" -c Release -o /application-build

# Publish stage
FROM build-environment AS publish-environment
RUN dotnet publish "NexusHome.IoT.csproj" -c Release -o /application-publish --no-restore --verbosity minimal

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final-runtime
WORKDIR /application-runtime
RUN addgroup --system --gid 1001 nexusgroup \ 
    && adduser --system --uid 1001 nexususer
RUN mkdir -p /application-runtime/logs /application-runtime/data /application-runtime/uploads /application-runtime/certificates
COPY --from=publish-environment /application-publish .
USER nexususer
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 CMD curl -f http://localhost:8080/health/ready || exit 1
EXPOSE 8080
ENTRYPOINT ["dotnet", "NexusHome.IoT.dll"]
