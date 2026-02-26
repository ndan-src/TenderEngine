namespace TenderScraper.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Design-time factory for TenderDbContext.
/// This allows EF Core tools (like dotnet ef) to create the DbContext for migrations.
/// </summary>
public class TenderDbContextFactory : IDesignTimeDbContextFactory<TenderDbContext>
{
    public TenderDbContext CreateDbContext(string[] args)
    {
        // Layer configuration sources in priority order (last one wins):
        //   1. appsettings.json  (base / placeholder values)
        //   2. Environment variable TENDERENGINE_POSTGRES  (CI / server override)
        //   3. User secrets (developer machine override)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()          // allows CONNECTIONSTRINGS__POSTGRES env var
            .AddUserSecrets(typeof(TenderDbContextFactory).Assembly, optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Postgres' not found. Add it to appsettings.json, " +
                "user secrets, or the CONNECTIONSTRINGS__POSTGRES environment variable.");
        }

        // Detect if a placeholder value leaked through (helps diagnose stale user secrets)
        if (connectionString.Contains("your-azure-db") || connectionString.Contains("placeholder"))
        {
            throw new InvalidOperationException(
                "The Postgres connection string looks like a placeholder: " +
                $"'{connectionString.Substring(0, Math.Min(60, connectionString.Length))}'\n" +
                "This is probably a stale value in your user secrets overriding appsettings.json.\n" +
                "Run: dotnet user-secrets remove \"ConnectionStrings:Postgres\"");
        }

        Console.WriteLine($"[Design-Time] Using connection string: {connectionString.Substring(0, Math.Min(40, connectionString.Length))}...");

        var optionsBuilder = new DbContextOptionsBuilder<TenderDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new TenderDbContext(optionsBuilder.Options);
    }
}

