using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.Infrastructure.Data;
using NexusHome.IoT.Application.DTOs;

namespace NexusHome.IoT.Controllers;

/// <summary>
/// Controller for energy consumption analytics and cost analysis
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "UserAccess")]
[Produces("application/json")]
public class EnergyController : ControllerBase
{
    private readonly SmartHomeDbContext _context;
    private readonly ILogger<EnergyController> _logger;

    public EnergyController(SmartHomeDbContext context, ILogger<EnergyController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get total energy consumption summary for a period
    /// </summary>
    /// <param name="from">Start date (optional, defaults to 30 days ago)</param>
    /// <param name="to">End date (optional, defaults to now)</param>
    /// <returns>Energy consumption summary</returns>
    /// <response code="200">Returns consumption summary</response>
    [HttpGet("consumption")]
    [ProducesResponseType(typeof(EnergyConsumptionSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EnergyConsumptionSummaryDto>> GetConsumption(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var startDate = from ?? DateTime.UtcNow.AddDays(-30);
            var endDate = to ?? DateTime.UtcNow;

            var energyData = await _context.EnergyConsumptions
                .AsNoTracking()
                .Include(e => e.Device)
                .Where(e => e.MeasurementTimestamp >= startDate && e.MeasurementTimestamp <= endDate)
                .ToListAsync();

            var totalConsumption = energyData.Sum(e => e.PowerConsumptionKilowattHours);
            var avgConsumption = energyData.Any() ? energyData.Average(e => e.PowerConsumptionKilowattHours) : 0;
            var peakRecord = energyData.OrderByDescending(e => e.PowerConsumptionKilowattHours).FirstOrDefault();

            // Device breakdown
            var deviceBreakdown = energyData
                .GroupBy(e => new { e.Device.Id, e.Device.DeviceFriendlyName, e.Device.UniqueDeviceIdentifier })
                .Select(g => new DeviceConsumptionBreakdown
                {
                    DeviceId = g.Key.UniqueDeviceIdentifier,
                    DeviceName = g.Key.DeviceFriendlyName,
                    Consumption = g.Sum(e => e.PowerConsumptionKilowattHours),
                    Percentage = totalConsumption > 0 ? (g.Sum(e => e.PowerConsumptionKilowattHours) / totalConsumption) * 100 : 0
                })
                .OrderByDescending(d => d.Consumption)
                .ToList();

            var summary = new EnergyConsumptionSummaryDto
            {
                TotalConsumption = totalConsumption,
                AverageConsumption = avgConsumption,
                PeakConsumption = peakRecord?.PowerConsumptionKilowattHours ?? 0,
                PeakTime = peakRecord?.MeasurementTimestamp ?? DateTime.UtcNow,
                Period = new DatePeriod { From = startDate, To = endDate },
                Timestamp = DateTime.UtcNow,
                DeviceBreakdown = deviceBreakdown
            };

            _logger.LogInformation("Retrieved energy consumption summary for period {From} to {To}", startDate, endDate);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving energy consumption");
            return StatusCode(500, new { error = "Failed to retrieve energy consumption" });
        }
    }

    /// <summary>
    /// Get cost analysis for energy consumption
    /// </summary>
    /// <param name="from">Start date (optional, defaults to 30 days ago)</param>
    /// <param name="to">End date (optional, defaults to now)</param>
    /// <returns>Cost analysis with breakdown and suggestions</returns>
    /// <response code="200">Returns cost analysis</response>
    [HttpGet("cost")]
    [ProducesResponseType(typeof(EnergyCostAnalysisDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EnergyCostAnalysisDto>> GetCostAnalysis(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var startDate = from ?? DateTime.UtcNow.AddDays(-30);
            var endDate = to ?? DateTime.UtcNow;
            var costPerKwh = 0.12m; // Default rate

            var energyData = await _context.EnergyConsumptions
                .AsNoTracking()
                .Where(e => e.MeasurementTimestamp >= startDate && e.MeasurementTimestamp <= endDate)
                .ToListAsync();

            var totalCost = energyData.Sum(e => e.CostEstimate);
            var avgCostPerKwh = energyData.Any() ? totalCost / energyData.Sum(e => e.PowerConsumptionKilowattHours) : costPerKwh;

            // Daily cost breakdown
            var dailyCosts = energyData
                .GroupBy(e => e.MeasurementTimestamp.Date)
                .Select(g => new DailyCostBreakdown
                {
                    Date = g.Key,
                    Consumption = g.Sum(e => e.PowerConsumptionKilowattHours),
                    Cost = g.Sum(e => e.CostEstimate)
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Generate optimization suggestions
            var suggestions = new List<CostOptimizationSuggestion>
            {
                new CostOptimizationSuggestion
                {
                    Title = "Shift High-Power Usage",
                    Description = "Move energy-intensive tasks to off-peak hours (11 PM - 6 AM) to reduce costs",
                    EstimatedSavings = totalCost * 0.15m
                },
                new CostOptimizationSuggestion
                {
                    Title = "Optimize Thermostat Settings",
                    Description = "Adjust thermostat by 2°C to reduce heating/cooling costs",
                    EstimatedSavings = totalCost * 0.10m
                }
            };

            var analysis = new EnergyCostAnalysisDto
            {
                TotalCost = totalCost,
                AverageCostPerKwh = avgCostPerKwh,
                DailyCosts = dailyCosts,
                Period = new DatePeriod { From = startDate, To = endDate },
                Suggestions = suggestions
            };

            _logger.LogInformation("Retrieved cost analysis for period {From} to {To}", startDate, endDate);

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cost analysis");
            return StatusCode(500, new { error = "Failed to retrieve cost analysis" });
        }
    }

    /// <summary>
    /// Get energy usage forecast
    /// </summary>
    /// <param name="hours">Number of hours to forecast (default: 24, max: 168)</param>
    /// <returns>Predicted energy usage</returns>
    /// <response code="200">Returns energy forecast</response>
    [HttpGet("forecast")]
    [ProducesResponseType(typeof(EnergyForecastDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EnergyForecastDto>> GetEnergyForecast(
        [FromQuery] int hours = 24)
    {
        try
        {
            if (hours > 168) hours = 168; // Cap at 1 week
            if (hours < 1) hours = 24;

            // Get historical data for pattern analysis
            var historicalData = await _context.EnergyConsumptions
                .AsNoTracking()
                .Where(e => e.MeasurementTimestamp >= DateTime.UtcNow.AddDays(-7))
                .OrderBy(e => e.MeasurementTimestamp)
                .ToListAsync();

            var avgHourlyConsumption = historicalData.Any() 
                ? historicalData.Average(e => e.PowerConsumptionKilowattHours)
                : 1.5m;

            // Generate simple forecast (in production, use ML model)
            var forecast = new List<ForecastDataPoint>();
            var random = new Random();

            for (int i = 0; i < hours; i++)
            {
                var timestamp = DateTime.UtcNow.AddHours(i);
                var hourOfDay = timestamp.Hour;
                
                // Simple pattern: higher consumption during day (8-22), lower at night
                var timeFactor = hourOfDay >= 8 && hourOfDay <= 22 ? 1.3m : 0.7m;
                var predicted = avgHourlyConsumption * timeFactor;
                var variance = predicted * 0.15m;

                forecast.Add(new ForecastDataPoint
                {
                    Timestamp = timestamp,
                    PredictedConsumption = predicted,
                    LowerBound = predicted - variance,
                    UpperBound = predicted + variance
                });
            }

            var forecastDto = new EnergyForecastDto
            {
                Forecast = forecast,
                HoursAhead = hours,
                PredictedConsumption = forecast.Sum(f => f.PredictedConsumption),
                ConfidenceLevel = 0.75m, // 75% confidence
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Generated {Hours}-hour energy forecast", hours);

            return Ok(forecastDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating energy forecast");
            return StatusCode(500, new { error = "Failed to generate forecast" });
        }
    }
}
