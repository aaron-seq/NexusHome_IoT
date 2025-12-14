using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NexusHome.IoT.Application.DTOs;
using NexusHome.IoT.Core.Domain;
using NexusHome.IoT.Infrastructure.Configuration;
using NexusHome.IoT.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NexusHome.IoT.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SmartHomeDbContext _context;
    private readonly JwtAuthenticationSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(SmartHomeDbContext context, JwtAuthenticationSettings jwtSettings, ILogger<AuthController> logger)
    {
        _context = context;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        // Simple password hashing/checking (In production, use BCrypt or similar)
        // For existing users seeded or created, we assume plain text or simple hash match for this demo refactor
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user {Username}", request.Username);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var token = GenerateJwtToken(user);

        return Ok(new LoginResponseDto
        {
            Token = token,
            Username = user.Username,
            Role = user.Role,
            Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
        });
    }

    private bool VerifyPassword(string inputPassword, string storedHash)
    {
        // simplistic placeholder: exact match or simple hash check
        // In reality, this should be: BCrypt.Verify(inputPassword, storedHash)
        return inputPassword == storedHash; 
    }

    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("id", user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
