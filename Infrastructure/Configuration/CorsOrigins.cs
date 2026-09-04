using Microsoft.Extensions.Configuration;

namespace NexusHome.IoT.Infrastructure.Configuration;

/// <summary>
/// Resolves the allowed CORS origins from configuration.
/// </summary>
public static class CorsOrigins
{
    /// <summary>
    /// Reads <paramref name="key"/> as either a JSON array (appsettings) or a
    /// comma-separated scalar (environment variable), falling back to
    /// <paramref name="fallback"/> when nothing is configured.
    /// </summary>
    /// <remarks>
    /// Environment variables cannot express an array without the indexed
    /// <c>__0</c>/<c>__1</c> suffixes, so <c>Cors__AllowedOrigins=https://a,https://b</c>
    /// binds to a scalar, and <c>Get&lt;string[]&gt;()</c> returns null for a
    /// scalar — so the configured production origins were silently replaced by
    /// the localhost fallback, with no error anywhere. The scalar form is split
    /// here instead.
    /// </remarks>
    public static string[] Resolve(IConfiguration configuration, string key, string[] fallback)
    {
        var section = configuration.GetSection(key);

        var fromArray = section.GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();

        if (fromArray.Length > 0)
        {
            return fromArray;
        }

        var fromScalar = section.Value?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];

        return fromScalar.Length > 0 ? fromScalar : fallback;
    }
}
