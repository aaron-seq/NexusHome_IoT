using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusHome.IoT.Core.Domain;
using NexusHome.IoT.Infrastructure.Data;
using NexusHome.IoT.Application.DTOs;
using System.Text.Json;

namespace NexusHome.IoT.Controllers;

/// <summary>
/// Controller for managing automation rules
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "UserAccess")]
[Produces("application/json")]
public class AutomationController : ControllerBase
{
    private readonly SmartHomeDbContext _context;
    private readonly ILogger<AutomationController> _logger;

    public AutomationController(SmartHomeDbContext context, ILogger<AutomationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all automation rules
    /// </summary>
    /// <returns>List of automation rules</returns>
    /// <response code="200">Returns the list of rules</response>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(IEnumerable<AutomationRuleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AutomationRuleDto>>> GetRules()
    {
        try
        {
            var rules = await _context.AutomationRules
                .AsNoTracking()
                .OrderByDescending(r => r.PriorityLevel)
                .Select(r => new AutomationRuleDto
                {
                    Id = r.Id,
                    Name = r.RuleName,
                    Description = r.Description,
                    IsEnabled = r.IsActive,
                    TriggerCondition = r.TriggerCondition,
                    ActionCommand = r.ActionCommand,
                    Priority = r.PriorityLevel,
                    CreatedAt = r.CreatedAt,
                    LastExecuted = r.LastExecutionTimestamp
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} automation rules", rules.Count);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving automation rules");
            return StatusCode(500, new { error = "Failed to retrieve rules" });
        }
    }

    /// <summary>
    /// Get a specific automation rule
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <returns>Automation rule details</returns>
    /// <response code="200">Returns the rule</response>
    /// <response code="404">Rule not found</response>
    [HttpGet("rules/{id}")]
    [ProducesResponseType(typeof(AutomationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AutomationRuleDto>> GetRule(int id)
    {
        try
        {
            var rule = await _context.AutomationRules
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rule == null)
            {
                return NotFound(new { error = $"Rule {id} not found" });
            }

            var ruleDto = new AutomationRuleDto
            {
                Id = rule.Id,
                Name = rule.RuleName,
                Description = rule.Description,
                IsEnabled = rule.IsActive,
                TriggerCondition = rule.TriggerCondition,
                ActionCommand = rule.ActionCommand,
                Priority = rule.PriorityLevel,
                CreatedAt = rule.CreatedAt,
                LastExecuted = rule.LastExecutionTimestamp
            };

            return Ok(ruleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rule {Id}", id);
            return StatusCode(500, new { error = "Failed to retrieve rule" });
        }
    }

    /// <summary>
    /// Create a new automation rule
    /// </summary>
    /// <param name="request">Rule details</param>
    /// <returns>Created rule</returns>
    /// <response code="201">Rule created successfully</response>
    /// <response code="400">Invalid rule data</response>
    [HttpPost("rules")]
    [ProducesResponseType(typeof(AutomationRuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AutomationRuleDto>> CreateRule([FromBody] CreateAutomationRuleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var rule = new IntelligentAutomationRule
            {
                RuleName = request.Name,
                Description = request.Description,
                TriggerCondition = request.TriggerCondition,
                ActionCommand = request.ActionCommand,
                PriorityLevel = request.Priority,
                IsActive = request.IsEnabled,
                ConditionsJson = JsonSerializer.Serialize(new { condition = request.TriggerCondition }),
                ActionsJson = JsonSerializer.Serialize(new { action = request.ActionCommand }),
                CreatedAt = DateTime.UtcNow
            };

            _context.AutomationRules.Add(rule);
            await _context.SaveChangesAsync();

            var ruleDto = new AutomationRuleDto
            {
                Id = rule.Id,
                Name = rule.RuleName,
                Description = rule.Description,
                IsEnabled = rule.IsActive,
                TriggerCondition = rule.TriggerCondition,
                ActionCommand = rule.ActionCommand,
                Priority = rule.PriorityLevel,
                CreatedAt = rule.CreatedAt
            };

            _logger.LogInformation("Created automation rule {Id}: {Name}", rule.Id, rule.RuleName);

            return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, ruleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating automation rule");
            return StatusCode(500, new { error = "Failed to create rule" });
        }
    }

    /// <summary>
    /// Update an existing automation rule
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <param name="request">Updated rule details</param>
    /// <returns>Success response</returns>
    /// <response code="200">Rule updated successfully</response>
    /// <response code="404">Rule not found</response>
    /// <response code="400">Invalid update data</response>
    [HttpPut("rules/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRule(int id, [FromBody] UpdateAutomationRuleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var rule = await _context.AutomationRules.FirstOrDefaultAsync(r => r.Id == id);

            if (rule == null)
            {
                return NotFound(new { error = $"Rule {id} not found" });
            }

            // Update only provided fields
            if (request.Name != null) rule.RuleName = request.Name;
            if (request.Description != null) rule.Description = request.Description;
            if (request.TriggerCondition != null) rule.TriggerCondition = request.TriggerCondition;
            if (request.ActionCommand != null) rule.ActionCommand = request.ActionCommand;
            if (request.Priority.HasValue) rule.PriorityLevel = request.Priority.Value;
            if (request.IsEnabled.HasValue) rule.IsActive = request.IsEnabled.Value;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated automation rule {Id}", id);

            return Ok(new { message = $"Rule {id} updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule {Id}", id);
            return StatusCode(500, new { error = "Failed to update rule" });
        }
    }

    /// <summary>
    /// Delete an automation rule
    /// </summary>
    /// <param name="id">Rule ID</param>
    /// <returns>Success response</returns>
    /// <response code="204">Rule deleted successfully</response>
    /// <response code="404">Rule not found</response>
    [HttpDelete("rules/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule(int id)
    {
        try
        {
            var rule = await _context.AutomationRules.FirstOrDefaultAsync(r => r.Id == id);

            if (rule == null)
            {
                return NotFound(new { error = $"Rule {id} not found" });
            }

            _context.AutomationRules.Remove(rule);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted automation rule {Id}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rule {Id}", id);
            return StatusCode(500, new { error = "Failed to delete rule" });
        }
    }
}
