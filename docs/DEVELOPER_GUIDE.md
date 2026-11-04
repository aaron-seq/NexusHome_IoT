# Developer guide

Use [SETUP.md](../SETUP.md) for installation and configuration, [ARCHITECTURE.md](ARCHITECTURE.md) to find components, and [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for failures.

## Checks

Name the solution explicitly: the root contains both a `.sln` and `.csproj`.

```sh
dotnet restore NexusHome.IoT.sln
dotnet build NexusHome.IoT.sln --configuration Release --no-restore
dotnet test NexusHome.IoT.sln --configuration Release --no-build
dotnet format NexusHome.IoT.sln --verify-no-changes --no-restore
```

The [.github/workflows/ci.yml](../.github/workflows/ci.yml) workflow builds the API/test solution, runs tests with coverage, audits dependencies, builds and probes the API image, and scans the repository. Formatting is a local check; CI does not currently run it or enforce a coverage threshold. Report existing warnings and documented skips separately from regressions.

The Blazor client is outside the solution/CI; build it separately with `dotnet build NexusHome.Web/NexusHome.Web.csproj --configuration Release` when changing client files ([#39](https://github.com/aaron-seq/NexusHome_IoT/issues/39)).

Tests have no `Category` traits. Use their namespaces to select a subset:

```sh
dotnet test Tests/NexusHome.IoT.Tests.csproj --filter "FullyQualifiedName~NexusHome.IoT.Tests.Unit"
dotnet test Tests/NexusHome.IoT.Tests.csproj --filter "FullyQualifiedName~NexusHome.IoT.Tests.Integration"
dotnet test NexusHome.IoT.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

Coverage output is reproducible and ignored. The integration fixture replaces database/services and creates its in-memory schema; it does not provision the API's SQL Server database. Issue [#35](https://github.com/aaron-seq/NexusHome_IoT/issues/35) tracks the incomplete behavior behind existing skips. Preserve those assertions and their reasons until the behavior is implemented.

`make` is optional. [Makefile](../Makefile) wraps these commands and container tools. `make smoke` uses a POSIX shell to start/probe the API; use the README commands and HTTP probes directly in PowerShell. `make clean` uses `dotnet clean` for the selected configuration and retains persistent data. For API development, use `make run` or `dotnet watch --project NexusHome.IoT.csproj run`. `dotnet format` is built into the SDK.

## Common tasks

- **Device type:** update `DeviceCategory` in [Models.cs](../Core/Domain/Models.cs), relevant manager/adapter mappings, DTOs, and meaningful service tests.
- **HTTP endpoint:** extend the appropriate controller under `Controllers/` or `API/Controllers/`, keep its route/authentication contract explicit, and add DTO/validator and HTTP tests where needed. Validator classes alone do not establish runtime validation; check registration in [Program.cs](../Program.cs).
- **Dependency:** inspect source, DI, scripts, runtime assets, and packaging before removal. Use the package manager, preserve required transitive behavior, and verify restore/build/test/publish.
- **Schema:** follow [database provisioning](../SETUP.md#database-provisioning); review migrations and backup/recovery before touching existing data.

## Debugging

In browser DevTools, inspect WebSocket requests to `/hubs/deviceStatus` and `/hubs/energy`, then check CORS and authentication before treating connection failure as a transport bug. [SignalRService.cs](../NexusHome.Web/Services/SignalRService.cs) defines the client's event names and connection behavior.

For MQTT, subscribe using credentials/TLS matching your development broker:

```sh
mosquitto_sub -h localhost -t "nexushome/#" -v
```

For SQL queries, set `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command` to `Information` in local development configuration. Review logs for sensitive values before sharing them. Runtime controls and service registrations live in [Program.cs](../Program.cs), not merely in package references or configuration examples.
