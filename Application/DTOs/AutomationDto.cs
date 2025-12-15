using System.ComponentModel.DataAnnotations;

namespace NexusHome.IoT.Application.DTOs;

/// <summary>
/// Automation rule information
/// </summary>
public class AutomationRuleDto
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public bool IsEnabled { get; set; }
    
    public string TriggerCondition { get; set; } = string.Empty;
    
    public string ActionCommand { get; set; } = string.Empty;
    
    public int Priority { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? LastExecuted { get; set; }
}

/// <summary>
/// Request to create a new automation rule
/// </summary>
public class CreateAutomationRuleRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public string TriggerCondition { get; set; } = string.Empty;
    
    [Required]
    public string ActionCommand { get; set; } = string.Empty;
    
    [Range(1, 10)]
    public int Priority { get; set; } = 5;
    
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Request to update an existing automation rule
/// </summary>
public class UpdateAutomationRuleRequest
{
    [StringLength(100, MinimumLength = 3)]
    public string? Name { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public string? TriggerCondition { get; set; }
    
    public string? ActionCommand { get; set; }
    
    [Range(1, 10)]
    public int? Priority { get; set; }
    
    public bool? IsEnabled { get; set; }
}
