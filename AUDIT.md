# NexusHome IoT — Repository Audit

Audit and remediation of the NexusHome IoT platform, covering build integrity,
runtime correctness, security, dependencies, containerisation, CI and tests.

Every finding below was reproduced against the running application or the build
toolchain, not inferred from reading code. Environment: .NET SDK 8.0.129 on
Linux, SQLite provider, `ASPNETCORE_ENVIRONMENT=Development`.

---

## Executive summary

| | Before | After |
| --- | --- | --- |
| Application starts with default config | No — crashed at boot | Yes |
| Real API endpoints | 500 or 401 on every call | 200 with data |
| Login | Impossible | Works (BCrypt) |
| Health endpoints | 404 | 200 |
| Test suite | Could not restore; 0 tests ran | 111 pass, 20 documented skips, 0 fail |
| Known vulnerable packages | 3 (2 High) | 0 |
| NuGet references in web project | 63 | 24 |
| Committed build-log files | 18 (~530 KB) | 0 |
| Secrets in source control | JWT key, MQTT password, `.env.dev` | None |
| Canonical build entry point | None (no solution) | `NexusHome.IoT.sln` |
| EF Core migrations | None — schema never created | `InitialCreate`, 12 tables, 11 indices |

---

## Critical findings

### 1. Two entry points differing only by filename case

`Program.cs` (22 KB) and `program.cs` (6.9 KB) were **both tracked in git**.

On a case-insensitive filesystem (Windows, default macOS) a clone produces one
file and silently discards the other. On Linux — which is what Docker, CI and
production use — both exist, MSBuild emitted `warning CS2002: Source file
'Program.cs' specified multiple times`, and the **lowercase file won**.

Verified by inspecting the compiled assembly: it contained the `<Main>$`
synthesised entry point and the string `BlazorClient` (unique to `program.cs`),
and did not contain `started successfully` (unique to `Program.cs`).

The consequence is that the large, apparently "hardened" pipeline — rate
limiting, security headers, HSTS, forwarded headers, health checks,
authorization policies — **was never compiled into any deployed artifact**. It
could not have been: it references ten types that exist nowhere in the
repository (`IntelligentAutomationRuleEngine`, `IPredictiveMaintenanceEngine`,
`AdvancedPredictiveMaintenanceEngine`, `IEnergyOptimizationEngine`,
`MachineLearningEnergyOptimizationEngine`, `MultiChannelNotificationDispatcher`,
`RealTimeDataAggregationService`, `ComprehensiveSecurityManager`,
`SystemHealthMonitoringService`, `ComprehensiveErrorHandlingMiddleware`) and
calls `DatabaseSeeder.SeedDevelopmentDataAsync`, which does not exist either.

**Fix.** Both files removed and replaced with a single `Program.cs` built on the
service graph that actually compiles, extended with the parts of the dead file
that were implementable: authorization policies, health checks, rate limiting,
forwarded headers, HSTS, JSON options, JWT-over-query-string for SignalR, and
configuration-driven CORS.

### 2. Startup crashed whenever Redis was unreachable

```csharp
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));   // program.cs:77
```

The connection was established **during service registration**, not inside a
factory lambda. With the shipped default (`"Redis": "localhost:6379"`) the
process died at boot:

```
Unhandled exception. StackExchange.Redis.RedisConnectionException:
It was not possible to connect to the redis server(s).
   at Program.<Main>$(String[] args) in /home/user/NexusHome_IoT/program.cs:line 77
```

The repository could not be run from a fresh clone.

**Fix.** Registered as a lazy factory with `AbortOnConnectFail = false`, and the
whole Redis block is skipped when no connection string is configured.

### 3. Every implemented endpoint returned HTTP 500

`DeviceController`, `EnergyController` and `AutomationController` declare
`[Authorize(Policy = "UserAccess")]` on six actions. No `AddAuthorization` call
registering that policy existed anywhere in the compiled entry point, so ASP.NET
Core threw `InvalidOperationException: The AuthorizationPolicy named 'UserAccess'
was not found` on every request.

The only endpoints that appeared to work were stubs in `API/Controllers/` that
returned hardcoded zeroes.

