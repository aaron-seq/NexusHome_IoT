# NexusHome IoT - Troubleshooting Guide

## Common Issues and Solutions

---

## Build and Startup Issues

### Error: "No .NET SDKs were found"
**Cause**: .NET 8.0 SDK not installed or not in PATH.

**Solution**:
```bash
# Verify installation
dotnet --version

# If not installed, download from:
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### Error: "Could not find a part of the path 'logs'"
**Cause**: Required directories missing.

**Solution**:
```bash
mkdir logs data uploads certificates
```

---

## Database Issues

### Error: "A network-related or instance-specific error occurred"
**Cause**: SQL Server not running or connection string incorrect.

**Solution**:
1. Verify SQL Server is running:
```bash
docker ps | grep sqlserver
```
2. Check connection string in `appsettings.Development.json`
3. Ensure `TrustServerCertificate=true` is set for local development

### Error: "Login failed for user"
**Cause**: SQL Server authentication issue.

**Solution**:
- For Docker SQL Server, use SA credentials
- Ensure password meets complexity requirements (8+ chars, mixed case, numbers)

---

## MQTT Issues

### Error: "MQTT client is not connected to broker"
**Cause**: MQTT broker not running or connection refused.

**Solution**:
1. Verify Mosquitto is running:
```bash
docker ps | grep mqtt
```
2. Test connection:
```bash
mosquitto_pub -h localhost -t test -m "hello"
```
3. Check firewall rules for port 1883

### Devices Not Receiving Commands
**Cause**: Topic mismatch or QoS issues.

**Solution**:
1. Verify topic structure matches configured pattern
2. Subscribe to wildcard topic for debugging:
```bash
mosquitto_sub -h localhost -t "nexushome/#" -v
```

---

## SignalR Issues

### WebSocket Connection Failed
**Cause**: Proxy configuration or CORS issues.

**Solution**:
1. Check browser console for specific error
2. Verify CORS is configured for your origin
3. If behind reverse proxy, ensure WebSocket upgrade is allowed:
```nginx
location /hubs/ {
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
}
```

---

## Docker Issues

### Container Exits Immediately
**Cause**: Missing environment variables or failed health check.

**Solution**:
1. Check logs:
```bash
docker logs nexushome-app
```
2. Verify all required environment variables are set
3. Check if dependent services (SQL, Redis, MQTT) are healthy

### "Port already in use"
**Cause**: Another process using the port.

**Solution**:
```bash
# Find process using port 5000
netstat -ano | findstr :5000

# Stop conflicting container
docker stop <container-name>
```

---

## Authentication Issues

### Error: "401 Unauthorized"
**Cause**: Missing or invalid JWT token.

**Solution**:
1. Obtain token via `/api/auth/login`
2. Include in request header: `Authorization: Bearer <token>`
3. Check token expiration (default 24 hours)

### Error: "IDX10223: Lifetime validation failed"
**Cause**: Token expired or clock skew.

**Solution**:
- Request new token
- If persistent, check server time synchronization

---

## Performance Issues

### High Memory Usage
**Cause**: Memory leaks or unbounded caching.

**Solution**:
1. Check Redis memory: `redis-cli INFO memory`
2. Review log file sizes in `/logs`
3. Monitor with: `dotnet-counters monitor --process-id <pid>`

### Slow API Responses
**Cause**: Database query performance or external API latency.

**Solution**:
1. Enable EF Core query logging
2. Check external API (Weather, Azure IoT) timeouts
3. Review Redis cache hit rates

---

## Getting More Help

1. Check [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md) for setup instructions
2. Review [ARCHITECTURE.md](ARCHITECTURE.md) for system understanding
3. Search existing GitHub issues
4. Create new issue with:
   - Error message and stack trace
   - Steps to reproduce
   - Environment details (.NET version, OS, Docker version)
