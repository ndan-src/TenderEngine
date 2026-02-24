using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TenderScraper.Models;
using TenderScraper.Services;

// Program.cs - CLI Mode Support
var builder = Host.CreateApplicationBuilder(args);

// 1. Register Configurations
builder.Services.Configure<TenderFilterOptions>(
    builder.Configuration.GetSection("TenderFilter"));

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
    var injest = scope.ServiceProvider.GetRequiredService<IngestionOrchestrator>();
    var translate = scope.ServiceProvider.GetRequiredService<TranslationService>();
    
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

            if (!noAi)
            {
                //await injest.ProcessHighValueTender(tender);
                if (tender.ProcedureType != null)
                {
                    var summary = await translate.GetSmartSummaryAsync(tender.ProcedureType, tender.Description);
                    summary.PrintAnalysis();
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
            Console.WriteLine();
            
            index++;
            if (index >= 4)
            {
                Console.WriteLine("********************   ...");
                break; // Show only top 1 for brevity
            }
            
        }
    }
    else
    {
        Console.WriteLine("⚠️  No high-value tenders found for this date.");
        Console.WriteLine("   Try adjusting filters in appsettings.json or checking a different date.");
    }
    
    Console.WriteLine();
    Console.WriteLine("✓ Ingestion complete!");
}
