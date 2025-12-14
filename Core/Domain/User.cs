using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NexusHome.IoT.Core.Domain;

public class User : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Role { get; set; } = "User";

    public string? Preferences { get; set; } // JSON user preferences

    public DateTime? LastLoginAt { get; set; }
}
