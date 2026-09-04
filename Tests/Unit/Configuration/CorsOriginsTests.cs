using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NexusHome.IoT.Infrastructure.Configuration;
using Xunit;

namespace NexusHome.IoT.Tests.Unit.Configuration;

public class CorsOriginsTests
{
    private static readonly string[] Fallback = ["http://localhost:5000"];

    private static IConfiguration Build(params (string Key, string Value)[] entries) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e =>
                new KeyValuePair<string, string?>(e.Key, e.Value)))
            .Build();

    [Fact]
    public void Resolve_ReadsTheIndexedArrayFormUsedByAppsettings()
    {
        var configuration = Build(
            ("Cors:AllowedOrigins:0", "https://one.example"),
            ("Cors:AllowedOrigins:1", "https://two.example"));

        CorsOrigins.Resolve(configuration, "Cors:AllowedOrigins", Fallback)
            .Should().Equal("https://one.example", "https://two.example");
    }

    // Cors__AllowedOrigins=a,b is the only form an environment variable can take
    // without index suffixes. Get<string[]>() returns null for a scalar, so the
    // configured production origins used to be replaced by the localhost
    // fallback and the real frontend was CORS-blocked with no error logged.
    [Fact]
    public void Resolve_SplitsTheCommaSeparatedFormUsedByEnvironmentVariables()
    {
        var configuration = Build(
            ("Cors:AllowedOrigins", "https://one.example, https://two.example"));

        CorsOrigins.Resolve(configuration, "Cors:AllowedOrigins", Fallback)
            .Should().Equal("https://one.example", "https://two.example");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ,  ")]
    public void Resolve_FallsBackWhenNothingUsableIsConfigured(string? configured)
    {
        var configuration = configured is null
            ? Build()
            : Build(("Cors:AllowedOrigins", configured));

        CorsOrigins.Resolve(configuration, "Cors:AllowedOrigins", Fallback)
            .Should().Equal(Fallback);
    }
}
