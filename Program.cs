using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NexusHome.IoT.API.Middleware;
using NexusHome.IoT.Application.Hubs;
using NexusHome.IoT.Core.Services;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Adapters;
using NexusHome.IoT.Infrastructure.Configuration;
using NexusHome.IoT.Infrastructure.Data;
using NexusHome.IoT.Infrastructure.Services;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logging
// ---------------------------------------------------------------------------
builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---------------------------------------------------------------------------
// JWT signing key
//
// Resolved before anything binds JwtAuthentication so that the options pattern
// and the JWT bearer handler always agree on the same key. A signing key
// shipped in source control is equivalent to no signing key at all, so refuse
// to start in production rather than accept forged tokens.
// ---------------------------------------------------------------------------
const string JwtSecretKeyPath = "JwtAuthentication:SecretKey";
var configuredSecret = builder.Configuration[JwtSecretKeyPath];

if (string.IsNullOrWhiteSpace(configuredSecret) || configuredSecret.Length < 32)
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            $"{JwtSecretKeyPath} must be configured with at least 32 characters. " +
            "Set it via the JwtAuthentication__SecretKey environment variable or a secret store.");
    }

    builder.Configuration[JwtSecretKeyPath] = "nexushome-development-only-signing-key-do-not-use-in-production";
    Log.Warning("{Path} is missing or too short. Using an insecure development key.", JwtSecretKeyPath);
}

// ---------------------------------------------------------------------------
// Strongly typed configuration
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtAuthenticationSettings>(builder.Configuration.GetSection("JwtAuthentication"));
builder.Services.Configure<MqttBrokerSettings>(builder.Configuration.GetSection("MqttBroker"));
builder.Services.Configure<WeatherApiSettings>(builder.Configuration.GetSection("WeatherApi"));

// ---------------------------------------------------------------------------
// Database
//
// The provider is selectable so the platform runs locally with zero external
// dependencies (Sqlite/InMemory) while production keeps using SQL Server.
// ---------------------------------------------------------------------------
var databaseProvider = builder.Configuration.GetValue("Database:Provider", "SqlServer")!;
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<SmartHomeDbContext>(options =>
{
    switch (databaseProvider.ToLowerInvariant())
    {
        case "inmemory":
            options.UseInMemoryDatabase("NexusHomeIoT");
            break;

        case "sqlite":
            options.UseSqlite(defaultConnection ?? "Data Source=nexushome.db");
            break;

        default:
            options.UseSqlServer(defaultConnection, sqlOptions =>
            {
                // Transient faults are expected when the database container is
                // still warming up alongside the app.
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(120);
            });
            break;
    }
});

// ---------------------------------------------------------------------------
// Authentication & authorization
// ---------------------------------------------------------------------------
var jwtSettings = builder.Configuration.GetSection("JwtAuthentication").Get<JwtAuthenticationSettings>()
    ?? new JwtAuthenticationSettings();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = jwtSettings.ValidateIssuer,
            ValidateAudience = jwtSettings.ValidateAudience,
            ValidateLifetime = jwtSettings.ValidateLifetime,
            ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(jwtSettings.ClockSkewMinutes)
        };

        // Browsers cannot set Authorization headers on WebSocket handshakes, so
        // SignalR clients pass the token as a query string parameter instead.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminAccess", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("UserAccess", policy => policy.RequireRole("User", "Administrator"));
    options.AddPolicy("DeviceAccess", policy => policy.RequireRole("Device", "User", "Administrator"));
    options.AddPolicy("TechnicianAccess", policy => policy.RequireRole("Technician", "Administrator"));
});

// ---------------------------------------------------------------------------
// CORS
// ---------------------------------------------------------------------------
var allowedOrigins = CorsOrigins.Resolve(
    builder.Configuration,
    "Cors:AllowedOrigins",
    ["http://localhost:5000", "https://localhost:5001", "http://localhost:5179"]);

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

// ---------------------------------------------------------------------------
// Caching, SignalR and the Redis backplane
//
// The multiplexer is registered as a lazy factory. Connecting eagerly here
// would take the whole process down whenever Redis is briefly unavailable.
// ---------------------------------------------------------------------------
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var signalRBuilder = builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

builder.Services.AddMemoryCache();

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
    redisOptions.AbortOnConnectFail = false;

    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
    signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.AbortOnConnectFail = false;
        options.Configuration.ChannelPrefix = RedisChannel.Literal("NexusHome");
    });

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "NexusHomeIoT";
    });
}

// ---------------------------------------------------------------------------
// Web API
// ---------------------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NexusHome IoT API",
        Version = "v1",
        Description = "Smart home energy management and IoT control platform."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("StandardApiLimiter", limiterOptions =>
    {
        limiterOptions.PermitLimit = builder.Configuration.GetValue("RateLimit:PermitLimit", 1000);
        limiterOptions.Window = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimit:WindowSeconds", 60));
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 100;
    });
});

