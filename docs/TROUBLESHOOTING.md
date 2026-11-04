# Troubleshooting

Use [SETUP.md](../SETUP.md) for configuration and dependency commands. Capture sanitized errors and the exact command before changing configuration or data.

## Build and startup

- **SDK missing:** check `dotnet --list-sdks` and install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- **Project/solution selection error:** name `NexusHome.IoT.sln` for restore/build/test/format, or `NexusHome.IoT.csproj` for running/publishing the API.
- **Missing browser runtime pack (`NETSDK1112`):** run `dotnet restore NexusHome.IoT.sln`, then rebuild; an old `obj` directory may have incomplete restore assets.
- **Publish runtime target missing (`NETSDK1047`):** allow publish to restore for the selected runtime, for example `dotnet publish NexusHome.IoT.csproj -c Release -r win-x64 --self-contained false`. Do not reuse `--no-restore` after a restore that omitted that runtime.
- **Missing writable directories:** confirm the host's configured log/data paths and create only the required directories. In a container, check the mounted directory's ownership and the non-root process user.

## Database, Redis, and MQTT

For connection/login failures, check `docker compose ps` and the affected service logs. Use `localhost` from a host process and service DNS names inside Compose. Ensure SQL credentials, Redis authentication, ports, and connection strings match. `TrustServerCertificate=true` is a local SQL development option, not a production TLS policy.

For missing tables/users, inspect startup initialization logs and [database provisioning](../SETUP.md#database-provisioning). SQL Server migrations are applied automatically; SQLite/InMemory create schema from the model and do not upgrade existing SQLite databases. Demo users are seeded only in Development. Back up existing data before a reviewed schema change; do not reset databases or delete volumes to diagnose a connection failure.

For MQTT, verify broker reachability, credentials/TLS, and topics from [MqttBrokerSettings.cs](../Infrastructure/Configuration/MqttBrokerSettings.cs). Subscribe to `nexushome/#` in an isolated development environment to inspect telemetry/commands. Anonymous access in the supplied broker configuration is for local development only.

For a Redis connection failure, check the password in `ConnectionStrings:Redis`. An empty connection string permits local development with local SignalR and disabled shadow persistence. A reachable Redis instance is required when persistence/backplane operation is configured.

## API and browser

- **401:** send `Authorization: Bearer <token>` and verify issuer, audience, signing key, expiry, and server clock. Login is `POST /api/v1/Auth/login`; the handler verifies BCrypt hashes; demo account seeding is Development-only ([AuthController.cs](../API/Controllers/AuthController.cs)).
- **403:** inspect controller policies and role claims in [Program.cs](../Program.cs).
- **404:** verify routes in Swagger or the [route table](ARCHITECTURE.md#http-and-real-time-contracts); `/api/devices` and `/api/Device` are different paths.
- **WebSocket/CORS:** inspect failed requests in browser DevTools. The API origin allowlist and hub paths are in `Program.cs`. A reverse proxy must forward to the API and support WebSocket upgrades. The client also needs a matching `ApiBaseUrl` and authenticated session ([#42](https://github.com/aaron-seq/NexusHome_IoT/issues/42)).
- **UI shows data while API fails:** several client paths use sample fallbacks. Check network responses before treating displayed values as live telemetry.

## Containers

If Docker cannot connect to its named pipe/socket, start the daemon before retrying. For container failures, inspect `docker compose logs nexushome-app` and dependency logs. The canonical image includes `curl` and probes `/health/live`; `/health/ready` additionally checks the database. A live process can have an unavailable database, so inspect both responses and initialization logs. Additional cloud templates still need validation; see [hosting limitations](../SETUP.md#containers-and-hosting).

For a port conflict on Windows, inspect `netstat -ano | findstr :5000` and select an available application port or resolve the identified conflict. Stop only services you own and intend to stop. Keep database/data volumes; avoid `down -v` and global pruning as troubleshooting shortcuts.

## Diagnostics and reporting

For slow queries, enable development EF command logging as described in the [developer guide](DEVELOPER_GUIDE.md#debugging). For memory growth, inspect application logs, database history size, and Redis `INFO memory`; use `dotnet-counters` if installed. Collect measurements before attributing a cause.

Search [existing issues](https://github.com/aaron-seq/NexusHome_IoT/issues) before reporting a new problem. Include SDK/OS/service versions, reproduction steps, the relevant commit, and sanitized logs. Report vulnerabilities through [SECURITY.md](../SECURITY.md).