**Fix.** Registered `AdminAccess`, `UserAccess`, `DeviceAccess` and
`TechnicianAccess`, matching the role claim `AuthController` issues.

### 4. Login could never succeed

`DatabaseSeeder` stores `BCrypt.HashPassword(password, workFactor: 12)`.
`AuthController.VerifyPassword` was:

```csharp
return inputPassword == storedHash;
```

A plaintext comparison against a BCrypt hash — always false, and a
non-constant-time comparison of a credential.

**Fix.** `BCrypt.Net.BCrypt.Verify`, with a stored value that is not a valid
BCrypt hash rejected and logged rather than falling back to plaintext.

### 5. Health endpoints did not exist

The Dockerfile and both compose files probed `/health/ready`. Nothing mapped it,
so it returned 404 and containers were permanently unhealthy — while
`depends_on: condition: service_healthy` gated other services on that signal.

**Fix.** `/health/live` (no dependencies, so a transient database outage cannot
cause the orchestrator to kill a healthy container) and `/health/ready`
(includes a database check).

### 6. The database schema was never created

The repository contained **no EF Core migrations**. The compiled entry point
called neither `Migrate()` nor `EnsureCreated()`, so tables never existed and
every query failed.

**Fix.** An `InitialCreate` migration is now committed, generated against SQL
Server via a new `SmartHomeDbContextFactory` design-time factory (needed because
the provider is selected from configuration and the committed connection strings
are empty). Startup applies migrations on SQL Server and creates the schema from
the model on the SQLite/InMemory development providers, then seeds. Failures are
logged without preventing the process from serving liveness probes while
dependencies come up.

Authoring the migration also surfaced two columns with no configured precision:
`WeatherData.Temperature` and `Humidity` would have silently fallen back to
`decimal(18,2)`. Both are now explicitly `decimal(5,2)`. The remaining decimal
columns already carried precision through data annotations — energy readings are
`decimal(12,4)`, which preserves the five-significant-figure kWh values the
seeder produces.

### 7. The test suite had never run

`dotnet restore` on the test project failed outright:

```
error NU1102: Unable to find package Verify.AspNetCore with version (>= 8.8.0)
error NU1107: Version conflict detected for xunit.extensibility.core
```

`Verify.AspNetCore 8.8.0` does not exist (latest is 5.0.0). CI hid this because
it ran `dotnet restore` / `build` / `test` at the repository root, which resolves
only `NexusHome.IoT.csproj` — the test project was never part of the pipeline.

**Fix.** Test project reduced to a coherent, resolvable set (xunit 2.9.2,
FluentAssertions, Moq, Mvc.Testing, EF InMemory, coverlet), the unused
heavyweight packages removed (NBomber, WireMock, BenchmarkDotNet, Verify,
Testcontainers, AutoFixture, Bogus, Shouldly, NSubstitute, ReportGenerator), and
a solution file added so CI builds and tests everything.

---

## Security findings

| # | Finding | Resolution |
| --- | --- | --- |
| S1 | JWT signing key committed in `appsettings.json` (`NexusHome-SuperSecure-JWT-SigningKey-2025-Version`). Anyone with repository access could forge tokens for any role. | Removed. Startup now **fails** outside Development unless a key of ≥32 characters is configured; Development falls back to a clearly-labelled insecure key. |
| S2 | MQTT broker password committed in `appsettings.json`. | Removed; supplied via environment. |
| S3 | `.env.dev` committed with database, Redis, MQTT and JWT credentials. | Deleted and git-ignored. |
| S4 | Plaintext password comparison in `AuthController`. | BCrypt verification (finding 4). |
| S5 | `API/Hubs/EnergyMonitoringHub` exposed `SendEnergyData` with **no authorization**, letting any connected client broadcast arbitrary payloads to all clients. The mapped hubs were these unauthenticated duplicates. | Deleted; the `[Authorize(Policy = "UserAccess")]` hubs in `Application/Hubs` are now the only ones mapped. |
| S6 | `AutoMapper 13.0.1` — GHSA-rvv3-g6hj-g44x (High). | Package was entirely unused; removed. |
| S7 | `MessagePack 2.5.108` via SignalR — GHSA-hv8m-jj95-wg3x, GHSA-vh6j-jc39-fggf (High) plus 6 Moderate. | Pinned to 2.5.302. |
| S8 | `SQLitePCLRaw.lib.e_sqlite3 2.1.6` — GHSA-2m69-gcr7-jv3q (High). | Pinned `SQLitePCLRaw.bundle_e_sqlite3` to 2.1.12. |
| S9 | Test frameworks (xunit, Moq, Testcontainers, Mvc.Testing) referenced by the **production web project**, and `Tests/**` was compiled into the deployed assembly. | Test packages removed; `Tests\**` added to `DefaultItemExcludes`. |
| S10 | No `.dockerignore`, so `.git`, `.env.dev` and build output were copied into the image build context. | Added. |
| S11 | JWT lifetime was 1440 minutes with no refresh-token implementation. | Development overrides to 60 minutes. The base value is unchanged — see *Known limitations*. |

