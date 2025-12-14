using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.API.Hubs;
using NexusHome.IoT.API.Middleware;
using NexusHome.IoT.Core.Services;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Configuration;
using NexusHome.IoT.Infrastructure.Data;
using NexusHome.IoT.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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
builder.Services.AddSignalR();
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
builder.Services.AddScoped<IPredictiveMaintenanceEngine, PredictiveMaintenanceEngine>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddScoped<IDataAggregationService, DataAggregationService>();
builder.Services.AddScoped<ISecurityManager, SecurityManager>();

// External Providers
builder.Services.AddSingleton<IMqttClientService, EnhancedMqttClientService>();
builder.Services.AddScoped<IWeatherDataProvider, OpenWeatherMapProvider>();
builder.Services.AddScoped<IUtilityPriceProvider, UtilityPriceProvider>();

// Background Services
builder.Services.AddHostedService<DeviceDataCollectionService>();
builder.Services.AddHostedService<EnergyMonitoringBackgroundService>();
builder.Services.AddHostedService<MaintenanceSchedulingService>();
builder.Services.AddHostedService<AutomationRuleProcessorService>();
builder.Services.AddHostedService<EnergyOptimizationBackgroundService>();
builder.Services.AddHostedService<MqttConnectionService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SmartDeviceStatusHub>("/hubs/deviceStatus");
app.MapHub<EnergyMonitoringHub>("/hubs/energy");
app.MapHub<SystemNotificationHub>("/hubs/notifications");

app.Run();
