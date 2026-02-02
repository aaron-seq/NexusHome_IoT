using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.API.Hubs;
using NexusHome.IoT.API.Middleware;
using NexusHome.IoT.Core.Services;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Configuration;
using NexusHome.IoT.Infrastructure.Data;
using NexusHome.IoT.Infrastructure.Services;
using NexusHome.IoT.Infrastructure.Adapters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// CORS for Blazor WebAssembly frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
        policy.WithOrigins(
            "https://localhost:5001",
            "http://localhost:5000",
            "https://localhost:7001",
            "http://localhost:5179"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

// Configuration
builder.Services.Configure<JwtAuthenticationSettings>(builder.Configuration.GetSection("JwtAuthentication"));
builder.Services.Configure<MqttBrokerSettings>(builder.Configuration.GetSection("MqttBroker"));
builder.Services.Configure<WeatherApiSettings>(builder.Configuration.GetSection("WeatherApi"));

// Database
builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication
var jwtSettings = builder.Configuration.GetSection("JwtAuthentication").Get<JwtAuthenticationSettings>();
if (jwtSettings != null)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });
}

builder.Services.AddControllers();
// SignalR with Redis Backplane
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddSignalR().AddStackExchangeRedis(redisConnectionString, options => {
        options.Configuration.ChannelPrefix = "NexusHome";
    });
    
    // Register Redis Connection for Shadow State
    builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
        StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));
}
else
{
    builder.Services.AddSignalR();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NexusHome IoT API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Core Services
builder.Services.AddScoped<ISmartDeviceManager, SmartDeviceManager>();
builder.Services.AddScoped<IEnergyConsumptionAnalyzer, EnergyConsumptionAnalyzer>();
builder.Services.AddScoped<IAutomationRuleEngine, AutomationRuleEngine>();
builder.Services.AddScoped<IPredictiveMaintenanceService, PredictiveMaintenanceService>();
builder.Services.AddScoped<IEnergyOptimizationService, EnergyOptimizationService>();
builder.Services.AddScoped<IMatterService, MatterService>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddScoped<IDataAggregationService, DataAggregationService>();
builder.Services.AddScoped<ISecurityManager, SecurityManager>();

// External Providers
builder.Services.AddSingleton<IMqttClientService, EnhancedMqttClientService>();
builder.Services.AddScoped<IWeatherDataProvider, OpenWeatherMapProvider>();
builder.Services.AddScoped<IUtilityPriceProvider, UtilityPriceProvider>();

// Device Adapters and Protocol Bridge
// Device Adapters and Protocol Bridge
builder.Services.AddScoped<IProtocolBridge, UnifiedProtocolBridge>();

// Phase 3: Universal Bridge & Shadow State
builder.Services.AddScoped<DeviceShadowService>();
builder.Services.AddScoped<UniversalDeviceBridge>();

// Register Adapters as IDeviceAdapter for the Bridge to consume
builder.Services.AddScoped<IDeviceAdapter, MatterDeviceAdapter>();
builder.Services.AddScoped<IDeviceAdapter, MqttDeviceAdapter>();
// Also register concrete types if needed elsewhere, or rely on interface
builder.Services.AddScoped<MatterDeviceAdapter>(); 
builder.Services.AddScoped<MqttDeviceAdapter>();

// Background Services
builder.Services.AddHostedService<DeviceDataCollectionService>();
builder.Services.AddHostedService<EnergyMonitoringBackgroundService>();
builder.Services.AddHostedService<MaintenanceSchedulingService>();
builder.Services.AddHostedService<AutomationRuleProcessorService>();
builder.Services.AddHostedService<EnergyOptimizationBackgroundService>();
builder.Services.AddHostedService<PredictiveMaintenanceBackgroundService>();
builder.Services.AddHostedService<MqttConnectionService>();

// Phase 3: BLE Simulation (Scoped or Singleton depending on state needs, Scoped is fine for generic service)
builder.Services.AddScoped<BleSimulationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("BlazorClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// Maps the Application layer Hub, not the API one (which was deleted)
app.MapHub<NexusHome.IoT.Application.Hubs.SmartDeviceStatusHub>("/hubs/deviceStatus");
app.MapHub<EnergyMonitoringHub>("/hubs/energy");
app.MapHub<SystemNotificationHub>("/hubs/notifications");

app.Run();

// Required for WebApplicationFactory integration testing
public partial class Program { }