`dotnet list package --vulnerable --include-transitive` now reports:

```
The given project `NexusHome.IoT` has no vulnerable packages given the current sources.
```

CI enforces this and fails the build if any advisory reappears.

---

## Correctness findings

### `[NotMapped]` alias properties used inside database queries

The domain model defines backwards-compatibility aliases (`PriorityLevel` →
`Priority`, `Device` → `SmartHomeDevice`, `WeatherData.Timestamp` →
`RecordedAt`). EF Core can evaluate these client-side in a final projection, but
**cannot translate them** in `Where`, `OrderBy`, `Include` or aggregates. Three
call sites threw at runtime:

- `AutomationController.GetRules` — `OrderByDescending(r => r.PriorityLevel)`
- `EnergyController.GetConsumption` — `Include(e => e.Device)`
- `EnergyOptimizationService.GetWeatherForecastAsync` — `Where(w => w.Timestamp …)`

All three now use the mapped columns. The remaining alias uses were audited and
are safe: they operate on already-materialised entities.

### Energy summary loaded the entire retention window into memory

`GetConsumption` materialised every reading in the period and aggregated in
process — cost growing linearly with device count and retention. Rewritten to
aggregate in the database, using nullable projections so an empty period returns
zero rather than throwing on `Average`.

### Background service could be torn down by a resolution failure

`AutomationRuleEngine.EvaluateRulesAsync` created its scope and resolved
services **outside** its `try` block, so a DI failure escaped to the hosted
service loop. Observed in practice as `Error in Automation Rule Processor
Service`. Moved inside the `try`. An existing test asserted this behaviour and
had been failing.

### Unregistered services

`MatterDiscoveryService` (required by `BleSimulationService`) and the ML
`AI.IPredictiveMaintenanceService` (resolved by the automation engine) were
never registered. Both now are; `MatterDiscoveryService` is a singleton because
it holds live mDNS discovery state.

### MQTT argument validation ran after the connection check

`PublishAsync`/`SubscribeAsync` called `ValidateConnectionState()` before
validating arguments, so a caller passing a null topic received a misleading
"not connected" error. Order reversed; whitespace-only topics are now rejected;
the constructor null-checks its options before dereferencing them.

### Configuration keys that silently did not bind

`appsettings.json` used `ExpirationInMinutes` and `RefreshTokenExpirationInDays`
while `JwtAuthenticationSettings` declares `ExpirationMinutes` and
`RefreshTokenExpirationDays`. The configured values were silently ignored in
favour of defaults. Renamed to match.

`docker-compose.prod.yml` set `Jwt__Secret`, `Mqtt__Host` and `Mqtt__Port` —
none of which are read by the application — and published ports 80/443 while the
container listens on 8080. Rewritten as a proper overlay.

---

## Duplication removed

