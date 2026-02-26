using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TenderScraper.Models;
using TenderScraper.Services;
using TenderScraper.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// Program.cs - CLI Mode Support
var builder = Host.CreateApplicationBuilder(args);

// 1. Register Configurations
builder.Services.Configure<TenderFilterOptions>(
    builder.Configuration.GetSection("TenderFilter"));

// 1a. Register Database Context
var connectionString = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("❌ WARNING: Postgres connection string is not configured!");
}
else
{
    Console.WriteLine($"✅ Postgres connection string loaded: {connectionString.Substring(0, Math.Min(30, connectionString.Length))}...");
}

builder.Services.AddDbContext<TenderDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Register HTTP Client for Providers
builder.Services.AddHttpClient<GermanEformsProvider>();

// 2a. Register HTTP Client for AI Services
builder.Services.AddHttpClient<ILlmSummarizer, LlmSummarizer>();

// 3. Register Providers (EFORMS XML-based)
builder.Services.AddTransient<ITenderProvider, GermanEformsProvider>();
// builder.Services.AddTransient<ITenderProvider, ItalianTenderProvider>(); 
builder.Services.AddScoped<TenderUrlExtractor>();

builder.Services.AddScoped<TranslationService>();

// 4. Register Logic Services
builder.Services.AddSingleton<TenderFilterService>();
builder.Services.AddScoped<DeepAnalysisService>();
builder.Services.AddScoped<IngestionOrchestrator>();
builder.Services.AddScoped<TenderDocumentDownloader>();

var host = builder.Build();

// Apply database migrations automatically
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TenderDbContext>();
    try
    {
        Console.WriteLine("🔄 Applying database migrations...");
        Console.WriteLine($"   Connecting to: {connectionString?.Split(';')[0]}...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("✅ Database migrations applied successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error applying migrations: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
        }
        Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
        Console.WriteLine();
        Console.WriteLine("💡 Troubleshooting tips:");
        Console.WriteLine("   1. Check your internet connection");
        Console.WriteLine("   2. Verify the Supabase host is reachable: ping db.jlgdsruraoktgbfwvzce.supabase.co");
        Console.WriteLine("   3. Check if a firewall is blocking port 5432");
        Console.WriteLine("   4. Verify your Supabase credentials are correct");
        Console.WriteLine();
        Console.WriteLine("⚠️  Continuing without database migrations (some features may not work)...");
        // Don't exit - allow the app to continue for testing
        Environment.Exit(1);
    }
}

// Check for CLI mode
if (args.Length > 0 && args[0] == "ingest")
{
    // CLI Mode: Run ingestion once and exit
    await RunCliIngestionAsync(host, args);
}
else
{
    // Service Mode: Run as background worker
    builder.Services.AddHostedService<TenderIngestionWorker>();
    await host.RunAsync();
}

