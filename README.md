# NexusHome IoT

NexusHome is a .NET 8 smart-home prototype for device control, energy reporting, automation, and MQTT/Matter integration. It contains an ASP.NET Core API, a separate Blazor WebAssembly client, and xUnit tests. Runtime registrations are in [Program.cs](Program.cs); project boundaries and limitations are described in the [architecture guide](docs/ARCHITECTURE.md).

## Quickstart

Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), then run from the repository root:

```sh
dotnet restore NexusHome.IoT.sln
dotnet build NexusHome.IoT.sln --configuration Release --no-restore
dotnet run --project NexusHome.IoT.csproj --configuration Release --no-build --no-launch-profile -- --environment Development --urls http://localhost:5000
```

Open [Swagger](http://localhost:5000/swagger) to inspect the API. Development defaults to a local SQLite database and seeds demo data. Liveness is available at [health/live](http://localhost:5000/health/live). Follow [SETUP.md](SETUP.md) for configuration, dependency services, the client, and provisioning blockers.

## Checks

```sh
dotnet test NexusHome.IoT.sln --configuration Release --no-build
dotnet format NexusHome.IoT.sln --verify-no-changes --no-restore
```

The suite contains documented skips for incomplete behavior; formatting violations also remain. See [development checks](docs/DEVELOPER_GUIDE.md#checks) for filters and coverage. Passing compilation alone does not establish a working device or browser flow.

## Material limitations

- SQL Server migrations and SQLite/InMemory schema creation use different upgrade paths. Back up existing data before schema changes; see [database setup](SETUP.md#database-provisioning).
- [Authentication](API/Controllers/AuthController.cs) verifies BCrypt passwords, but a refresh-token flow is incomplete ([#37](https://github.com/aaron-seq/NexusHome_IoT/issues/37)).
- The Blazor client has route/session mismatches and demo fallbacks ([#42](https://github.com/aaron-seq/NexusHome_IoT/issues/42)).
- Additional cloud deployment templates need validation ([deployment limitations](SETUP.md#containers-and-hosting)). No measured throughput, model accuracy, energy savings, or coverage percentage is promised.

## Documentation

- [Setup and configuration](SETUP.md)
- [Development tasks](docs/DEVELOPER_GUIDE.md)
- [Architecture and decision records](docs/ARCHITECTURE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Contributing](CONTRIBUTING.md), [security policy](SECURITY.md), [code of conduct](CODE_OF_CONDUCT.md), and [changelog](CHANGELOG.md)

Licensed under the [MIT License](LICENSE).