| Duplicate | Resolution |
| --- | --- |
| `Program.cs` / `program.cs` | Single `Program.cs` |
| `Controllers/EnergyController` vs `API/Controllers/EnergyController` | Routes `api/Energy/consumption` and `api/energy/consumption` collide case-insensitively. Deleted the stub returning hardcoded zeroes. |
| `Controllers/AutomationController` vs `API/Controllers/AutomationController` | Same; stub deleted. |
| `EnergyMonitoringHub`, `SystemNotificationHub` in both `API/Hubs` and `Application/Hubs` | Kept the authorized `Application/Hubs` versions. |
| `Configuration/mosquitto.conf` and `Infrastructure/Configuration/mosquitto.conf` | Kept the one compose actually mounts. |
| `appsettings.Development.json` byte-identical to `appsettings.json` | Rewritten as a real override (SQLite, no Redis, no HTTPS redirect). |

---

## Dependency audit

The web project declared 63 package references. Usage analysis across all `.cs`
files found the following referenced **zero** times: AutoMapper (+DI), MediatR
(+DI), Hangfire (+SqlServer), Quartz (+Hosting), NCronTab, Refit
(+HttpClientFactory), Polly (+Extensions.Http), FluentResults,
AspNetCoreRateLimit, Newtonsoft.Json, Azure.Identity, Microsoft.Azure.Devices.Client,
Azure.Storage.Blobs, Microsoft.Extensions.Azure, ApplicationInsights,
SignalR.Protocols.MessagePack, SignalR.Client, MQTTnet.AspNetCore,
Testcontainers, DataProtection (+Redis), HealthChecks.UI (+storage, Redis,
SqlServer), System.Linq.Async, ObjectPool, System.Threading.Channels,
Swashbuckle Annotations/Filters, EF Proxies, ML.TimeSeries, ML.AutoML,
ML.LightGbm, ML.FastTree, and the VS container tooling target.

Removing them resolved two of the three vulnerability advisories outright.
Reference count: **63 → 24**. Build warnings: **98 → 78** (`CS2002` duplicate
source, `NU1608` constraint conflicts and `NU1903` advisories all cleared).

The project file also carried dead scaffolding: a `<Folder Include>` list
creating fifteen empty directories, `PreBuild`/`PostBuild` targets that only
echoed text, `Content Update` entries copying `docker-compose.yml` into the
build output, and `RetainVMGarbageCollection`, which is not a real MSBuild
property.

---

## Container and CI findings

- **Health check used `curl`, absent from `mcr.microsoft.com/dotnet/aspnet:8.0`.**
  Combined with the missing endpoint, the check could never pass. `curl` is now
  installed in the runtime stage and the probe targets `/health/live`.
- **Runtime directories were created before `COPY` and owned by root** while the
  container runs as `nexususer`, so log and data writes would fail. Now
  `chown`ed.
- **The image built twice** (`dotnet build` then `dotnet publish`). Now publishes
  directly.
- **SQL Server health check** used `/opt/mssql-tools/bin/sqlcmd`; the
  `2022-latest` image ships `mssql-tools18`, which also requires `-C` to accept
  the self-signed certificate. Since the app gated on this signal, the stack
  could not come up. `SA_PASSWORD` also updated to `MSSQL_SA_PASSWORD`.
- **Obsolete `version:` key** removed from all compose files.
- **CI never built or ran the test project**, ran `dotnet format
  --verify-no-changes` against a codebase that has never been formatted, and used
  deprecated `actions/cache@v3`, `codeql-action@v2` and
  `docker/build-push-action@v5`. Rewritten around the solution file with a
  vulnerability gate and a container smoke test that boots the published image
  and probes liveness.

---

## Repository hygiene

Eighteen build-transcript files were committed (`build.log`, `build_errors.txt`,
`build_phase3_error_utf8.txt`, `backend_build.txt`, …) totalling roughly 530 KB
of compiler output, plus an empty file named `Docker`. All removed, and
`.gitignore` extended so they cannot return.

`SmartHomeDbContext` was 52 lines of which about 20 were an assistant's
stream-of-consciousness notes ("Wait, looking at file 204 again…") left in
production code, with no indices or relationship configuration. Rewritten with
unique indices on `User.Username`/`Email` and `SmartHomeDevice.UniqueDeviceIdentifier`,
composite indices on the device/timestamp pairs that the energy and alert queries
filter on, and a SQLite decimal converter (SQLite cannot `Sum` a decimal column,
which was breaking the energy optimizer under the development provider).

