using System.Net.Http.Json;

namespace NexusHome.Web.Services;

/// <summary>
/// HTTP client service for communicating with the NexusHome API
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7000";
        _httpClient.BaseAddress = new Uri(_baseUrl);
    }

    // ===== Device Operations =====
    
    public async Task<List<DeviceDto>?> GetDevicesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<DeviceDto>>("api/devices");
        }
        catch (Exception)
        {
            return new List<DeviceDto>();
        }
    }

    public async Task<DeviceDto?> GetDeviceAsync(string deviceId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DeviceDto>($"api/devices/{deviceId}");
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> ToggleDeviceAsync(string deviceId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/devices/{deviceId}/toggle", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // ===== Energy Operations =====
    
    public async Task<EnergyDashboardDto?> GetEnergyDashboardAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EnergyDashboardDto>("api/energy/consumption");
        }
        catch (Exception)
        {
            return new EnergyDashboardDto();
        }
    }

    public async Task<CostAnalysisDto?> GetCostAnalysisAsync(DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var url = "api/energy/cost";
            if (from.HasValue || to.HasValue)
            {
                var @params = new List<string>();
                if (from.HasValue) @params.Add($"from={from.Value:yyyy-MM-dd}");
                if (to.HasValue) @params.Add($"to={to.Value:yyyy-MM-dd}");
                url += "?" + string.Join("&", @params);
            }
            return await _httpClient.GetFromJsonAsync<CostAnalysisDto>(url);
        }
        catch (Exception)
        {
            return null;
        }
    }

    // ===== Automation Operations =====
    
    public async Task<List<AutomationRuleDto>?> GetAutomationRulesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<AutomationRuleDto>>("api/automation/rules");
        }
        catch (Exception)
        {
            return new List<AutomationRuleDto>();
        }
    }

    public async Task<AutomationRuleDto?> GetAutomationRuleAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AutomationRuleDto>($"api/automation/rules/{id}");
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> ToggleAutomationRuleAsync(int id, bool enabled)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/automation/rules/{id}", 
                new { IsEnabled = enabled });
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

// ===== DTO Models =====

public class DeviceDto
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Room { get; set; }
    public decimal PowerRating { get; set; }
    public decimal CurrentPowerConsumption { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
}

public class EnergyDashboardDto
{
    public decimal TotalConsumption { get; set; }
    public decimal TotalCost { get; set; }
    public decimal SolarGeneration { get; set; }
    public decimal BatteryLevel { get; set; }
    public decimal CostSavings { get; set; }
    public decimal CarbonFootprint { get; set; }
    public List<DeviceConsumptionDto> TopConsumers { get; set; } = new();
}

public class DeviceConsumptionDto
{
    public string DeviceName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal PowerConsumption { get; set; }
    public decimal Cost { get; set; }
    public decimal Percentage { get; set; }
}

public class CostAnalysisDto
{
    public decimal TotalCost { get; set; }
    public decimal AverageDailyCost { get; set; }
    public decimal ProjectedMonthlyCost { get; set; }
    public List<DailyCostDto> DailyCosts { get; set; } = new();
}

public class DailyCostDto
{
    public DateTime Date { get; set; }
    public decimal Cost { get; set; }
    public decimal Consumption { get; set; }
}

public class AutomationRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TriggerCondition { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime? LastExecuted { get; set; }
    public int ExecutionCount { get; set; }
}
