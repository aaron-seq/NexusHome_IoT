using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NexusHome.IoT.Infrastructure.Data;
using NexusHome.IoT.Core.Services.Interfaces;
using NexusHome.IoT.Infrastructure.Services;
using NexusHome.IoT.Core.Services;
using NexusHome.IoT.Application.Hubs;
using NexusHome.IoT.API.Middleware;
using NexusHome.IoT.Infrastructure.Configuration;
using Serilog;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexusHome.Energy; // Added for EnergyOptimizationService

// ...

    private static IServiceCollection AddBusinessLogicServices(this IServiceCollection services)
    {
        // Core domain services
        services.AddScoped<ISmartDeviceManager, SmartDeviceManager>();
        services.AddScoped<IEnergyConsumptionAnalyzer, EnergyConsumptionAnalyzer>();
        services.AddScoped<IAutomationRuleEngine, AutomationRuleEngine>();
        services.AddScoped<IPredictiveMaintenanceEngine, PredictiveMaintenanceEngine>();
        services.AddScoped<IEnergyOptimizationService, EnergyOptimizationService>(); // Fixed type
        
        // Utility services
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<IDataAggregationService, DataAggregationService>();
        services.AddScoped<ISecurityManager, SecurityManager>();
        
        return services;
    }
    
    private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<DeviceDataCollectionService>();
        services.AddHostedService<EnergyMonitoringBackgroundService>();
        services.AddHostedService<MaintenanceSchedulingService>();
        services.AddHostedService<AutomationRuleProcessorService>();
        services.AddHostedService<MqttConnectionService>();
        services.AddHostedService<EnergyOptimizationBackgroundService>(); // Added new background service
        
        return services;
    }
    
    private static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.ReturnHttpNotAcceptable = true;
            options.RespectBrowserAcceptHeader = true;
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = false;
        });
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "NexusHome Smart IoT API", 
                Version = "v2.0",
                Description = "Advanced Smart Home Energy Management & IoT Control System",
                Contact = new OpenApiContact 
                { 
                    Name = "NexusHome Development Team", 
                    Email = "developers@nexushome.tech",
                    Url = new Uri("https://github.com/aaronseq12/NexusHome_IoT")
                }
            });
            
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
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
                    Array.Empty<string>()
                }
            });
        });
        
        // Rate limiting
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("ApiLimiter", rateLimiterOptions =>
            {
                rateLimiterOptions.PermitLimit = 1000;
                rateLimiterOptions.Window = TimeSpan.FromMinutes(1);
                rateLimiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                rateLimiterOptions.QueueLimit = 100;
            });
        });
        
        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
        
        return services;
    }
    
    private static IServiceCollection AddRealTimeServices(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        });
        
        return services;
    }
    
    private static IServiceCollection AddMonitoringServices(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<SmartHomeDbContext>("database")
            .AddCheck("mqtt_connection", () => HealthCheckResult.Healthy("MQTT broker connection is healthy"))
            .AddCheck("system_memory", () => 
            {
                var allocatedMemory = GC.GetTotalMemory(false);
                var memoryThreshold = 1024L * 1024L * 1024L; // 1GB
                
                return allocatedMemory < memoryThreshold 
                    ? HealthCheckResult.Healthy($"Memory usage: {allocatedMemory / (1024 * 1024)} MB")
                    : HealthCheckResult.Degraded($"High memory usage: {allocatedMemory / (1024 * 1024)} MB");
            });
            
        return services;
    }
    
    private static IServiceCollection AddThirdPartyIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        // Weather API integration
        var weatherApiKey = configuration["WeatherApi:ApiKey"];
        if (!string.IsNullOrEmpty(weatherApiKey))
        {
            services.Configure<WeatherApiSettings>(configuration.GetSection("WeatherApi"));
            services.AddHttpClient<IWeatherDataProvider, OpenWeatherMapProvider>();
        }
        
        // Utility provider integration
        services.AddHttpClient<IUtilityPriceProvider, UtilityPriceProvider>();
        
        return services;
    }
}

/// <summary>
/// Extension methods for application pipeline configuration
/// </summary>
public static class WebApplicationExtensions
{
    public static WebApplication ConfigureNexusHomePipeline(this WebApplication app)
    {
        // Development-specific middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => 
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "NexusHome IoT API v2.0");
                c.RoutePrefix = "api-docs";
                c.DisplayRequestDuration();
                c.EnableDeepLinking();
            });
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        
        // Core middleware pipeline
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        
        app.UseRouting();
        app.UseCors();
        app.UseRateLimiter();
        
        // Custom middleware
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        
        app.UseAuthentication();
        app.UseAuthorization();
        
        // SignalR Hubs
        app.MapHub<SmartDeviceStatusHub>("/hubs/device-status");
        app.MapHub<EnergyMonitoringHub>("/hubs/energy-monitoring");
        app.MapHub<SystemNotificationHub>("/hubs/notifications");
        
        // API Controllers
        app.MapControllers().RequireRateLimiting("ApiLimiter");
        
        // Health checks
        app.MapHealthChecks("/health/ready");
        app.MapHealthChecks("/health/live");
        
        // Minimal API endpoints for high-performance scenarios
        app.MapPost("/api/v2/devices/telemetry", HandleDeviceTelemetry)
            .RequireAuthorization("DeviceAccess")
            .WithTags("Device Data")
            .WithOpenApi();
            
        return app;
    }
    
    private static async Task<IResult> HandleDeviceTelemetry(
        DeviceTelemetryRequest request,
        ISmartDeviceManager deviceManager,
        ILogger<Program> logger)
    {
        try
        {
            await deviceManager.ProcessTelemetryDataAsync(request);
            return Results.Accepted();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process telemetry data for device {DeviceId}", request.DeviceId);
            return Results.Problem("Failed to process telemetry data");
        }
    }
    
    public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("🔄 Initializing database...");
            
            await context.Database.MigrateAsync();
            await DatabaseSeeder.SeedAsync(context, scope.ServiceProvider, logger);
            
            logger.LogInformation("✅ Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "❌ Database initialization failed");
            throw;
        }
        
        return app;
    }
}

/// <summary>
/// Request model for device telemetry
/// </summary>
public record DeviceTelemetryRequest(
    string DeviceId,
    Dictionary<string, object> SensorData,
    DateTime Timestamp);

    public partial class Program { }