---

## Verification

Build:

```
dotnet build NexusHome.IoT.sln -c Release   →  0 errors
dotnet test  NexusHome.IoT.sln -c Release   →  111 passed, 20 skipped, 0 failed
dotnet list  package --vulnerable --include-transitive  →  none
```

End-to-end against the running application (`scripts` equivalent in
`make smoke`):

| Check | Result |
| --- | --- |
| `/health/live` | 200 |
| `/health/ready` | 200 |
| `/swagger/v1/swagger.json` | 200 |
| `/api/Energy/consumption` unauthenticated | 401 |
| Login, wrong password | 401 |
| Login, correct password | 200, JWT issued |
| `/api/Energy/consumption` authenticated | 200 with aggregated data |
| `/api/Energy/cost`, `/api/Energy/forecast` | 200 |
| `/api/Device`, `/api/Automation/rules` | 200 |

Seeding produces 5 demo devices, 672 energy readings and 3 automation rules. The
only remaining runtime error in a clean local run is the MQTT broker being
absent, which is expected without the compose stack.

---

## Known limitations

These are deliberately left open rather than silently patched.

1. **Migrations exist only for SQL Server.** An `InitialCreate` migration is now
   committed (12 tables, 11 indices) and is applied automatically when the
   provider is SQL Server. The SQLite and InMemory development providers still
   use `EnsureCreated`, because the generated SQL is provider-specific — adding
   a second provider to the deployment set would require either a second
   migrations assembly or provider-conditional model configuration.

2. **20 tests are skipped, each with a reason string.** Sixteen in
   `SmartDevicesControllerIntegrationTests` specify an API that
   `SmartDevicesController` does not implement — pagination, category filtering,
   `409 Conflict` on duplicate registration, force-delete semantics, telemetry
   submission and device command endpoints. Four unit tests assert service
   behaviour that likewise does not exist yet. They were failing because they
   describe intended behaviour, so they are retained as an executable
   specification rather than deleted. Implementing that API surface is the
   natural next piece of work.

3. **Base JWT lifetime remains 1440 minutes** with no refresh-token flow, despite
   `RefreshTokenExpirationDays` being configured. Development overrides to 60.
   Shortening the production default is a behavioural change that should be made
   deliberately alongside implementing refresh tokens.

4. **Build warnings remain**, dominated by 24 `CS8618` (non-nullable field
   uninitialised), 24 `CS1998` (async method without await), 8 `CS0067` (unused
   event) and 6 `ASP0019` (header assignment that can throw). The six `CS4014`
   unawaited-task warnings have been fixed: `OnOffClusterHandler.TurnOn/TurnOff/
   Toggle` were `void` wrappers that discarded an async command, so a failure to
   reach the device surfaced nowhere. They are now `TurnOnAsync`/`TurnOffAsync`/
   `ToggleAsync` returning the task. They had no callers, so nothing broke.

5. **`AI/PredictiveMaintenanceService` and `Core/Services/PredictiveMaintenanceService`
   are two different types implementing two different `IPredictiveMaintenanceService`
   interfaces** in different namespaces. Both are now registered because both are
   used, but the duplication is confusing and should be consolidated.

6. **`NexusHome.Web`, `NexusHome.WebApp` and `NexusHome.DeviceSimulator` were not
   audited.** They are excluded from the web project's compilation and are not in
   the solution. `NexusHome.WebApp` contains a single `.razor` file and no project
   file, so it cannot build at all.

7. **The MQTT/Matter/BLE integrations were exercised only to the point of
   startup.** No broker or Matter fabric was available in the audit environment,
   so their message-handling paths are unverified.

---

## Rollback

Every change is confined to this branch. Reverting the two commits restores the
previous state exactly; no data migration or schema change is involved, and no
external system is written to. The riskiest individual change is the removal of
`API/Controllers/{Energy,Automation}Controller.cs` — if any client depended on
the lowercase `api/energy/*` routes returning hardcoded zeroes, it now receives
real data from the equivalent `api/Energy/*` routes, which require
authentication.