static async Task RunCliIngestionAsync(IHost host, string[] args)
{
    // Parse arguments
    DateTime targetDate = DateTime.Today.AddDays(-1); // Default: yesterday
    bool noAi = args.Contains("--no-ai");
    
    // Check for custom date: --date=2026-02-15
    var dateArg = args.FirstOrDefault(a => a.StartsWith("--date="));
    if (dateArg != null)
    {
        var dateStr = dateArg.Split('=')[1];
        if (DateTime.TryParse(dateStr, out var parsed))
            targetDate = parsed;
    }
    
    Console.WriteLine("╔════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     TenderEngine - German Procurement Ingestion       ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine($"Target Date: {targetDate:yyyy-MM-dd} ({targetDate:dddd})");
    Console.WriteLine($"AI Analysis: {(noAi ? "DISABLED" : "ENABLED")}");
    Console.WriteLine();
    
    using var scope = host.Services.CreateScope();
    var providers = scope.ServiceProvider.GetRequiredService<IEnumerable<ITenderProvider>>();
    var filterService = scope.ServiceProvider.GetRequiredService<TenderFilterService>();
    var translate = scope.ServiceProvider.GetRequiredService<TranslationService>();
    var dbContext = scope.ServiceProvider.GetRequiredService<TenderDbContext>();
    
    var allTenders = new List<RawTender>();
    var highValueTenders = new List<RawTender>();
    
    foreach (var provider in providers)
    {
        Console.WriteLine($"📡 Fetching from: {provider.ProviderName}...");
        
        try
        {
            var tenders = await provider.FetchLatestNoticesAsync(targetDate);
            var tenderList = tenders.ToList();
            
            Console.WriteLine($"   ✓ Retrieved {tenderList.Count} IT tenders (CPV 72*)");
            
            allTenders.AddRange(tenderList);
            
            // Apply filters
            var filtered = tenderList.Where(t => filterService.IsHighValue(t)).ToList();
            highValueTenders.AddRange(filtered);
            
            Console.WriteLine($"   ✓ {filtered.Count} high-value tenders after filtering");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   ✗ Inner Error: {ex.InnerException.Message}");
            }
            Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
        }
    }
    
    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine($"SUMMARY: {allTenders.Count} total tenders, {highValueTenders.Count} high-value");
    Console.WriteLine("═══════════════════════════════════════════════════════");
    Console.WriteLine();
    
    if (highValueTenders.Any())
    {
        Console.WriteLine("HIGH-VALUE TENDERS:");
        Console.WriteLine();
        
        int index = 1;
        foreach (var tender in highValueTenders.OrderByDescending(t => t.EstimatedValue ?? 0))
        {
            Console.WriteLine($"[{index}] {tender.Title}");
            Console.WriteLine($"    OCID:           {tender.OCID}");
            Console.WriteLine($"    Lot:            {tender.LotId}");
            Console.WriteLine($"    Procedure:      {tender.ProcedureType ?? "N/A"}");
            Console.WriteLine($"    Est. Value:     {(tender.EstimatedValue.HasValue ? $"€{tender.EstimatedValue.Value:N2}" : "Not specified")}");
            
            // Display buyer portal URL
            if (!string.IsNullOrEmpty(tender.BuyerPortalUrl))
            {
                Console.WriteLine($"    Portal URL:     {tender.BuyerPortalUrl}");
            }

            UnifiedTenderAnalysis? analysis = null;
            
            if (!noAi)
            {
                //await injest.ProcessHighValueTender(tender);
                if (tender.ProcedureType != null)
                {
                    analysis = await translate.GetSmartSummaryAsync(tender.ProcedureType, tender.Description);
                    analysis.PrintAnalysis();
                }
            }

            // Truncate description (handle empty descriptions)
            if (!string.IsNullOrEmpty(tender.Description))
            {
                var desc = tender.Description.Length > 150 
                    ? tender.Description.Substring(0, 150) + "..." 
                    : tender.Description;
                Console.WriteLine($"    Description:    {desc}");
            }
            else
            {
                Console.WriteLine($"    Description:    [No description available]");
            }
            
            // Save to database
            try
            {
                // Check if tender already exists (avoid duplicates)
                var existingTender = await dbContext.Tenders
                    .FirstOrDefaultAsync(t => t.SourceId == $"{tender.OCID}-{tender.LotId}");
                
                if (existingTender == null)
                {
                    var dbTender = new Tender
                    {
                        SourceId = $"{tender.OCID}-{tender.LotId}",
                        TitleDe = tender.Title,
                        TitleEn = analysis?.Metadata.Title,
                        BuyerName = "Unknown", // Extract from provider data if available
                        ValueEuro = tender.EstimatedValue,
                        Deadline = null, // Extract from provider data if available
                        SuitabilityScore = analysis?.DecisionSupport.AccessibilityScore != null 
                            ? (decimal?)analysis.DecisionSupport.AccessibilityScore 
                            : null,
                        RawXml = tender.RawXml, // Store the raw XML from EFORMS
                        EnglishExecutiveSummary = analysis != null ? string.Join(" ", analysis.Metadata.Summary) : null,
                        FatalFlaws = analysis != null ? string.Join("; ", analysis.RedFlags.FatalFlaws) : null,
                        HardCertifications = analysis != null ? string.Join("; ", analysis.Technical.Certifications) : null,
                        TechStack = analysis != null ? string.Join(", ", analysis.Technical.TechStack) : null,
                        EligibilityProbability = null, // Could be calculated from analysis
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    dbContext.Tenders.Add(dbTender);
                    await dbContext.SaveChangesAsync();
                    Console.WriteLine($"    💾 Saved to database (ID: {dbTender.TenderID})");
                }
                else
                {
                    Console.WriteLine($"    ℹ️  Already in database (ID: {existingTender.TenderID})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ❌ Error saving to database: {ex.Message}");
            }
            
            Console.WriteLine();
            
            index++;
            /*if (index >= 4)
            {
                Console.WriteLine("********************   ...");
                break; // Show only top 1 for brevity
            }*/
            
        }
    }
    else
    {
        Console.WriteLine("⚠️  No high-value tenders found for this date.");
        Console.WriteLine("   Try adjusting filters in appsettings.json or checking a different date.");
    }
    
    Console.WriteLine();
    
    // Database summary
    try
    {
        var totalInDb = await dbContext.Tenders.CountAsync();
        Console.WriteLine($"📊 Database Stats: {totalInDb} total tenders stored");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Could not retrieve database stats: {ex.Message}");
    }
    
    Console.WriteLine();
    Console.WriteLine("✓ Ingestion complete!");
}
