# Setup and configuration

Use the [README quickstart](README.md#quickstart) to restore, build, and start the API. The solution includes the API and tests; [NexusHome.Web](NexusHome.Web/NexusHome.Web.csproj) is a separate Blazor client outside that solution. Development defaults to local SQLite and demo data; Docker, SQL Server, Redis, and MQTT are optional for inspecting the API.

## Configuration

The API reads [appsettings.json](appsettings.json), environment-specific overrides, process environment variables, and command-line settings through [Program.cs](Program.cs). Double underscores select nested environment settings. `dotnet run` does **not** load `.env` files automatically; Docker Compose uses `.env` for interpolation and passes only variables listed in its service definition.

| Setting | Purpose |
| --- | --- |
| `Database__Provider` | `SqlServer`, `Sqlite`, or `InMemory`; Development selects SQLite. |
| `ConnectionStrings__DefaultConnection` | Provider-specific database connection string. Use `localhost` for host SQL Server and `sqlserver` inside Compose. |
| `ConnectionStrings__Redis` | Optional Redis connection for device shadows and the SignalR backplane. Empty means local SignalR and disabled shadow persistence. |
| `JwtAuthentication__SecretKey`, `Issuer`, `Audience`, `ExpirationMinutes` | Prefix each key with `JwtAuthentication__`. Supply a private signing key of at least 32 characters outside Development. |
| `MqttBroker__Host`, `Port`, `Username`, `Password`, `UseTls` | Prefix each key with `MqttBroker__`; match the broker's authentication and transport. |
| `Cors__AllowedOrigins` | Allowed browser origins; comma-separated values or indexed configuration entries. See [CorsOrigins](Infrastructure/Configuration/CorsOrigins.cs). |
| `WeatherApi__ApiKey`, `BaseUrl`, `Location`, `Units` | Prefix each key with `WeatherApi__`. The current [weather provider](Infrastructure/Services/OpenWeatherMapProvider.cs) returns fixed sample data. |

For a disposable local API session with no database file:

```sh
dotnet run --project NexusHome.IoT.csproj --configuration Release --no-build --no-launch-profile -- --environment Development --urls http://localhost:5000 --Database:Provider InMemory
```

For persistent local development, use the README command, which loads the SQLite development settings. Keep the resulting database and logs private. For host overrides, use process variables or initialize .NET user secrets for the API project. Keep secrets out of commits and terminal transcripts; see [SECURITY.md](SECURITY.md).

## First API use

Open [Swagger](http://localhost:5000/swagger), [liveness](http://localhost:5000/health/live), and [readiness](http://localhost:5000/health/ready). Development seeding creates `admin` / `Admin123!` if no users exist. These are public demo credentials: use them only in an isolated development environment and never expose that account publicly.

Login with `POST /api/v1/Auth/login` and a JSON body containing `username` and `password`. Add the returned token to Swagger's authorization field or send `Authorization: Bearer <token>`. The [login handler](API/Controllers/AuthController.cs) verifies BCrypt hashes. Token refresh/session limitations remain ([#37](https://github.com/aaron-seq/NexusHome_IoT/issues/37)). See the [route table](docs/ARCHITECTURE.md#http-and-real-time-contracts) for the two API route families.

## Database provisioning

[Program.cs](Program.cs) initializes the database before starting the listener: SQL Server applies the checked-in [migrations](Infrastructure/Data/Migrations), while SQLite and InMemory call `EnsureCreated`. [DatabaseSeeder](Infrastructure/Data/DatabaseSeeder.cs) inserts demo entities and only creates the demo administrator in Development. Initialization errors are logged; check readiness as well as liveness.

`EnsureCreated` does not upgrade an existing SQLite schema. Review a provider-specific upgrade/backup plan before changing persisted models ([#40](https://github.com/aaron-seq/NexusHome_IoT/issues/40)). Do not reset databases or delete `.db` files as routine cleanup.

For deliberate SQL Server schema work, install a `dotnet-ef` tool version matching the project's EF version and specify the target project:

```sh
dotnet ef migrations add MigrationName --project NexusHome.IoT.csproj --output-dir Infrastructure/Data/Migrations
dotnet ef migrations script --idempotent --project NexusHome.IoT.csproj --output schema.sql
```

Review generated SQL, back up existing data, and test migrations/rollback on a disposable database before deployment. Runtime startup currently applies SQL Server migrations automatically; account for that when coordinating production changes.

## Dependency services and containers

Docker Desktop must be running. Copy [.env.example](.env.example) to a local `.env`, replace placeholders, and ensure SQL/Redis credentials and connection strings agree. Container DNS names in `.env` are for the Compose network, not a host `dotnet run` process. [docker-compose.yml](docker-compose.yml) selects SQL Server explicitly and forwards the weather key from `WEATHER_API_KEY`.

```sh
docker compose config --quiet
docker compose up -d --build
docker compose ps
docker compose logs -f nexushome-app
docker compose down
```

The API is available on host port 5000, with SQL Server on 1433, Redis on 6379, MQTT on 1883, and MQTT WebSockets on 9001. The broker mounts [Configuration/mosquitto.conf](Configuration/mosquitto.conf); it permits anonymous development access and must not be exposed publicly. Docker volumes preserve database, broker, logs, uploads, and application data. Avoid `down -v` or global pruning as troubleshooting shortcuts.

[Dockerfile](Dockerfile) builds only the API, includes `curl` for its `/health/live` probe, and runs as a non-root user on port 8080 with writable paths under `/app`. The separate Blazor client is not hosted by this image. To build the image alone:

```sh
docker build -t nexushome-iot:dev -f Dockerfile .
```

## Blazor client

```sh
dotnet run --project NexusHome.Web/NexusHome.Web.csproj --no-launch-profile -- --urls http://localhost:5179
```

The API's default CORS allowlist permits `http://localhost:5179`. Both [ApiService](NexusHome.Web/Services/ApiService.cs) and [SignalRService](NexusHome.Web/Services/SignalRService.cs) read `ApiBaseUrl`, defaulting to `https://localhost:7000`. Configure a browser-delivered `ApiBaseUrl` for your API address; it is public client configuration, never a place for secrets.

The client calls `/api/devices`, while the legacy controller uses `/api/Device`, and lacks a complete login/token flow. Some pages display sample data on failure. Running the UI is a preview, not evidence of live connectivity. Follow [#42](https://github.com/aaron-seq/NexusHome_IoT/issues/42) for integration work.

## Containers and hosting

The additional [Render](render.yaml), [Railway](railway.json), [Vercel](vercel.json), and [production Compose](docker-compose.prod.yml) files remain pending a supported-hosting decision ([#43](https://github.com/aaron-seq/NexusHome_IoT/issues/43)). They differ from the canonical Dockerfile/Compose configuration in provider, process, paths, ports, or probes. Their existence does not establish a validated production deployment.

See [troubleshooting](docs/TROUBLESHOOTING.md) for failed probes, credentials, and connection diagnostics; preserve data volumes while investigating.
