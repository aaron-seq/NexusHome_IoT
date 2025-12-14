using System.ComponentModel.DataAnnotations;

namespace NexusHome.IoT.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for JWT authentication in the IoT platform
/// Provides secure token-based authentication for API access and device communication
/// </summary>
public class JwtAuthenticationSettings
{
    /// <summary>
    /// Secret key used for JWT token signing and verification
    /// Must be at least 256 bits (32 characters) for HS256 algorithm
    /// </summary>
    [Required]
    [MinLength(32, ErrorMessage = "JWT secret key must be at least 32 characters for security")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT token issuer identifier
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Issuer { get; set; } = "NexusHome.IoT";

    /// <summary>
    /// JWT token audience identifier
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Audience { get; set; } = "NexusHome.Clients";

    /// <summary>
    /// Access token expiration time in minutes
    /// </summary>
    [Range(5, 10080)] // 5 minutes to 1 week
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration time in days
    /// </summary>
    [Range(1, 365)] // 1 day to 1 year
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Whether to validate the token issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the token audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate the token lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Whether to validate the issuer signing key
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance in minutes to account for server time differences
    /// </summary>
    [Range(0, 30)]
    public int ClockSkewMinutes { get; set; } = 5;
}
