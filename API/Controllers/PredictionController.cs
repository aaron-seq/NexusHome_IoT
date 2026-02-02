using Microsoft.AspNetCore.Mvc;
using NexusHome.IoT.AI;

namespace NexusHome.IoT.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PredictionController : ControllerBase
{
    private readonly IPredictiveMaintenanceService _predictionService;

    public PredictionController(IPredictiveMaintenanceService predictionService)
    {
        _predictionService = predictionService;
    }

    [HttpGet("next-action/{deviceId}")]
    public async Task<ActionResult<ActionSuggestion>> GetNextAction(string deviceId)
    {
        // Map string ID to int for the service (simplification for prototype)
        // In real app, IDs would be consistent
        int id = deviceId.GetHashCode();
        
        var prediction = await _predictionService.PredictMaintenanceNeedAsync(id);
        
        var suggestion = new ActionSuggestion
        {
            DeviceId = deviceId,
            Confidence = (int)(prediction.FailureProbability * 100)
        };

        if (prediction.FailureProbability > 0.7) // High probability of failure
        {
            suggestion.Title = "Schedule Repair";
            suggestion.Icon = "🔧";
            suggestion.Reason = "Anomaly Detected (High Risk)";
            suggestion.ActionCommand = "ScheduleMaintenance";
        }
        else if (DateTime.UtcNow.Hour >= 18) // Evening context
        {
            suggestion.Title = "Turn On Lights";
            suggestion.Icon = "💡";
            suggestion.Reason = "Usually at 6:00 PM";
            suggestion.ActionCommand = "TurnOn";
            suggestion.Confidence = 85; 
        }
        else
        {
            suggestion.Title = "Optimize Energy";
            suggestion.Icon = "⚡";
            suggestion.Reason = "Idle Usage";
            suggestion.ActionCommand = "EnablePowerSave";
            suggestion.Confidence = 60;
        }

        return Ok(suggestion);
    }
}

public class ActionSuggestion
{
    public string DeviceId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Reason { get; set; } = "";
    public int Confidence { get; set; }
    public string ActionCommand { get; set; } = "";
}
