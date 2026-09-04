using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NexusHome.IoT.Infrastructure.Data;

/// <summary>
/// Design-time factory used by the EF Core tools (<c>dotnet ef</c>).
///
/// The application selects its provider from configuration at runtime, and the
/// committed connection strings are empty, so the tools cannot build the
/// context from the host. Migrations are authored against SQL Server because
/// that is the production provider; the SQLite and InMemory providers used for
/// local development create their schema with EnsureCreated instead.
///
/// The connection string below is never opened — <c>migrations add</c> only
/// needs a provider to generate SQL. Commands that reach the database
/// (<c>database update</c>) read ConnectionStrings__DefaultConnection from the
/// environment.
/// </summary>
public class SmartHomeDbContextFactory : IDesignTimeDbContextFactory<SmartHomeDbContext>
{
    private const string DesignTimeFallbackConnection =
        "Server=localhost;Database=NexusHomeIoT;Trusted_Connection=true;TrustServerCertificate=true";

    public SmartHomeDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DesignTimeFallbackConnection;

        var optionsBuilder = new DbContextOptionsBuilder<SmartHomeDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new SmartHomeDbContext(optionsBuilder.Options);
    }
}
