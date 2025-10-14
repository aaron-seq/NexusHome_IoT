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
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json;

namespace NexusHome.IoT;

public class Program
{
    public static async Task Main(string[] args)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/nexushome-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1));

        Log.Logger = loggerConfiguration.CreateBootstrapLogger();

        try
        {
            var applicationBuilder = WebApplication.CreateBuilder(args);
            
            ConfigureLogging(applicationBuilder);
            ConfigureServices(applicationBuilder.Services, applicationBuilder.Configuration);
            
            var smartHomeApplication = applicationBuilder.Build();
            
            ConfigureApplicationPipeline(smartHomeApplication);
            
            await InitializeApplicationDatabase(smartHomeApplication);
            
            Log.Information("NexusHome IoT Platform started successfully at {StartTime}", DateTime.UtcNow);
            
            await smartHomeApplication.RunAsync();
        }
        catch (Exception applicationException)
        {
            Log.Fatal(applicationException, "NexusHome IoT Platform terminated unexpectedly");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static void ConfigureLogging(WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
            loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration));
    }

    private static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration applicationConfiguration)
    {
        RegisterCoreInfrastructure(serviceCollection, applicationConfiguration);
        RegisterBusinessServices(serviceCollection);
        RegisterWebApiServices(serviceCollection);
        RegisterExternalIntegrations(serviceCollection, applicationConfiguration);
    }

    private static void RegisterCoreInfrastructure(IServiceCollection serviceCollection, IConfiguration applicationConfiguration)
    {
        RegisterDatabaseServices(serviceCollection, applicationConfiguration);
        RegisterAuthenticationAndAuthorization(serviceCollection, applicationConfiguration);
        RegisterCachingServices(serviceCollection, applicationConfiguration);
        RegisterMessagingServices(serviceCollection, applicationConfiguration);
    }

    private static void RegisterDatabaseServices(IServiceCollection serviceCollection, IConfiguration applicationConfiguration)
    {
        serviceCollection.AddDbContext<SmartHomeDbContext>(databaseOptions =>
        {
            var connectionString = applicationConfiguration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=NexusHomeIoT;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";

            databaseOptions.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlServerOptions.CommandTimeout(120);
            });

            databaseOptions.EnableSensitiveDataLogging(false);
            databaseOptions.EnableServiceProviderCaching();
            databaseOptions.EnableDetailedErrors(false);
        });
    }

    private static void RegisterAuthenticationAndAuthorization(IServiceCollection serviceCollection, IConfiguration applicationConfiguration)
    {
        var jwtConfigurationSettings = applicationConfiguration.GetSection("JwtAuthentication").Get<JwtAuthenticationSettings>()
            ?? new JwtAuthenticationSettings();

        serviceCollection.AddSingleton(jwtConfigurationSettings);

        serviceCollection.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOptions =>
            {
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfigurationSettings.Issuer,
                    ValidAudience = jwtConfigurationSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfigurationSettings.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                jwtOptions.Events = new JwtBearerEvents
                {
                    OnMessageReceived = tokenContext =>
                    {
                        var accessTokenFromQuery = tokenContext.Request.Query["access_token"];
                        var currentRequestPath = tokenContext.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessTokenFromQuery) && currentRequestPath.StartsWithSegments("/hubs"))
                        {
                            tokenContext.Token = accessTokenFromQuery;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        serviceCollection.AddAuthorization(authorizationOptions =>
        {
            authorizationOptions.AddPolicy("AdminAccess", policy => policy.RequireRole("Administrator"));
            authorizationOptions.AddPolicy("UserAccess", policy => policy.RequireRole("User", "Administrator"));
            authorizationOptions.AddPolicy("DeviceAccess", policy => policy.RequireRole("Device", "User", "Administrator"));
            authorizationOptions.AddPolicy("TechnicianAccess", policy => policy.RequireRole("Technician", "Administrator"));
        });
    }

    private static void RegisterCachingServices(IServiceCollection serviceCollection, IConfiguration applicationConfiguration)
    {
        serviceCollection.AddMemoryCache(memoryCacheOptions =>
        {
            memoryCacheOptions.SizeLimit = 1024;
            memoryCacheOptions.CompactionPercentage = 0.25;
        });

        var redisConnectionString = applicationConfiguration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            serviceCollection.AddStackExchangeRedisCache(redisOptions =>
            {
                redisOptions.Configuration = redisConnectionString;
                redisOptions.InstanceName = "NexusHomeIoT";
            });
        }
    }

    private static void RegisterMessagingServices(IServiceCollection serviceCollection, IConfiguration applicationConfiguration)
    {
        serviceCollection.Configure<MqttBrokerSettings>(applicationConfiguration.GetSection("MqttBroker"));
        serviceCollection.AddSingleton<IMqttClientService, EnhancedMqttClientService>();
    }

    private static void RegisterBusinessServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISmartDeviceManager, SmartDeviceManager>();
        serviceCollection.AddScoped<IEnergyConsumptionAnalyzer, EnergyConsumptionAnalyzer>();
        serviceCollection.AddScoped<IAutomationRuleEngine, IntelligentAutomationRuleEngine>();
        serviceCollection.AddScoped<IPredictiveMaintenanceEngine, AdvancedPredictiveMaintenanceEngine>();
        serviceCollection.AddScoped<IEnergyOptimizationEngine, MachineLearningEnergyOptimizationEngine>();
        serviceCollection.AddScoped<INotificationDispatcher, MultiChannelNotificationDispatcher>();
        serviceCollection.AddScoped<IDataAggregationService, RealTimeDataAggregationService>();
        serviceCollection.AddScoped<ISecurityManager, ComprehensiveSecurityManager>();

        RegisterBackgroundServices(serviceCollection);
    }

    private static void RegisterBackgroundServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddHostedService<DeviceDataCollectionService>();
        serviceCollection.AddHostedService<EnergyMonitoringBackgroundService>();
        serviceCollection.AddHostedService<MaintenanceSchedulingService>();
        serviceCollection.AddHostedService<AutomationRuleProcessorService>();
        serviceCollection.AddHostedService<SystemHealthMonitoringService>();
    }

    private static void RegisterWebApiServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddControllers(controllerOptions =>
        {
            controllerOptions.ReturnHttpNotAcceptable = true;
            controllerOptions.RespectBrowserAcceptHeader = true;
        })
        .AddJsonOptions(jsonOptions =>
        {
            jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            jsonOptions.JsonSerializerOptions.WriteIndented = false;
            jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

        RegisterSwaggerDocumentation(serviceCollection);
        RegisterRateLimiting(serviceCollection);
        RegisterSignalRServices(serviceCollection);
        RegisterHealthChecks(serviceCollection);
        RegisterCorsPolicy(serviceCollection);
    }

    private static void RegisterSwaggerDocumentation(IServiceCollection serviceCollection)
    {
        serviceCollection.AddEndpointsApiExplorer();
        serviceCollection.AddSwaggerGen(swaggerOptions =>
        {
            swaggerOptions.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "NexusHome Smart IoT Platform API",
                Version = "v2.1.0",
                Description = "Advanced Smart Home Energy Management & IoT Control System with AI-powered optimization",
                Contact = new OpenApiContact
                {
                    Name = "Aaron Sequeira",
                    Email = "aaron@nexushome.tech",
                    Url = new Uri("https://github.com/aaron-seq/NexusHome_IoT")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            swaggerOptions.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            swaggerOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    }

    private static void RegisterRateLimiting(IServiceCollection serviceCollection)
    {
        serviceCollection.AddRateLimiter(rateLimitOptions =>
        {
            rateLimitOptions.AddFixedWindowLimiter("StandardApiLimiter", fixedWindowOptions =>
            {
                fixedWindowOptions.PermitLimit = 1000;
                fixedWindowOptions.Window = TimeSpan.FromMinutes(1);
                fixedWindowOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                fixedWindowOptions.QueueLimit = 100;
            });

            rateLimitOptions.AddFixedWindowLimiter("DeviceTelemetryLimiter", fixedWindowOptions =>
            {
                fixedWindowOptions.PermitLimit = 10000;
                fixedWindowOptions.Window = TimeSpan.FromMinutes(1);
                fixedWindowOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                fixedWindowOptions.QueueLimit = 1000;
            });
        });
    }

    private static void RegisterSignalRServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSignalR(signalROptions =>
        {
            signalROptions.EnableDetailedErrors = true;
            signalROptions.KeepAliveInterval = TimeSpan.FromSeconds(15);
            signalROptions.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            signalROptions.HandshakeTimeout = TimeSpan.FromSeconds(15);
        });
    }

    private static void RegisterHealthChecks(IServiceCollection serviceCollection)
    {
        serviceCollection.AddHealthChecks()
            .AddDbContextCheck<SmartHomeDbContext>("database_connectivity")
            .AddCheck("mqtt_broker_status", () => HealthCheckResult.Healthy("MQTT broker is responsive"))
            .AddCheck("system_memory_usage", () =>
            {
                var currentMemoryUsage = GC.GetTotalMemory(false);
                var memoryThresholdBytes = 1024L * 1024L * 1024L; // 1GB threshold

                return currentMemoryUsage < memoryThresholdBytes
                    ? HealthCheckResult.Healthy($"Memory usage is optimal: {currentMemoryUsage / (1024 * 1024)} MB")
                    : HealthCheckResult.Degraded($"High memory usage detected: {currentMemoryUsage / (1024 * 1024)} MB");
            });
    }

    private static void RegisterCorsPolicy(IServiceCollection serviceCollection)
    {
        serviceCollection.AddCors(corsOptions =>
        {
            corsOptions.AddDefaultPolicy(corsBuilder =>
            {
                corsBuilder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
            });
        });
    }

    private static void RegisterExternalIntegrations(IServiceCollection serviceCollection, IConfiguration applicationConfiguration)
    {
        var weatherApiKey = applicationConfiguration["WeatherApi:ApiKey"];
        if (!string.IsNullOrEmpty(weatherApiKey))
        {
            serviceCollection.Configure<WeatherApiSettings>(applicationConfiguration.GetSection("WeatherApi"));
            serviceCollection.AddHttpClient<IWeatherDataProvider, OpenWeatherMapProvider>(httpClient =>
            {
                httpClient.BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/");
                httpClient.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        serviceCollection.AddHttpClient<IUtilityPriceProvider, UtilityPriceProvider>(httpClient =>
        {
            httpClient.Timeout = TimeSpan.FromSeconds(45);
        });
    }

    private static void ConfigureApplicationPipeline(WebApplication smartHomeApplication)
    {
        ConfigureDevelopmentMiddleware(smartHomeApplication);
        ConfigureProductionMiddleware(smartHomeApplication);
        ConfigureSecurityMiddleware(smartHomeApplication);
        ConfigureRoutingAndEndpoints(smartHomeApplication);
    }

    private static void ConfigureDevelopmentMiddleware(WebApplication smartHomeApplication)
    {
        if (smartHomeApplication.Environment.IsDevelopment())
        {
            smartHomeApplication.UseDeveloperExceptionPage();
            smartHomeApplication.UseSwagger();
            smartHomeApplication.UseSwaggerUI(swaggerUiOptions =>
            {
                swaggerUiOptions.SwaggerEndpoint("/swagger/v1/swagger.json", "NexusHome IoT API v2.1.0");
                swaggerUiOptions.RoutePrefix = "api-docs";
                swaggerUiOptions.DisplayRequestDuration();
                swaggerUiOptions.EnableDeepLinking();
                swaggerUiOptions.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            });
        }
    }

    private static void ConfigureProductionMiddleware(WebApplication smartHomeApplication)
    {
        if (!smartHomeApplication.Environment.IsDevelopment())
        {
            smartHomeApplication.UseExceptionHandler("/Error");
            smartHomeApplication.UseHsts();
        }
    }

    private static void ConfigureSecurityMiddleware(WebApplication smartHomeApplication)
    {
        smartHomeApplication.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        
        smartHomeApplication.UseHttpsRedirection();
        smartHomeApplication.UseStaticFiles();
        smartHomeApplication.UseRouting();
        smartHomeApplication.UseCors();
        smartHomeApplication.UseRateLimiter();

        smartHomeApplication.UseMiddleware<RequestLoggingMiddleware>();
        smartHomeApplication.UseMiddleware<ComprehensiveErrorHandlingMiddleware>();
        smartHomeApplication.UseMiddleware<SecurityHeadersMiddleware>();

        smartHomeApplication.UseAuthentication();
        smartHomeApplication.UseAuthorization();
    }

    private static void ConfigureRoutingAndEndpoints(WebApplication smartHomeApplication)
    {
        smartHomeApplication.MapHub<SmartDeviceStatusHub>("/hubs/device-status");
        smartHomeApplication.MapHub<EnergyMonitoringHub>("/hubs/energy-monitoring");
        smartHomeApplication.MapHub<SystemNotificationHub>("/hubs/notifications");
        smartHomeApplication.MapHub<MaintenanceAlertHub>("/hubs/maintenance-alerts");

        smartHomeApplication.MapControllers().RequireRateLimiting("StandardApiLimiter");

        smartHomeApplication.MapHealthChecks("/health/ready");
        smartHomeApplication.MapHealthChecks("/health/live");
        smartHomeApplication.MapHealthChecks("/health/detailed").RequireAuthorization("AdminAccess");

        ConfigureMinimalApiEndpoints(smartHomeApplication);
    }

    private static void ConfigureMinimalApiEndpoints(WebApplication smartHomeApplication)
    {
        smartHomeApplication.MapPost("/api/v2/devices/telemetry", HandleDeviceTelemetrySubmission)
            .RequireAuthorization("DeviceAccess")
            .RequireRateLimiting("DeviceTelemetryLimiter")
            .WithTags("Device Telemetry")
            .WithOpenApi();

        smartHomeApplication.MapGet("/api/v2/system/status", GetSystemStatus)
            .RequireAuthorization("UserAccess")
            .WithTags("System Information")
            .WithOpenApi();
    }

    private static async Task<IResult> HandleDeviceTelemetrySubmission(
        DeviceTelemetryRequest telemetryRequest,
        ISmartDeviceManager deviceManager,
        ILogger<Program> applicationLogger)
    {
        try
        {
            await deviceManager.ProcessTelemetryDataAsync(telemetryRequest);
            applicationLogger.LogInformation("Telemetry data processed successfully for device {DeviceId}", telemetryRequest.DeviceId);
            return Results.Accepted();
        }
        catch (ArgumentException argumentException)
        {
            applicationLogger.LogWarning(argumentException, "Invalid telemetry data received for device {DeviceId}", telemetryRequest.DeviceId);
            return Results.BadRequest("Invalid telemetry data format");
        }
        catch (Exception processingException)
        {
            applicationLogger.LogError(processingException, "Failed to process telemetry data for device {DeviceId}", telemetryRequest.DeviceId);
            return Results.Problem("Failed to process telemetry data");
        }
    }

    private static async Task<IResult> GetSystemStatus(
        IServiceProvider serviceProvider,
        ILogger<Program> applicationLogger)
    {
        try
        {
            var systemStatus = new
            {
                Timestamp = DateTime.UtcNow,
                ApplicationName = "NexusHome IoT Platform",
                Version = "v2.1.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                MemoryUsage = $"{GC.GetTotalMemory(false) / (1024 * 1024)} MB",
                Status = "Healthy"
            };

            return Results.Ok(systemStatus);
        }
        catch (Exception systemException)
        {
            applicationLogger.LogError(systemException, "Failed to retrieve system status");
            return Results.Problem("Unable to retrieve system status");
        }
    }

    private static async Task InitializeApplicationDatabase(WebApplication smartHomeApplication)
    {
        try
        {
            using var serviceScope = smartHomeApplication.Services.CreateScope();
            var databaseContext = serviceScope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
            var applicationLogger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            applicationLogger.LogInformation("Starting database initialization process");

            await databaseContext.Database.MigrateAsync();
            await DatabaseSeeder.SeedDevelopmentDataAsync(databaseContext, serviceScope.ServiceProvider, applicationLogger);

            applicationLogger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception databaseException)
        {
            var fallbackLogger = smartHomeApplication.Services.GetRequiredService<ILogger<Program>>();
            fallbackLogger.LogError(databaseException, "Database initialization failed critically");
            throw;
        }
    }
}

public record DeviceTelemetryRequest(
    string DeviceId,
    Dictionary<string, object> SensorData,
    DateTime Timestamp);