// ---------------------------------------------------------------------------
// Health checks
// ---------------------------------------------------------------------------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SmartHomeDbContext>("database");

// ---------------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------------
builder.Services.AddScoped<ISmartDeviceManager, SmartDeviceManager>();
builder.Services.AddScoped<IEnergyConsumptionAnalyzer, EnergyConsumptionAnalyzer>();
builder.Services.AddScoped<IAutomationRuleEngine, AutomationRuleEngine>();
builder.Services.AddScoped<IPredictiveMaintenanceService, PredictiveMaintenanceService>();
builder.Services.AddScoped<IEnergyOptimizationService, EnergyOptimizationService>();
builder.Services.AddScoped<IMatterService, MatterService>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddScoped<IDataAggregationService, DataAggregationService>();
builder.Services.AddScoped<ISecurityManager, SecurityManager>();

// The ML-backed anomaly detection stack is a separate contract from
// Core.Services.IPredictiveMaintenanceService and is resolved by the
// automation rule engine at evaluation time.
builder.Services.AddSingleton<Microsoft.ML.MLContext>(_ => new Microsoft.ML.MLContext(seed: 0));
builder.Services.AddScoped<NexusHome.IoT.AI.IPredictiveMaintenanceService, NexusHome.IoT.AI.PredictiveMaintenanceService>();

builder.Services.AddSingleton<IMqttClientService, EnhancedMqttClientService>();
builder.Services.AddHttpClient<IWeatherDataProvider, OpenWeatherMapProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<IUtilityPriceProvider, UtilityPriceProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(45);
});

builder.Services.AddScoped<IProtocolBridge, UnifiedProtocolBridge>();
// Holds live mDNS discovery state, so it must outlive a single request scope.
builder.Services.AddSingleton<MatterDiscoveryService>();
builder.Services.AddScoped<DeviceShadowService>();
builder.Services.AddScoped<UniversalDeviceBridge>();
builder.Services.AddScoped<IDeviceAdapter, MatterDeviceAdapter>();
builder.Services.AddScoped<IDeviceAdapter, MqttDeviceAdapter>();
builder.Services.AddScoped<MatterDeviceAdapter>();
builder.Services.AddScoped<MqttDeviceAdapter>();
builder.Services.AddScoped<BleSimulationService>();

builder.Services.AddHostedService<DeviceDataCollectionService>();
builder.Services.AddHostedService<EnergyMonitoringBackgroundService>();
builder.Services.AddHostedService<MaintenanceSchedulingService>();
builder.Services.AddHostedService<AutomationRuleProcessorService>();
builder.Services.AddHostedService<EnergyOptimizationBackgroundService>();
builder.Services.AddHostedService<PredictiveMaintenanceBackgroundService>();
builder.Services.AddHostedService<MqttConnectionService>();

var app = builder.Build();

// ---------------------------------------------------------------------------
// HTTP pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "NexusHome IoT API v1"));
}
else
{
    app.UseHsts();
}

// Must run before authentication so the scheme and client IP reflect the
// original request when running behind a reverse proxy or load balancer.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

// Containers terminate TLS at the ingress, so an in-container redirect would
// produce a loop against a plain-HTTP listener.
if (builder.Configuration.GetValue("Security:UseHttpsRedirection", !app.Environment.IsDevelopment()))
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("BlazorClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("StandardApiLimiter");

app.MapHub<SmartDeviceStatusHub>("/hubs/deviceStatus");
app.MapHub<EnergyMonitoringHub>("/hubs/energy");
app.MapHub<SystemNotificationHub>("/hubs/notifications");
app.MapHub<MaintenanceAlertHub>("/hubs/maintenance");

// Liveness must not depend on external systems, otherwise a transient database
// outage causes the orchestrator to kill an otherwise healthy container.
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready");

await InitializeDatabaseAsync(app);

await app.RunAsync();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<SmartHomeDbContext>();

        // Migrations are authored against SQL Server, so the generated SQL is
        // provider-specific. The SQLite and InMemory development providers
        // create their schema from the model instead.
        var isSqlServer = context.Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer";

        if (isSqlServer && context.Database.GetMigrations().Any())
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Applied database migrations");
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Created database schema from the model ({Provider})",
                context.Database.ProviderName);
        }

        await DatabaseSeeder.SeedAsync(context, services, logger, app.Environment);
        logger.LogInformation("Database initialization completed");
    }
    catch (Exception exception)
    {
        // A database that is not reachable yet should not prevent the API from
        // serving liveness probes while the orchestrator retries dependencies.
        logger.LogError(exception, "Database initialization failed");
    }
}

/// <summary>
/// Exposed so <c>WebApplicationFactory&lt;Program&gt;</c> can bootstrap the
/// application in integration tests.
/// </summary>
public partial class Program;
