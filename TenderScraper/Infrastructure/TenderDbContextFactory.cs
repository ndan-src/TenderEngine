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
        // Build configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<TenderDbContextFactory>(optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Postgres' not found in appsettings.json or user secrets. " +
                "Please ensure your appsettings.json contains a valid ConnectionStrings:Postgres entry.");
        }

        Console.WriteLine($"[Design-Time] Using connection string: {connectionString.Substring(0, Math.Min(30, connectionString.Length))}...");

        var optionsBuilder = new DbContextOptionsBuilder<TenderDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new TenderDbContext(optionsBuilder.Options);
    }
}

