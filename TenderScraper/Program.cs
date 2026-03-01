using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TenderScraper.Models;
using TenderScraper.Services;
using TenderScraper.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// Program.cs - CLI Mode Support
var builder = Host.CreateApplicationBuilder(args);

// Ensure user secrets are loaded in all environments (not just Development)
builder.Configuration.AddUserSecrets<Program>();

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
builder.Services.AddScoped<WeeklyBriefService>();

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
else if (args.Length > 0 && args[0] == "brief")
{
    await RunWeeklyBriefAsync(host, args);
}
else if (args.Length > 0 && args[0] == "retro")
{
    // Retro-fill NoticeStatus from existing RawXml in the database
    await RunRetroFillNoticeStatusAsync(host);
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
                if (tender.ProcedureType != null)
                {
                    // Pass buyer name so the main analysis translates it in one shot
                    analysis = await translate.GetSmartSummaryAsync(tender.ProcedureType, tender.Description, tender.BuyerName);
                    analysis.PrintAnalysis();
                }
            }

            // BuyerNameEn: from AI analysis if available, otherwise a separate cheap translation call
            string? buyerNameEn = null;
            if (!string.IsNullOrEmpty(analysis?.Metadata.BuyerNameEn))
            {
                buyerNameEn = analysis.Metadata.BuyerNameEn;
            }
            else
            {
                try
                {
                    buyerNameEn = await translate.TranslateBuyerNameAsync(tender.BuyerName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ⚠️  Could not translate buyer name: {ex.Message}");
                    buyerNameEn = tender.BuyerName;
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
                // SourceId includes the version so every notice version gets its own row:
                //   "{OCID}-{LotId}-v{version}"  e.g. "abc123-LOT-0001-v01"
                var version = tender.NoticeVersion ?? "01";
                var sourceId = $"{tender.OCID}-{tender.LotId}-v{version}";

                var existingRow = await dbContext.Tenders
                    .FirstOrDefaultAsync(t => t.SourceId == sourceId);

                if (existingRow == null)
                {
                    var dbTender = new Tender
                    {
                        SourceId = sourceId,
                        NoticeId = tender.OCID,          // bare GUID — shared across all versions
                        NoticeVersion = version,
                        LotId = tender.LotId,
                        NoticeType = tender.NoticeType,
                        NoticeStatus = tender.NoticeStatus,

                        // Title & description (German original; English filled by AI if run)
                        TitleDe = tender.Title,
                        TitleEn = analysis?.Metadata.Title,
                        DescriptionDe = tender.Description,
                        DescriptionEn = analysis != null ? string.Join(" ", analysis.Metadata.Summary) : null,

                        // Buyer
                        BuyerName = tender.BuyerName,
                        BuyerNameEn = buyerNameEn,
                        BuyerWebsite = tender.BuyerWebsite,
                        BuyerContactEmail = tender.BuyerContactEmail,
                        BuyerContactPhone = tender.BuyerContactPhone,
                        BuyerCity = tender.BuyerCity,
                        BuyerCountry = tender.BuyerCountry,

                        // Classification
                        CpvCode = tender.CpvCode,
                        AdditionalCpvCodes = tender.AdditionalCpvCodes,
                        NutsCode = tender.NutsCode,
                        ContractNature = tender.ContractNature,
                        ProcedureType = tender.ProcedureType,

                        // Financials
                        ValueEuro = tender.EstimatedValue,

                        // Dates
                        PublicationDate = tender.PublicationDate == default ? null : tender.PublicationDate,
                        SubmissionDeadline = tender.SubmissionDeadline,
                        Deadline = tender.SubmissionDeadline,
                        ContractStartDate = tender.ContractStartDate,
                        ContractEndDate = tender.ContractEndDate,

                        // Portal
                        BuyerPortalUrl = tender.BuyerPortalUrl,

                        // AI outputs
                        SuitabilityScore = analysis?.DecisionSupport.AccessibilityScore != null
                            ? (decimal?)analysis.DecisionSupport.AccessibilityScore
                            : null,
                        EnglishExecutiveSummary = analysis != null ? string.Join(" ", analysis.Metadata.Summary) : null,
                        FatalFlaws = analysis != null ? string.Join("; ", analysis.RedFlags.FatalFlaws) : null,
                        HardCertifications = analysis != null ? string.Join("; ", analysis.Technical.Certifications) : null,
                        TechStack = analysis != null ? string.Join(", ", analysis.Technical.TechStack) : null,
                        EligibilityProbability = null,

                        // Raw data
                        RawXml = tender.RawXml,
                        CreatedAt = DateTime.UtcNow
                    };

                    dbContext.Tenders.Add(dbTender);
                    await dbContext.SaveChangesAsync();
                    Console.WriteLine($"    💾 Saved {tender.NoticeStatus} v{version} (ID: {dbTender.TenderID})");

                    // Backfill BuyerNameEn onto all sibling rows (same NoticeId) that are missing it
                    if (!string.IsNullOrEmpty(buyerNameEn))
                    {
                        var siblings = await dbContext.Tenders
                            .Where(t => t.NoticeId == tender.OCID && string.IsNullOrEmpty(t.BuyerNameEn) && t.TenderID != dbTender.TenderID)
                            .ToListAsync();
                        if (siblings.Any())
                        {
                            foreach (var s in siblings) s.BuyerNameEn = buyerNameEn;
                            await dbContext.SaveChangesAsync();
                            Console.WriteLine($"    🔗 Backfilled BuyerNameEn on {siblings.Count} sibling row(s)");
                        }
                    }
                }
                else
                {
                    // Same version already ingested — only update BuyerNameEn if missing
                    if (string.IsNullOrEmpty(existingRow.BuyerNameEn) && !string.IsNullOrEmpty(buyerNameEn))
                    {
                        existingRow.BuyerNameEn = buyerNameEn;
                        await dbContext.SaveChangesAsync();
                        Console.WriteLine($"    ℹ️  Updated BuyerNameEn on existing row (ID: {existingRow.TenderID})");
                    }
                    else
                    {
                        Console.WriteLine($"    ℹ️  Already in database — {tender.NoticeStatus} v{version} (ID: {existingRow.TenderID})");
                    }
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

static async Task RunWeeklyBriefAsync(IHost host, string[] args)
{
    // ── Argument parsing ─────────────────────────────────────────────────
    // --sector=IT        (default: IT)
    // --cpv=72           (default: 72 = IT services)
    // --top=10           (default: 10)
    // --output=brief.pdf (default: TenderBrief_<date>.pdf in current dir)
    // --week=2026-02-24  (start of week, default: last Monday)

    var sectorArg  = args.FirstOrDefault(a => a.StartsWith("--sector="))?.Split('=')[1] ?? "IT Services";
    var cpvArg     = args.FirstOrDefault(a => a.StartsWith("--cpv="))?.Split('=')[1]    ?? "72";
    var topArg     = args.FirstOrDefault(a => a.StartsWith("--top="))?.Split('=')[1]    ?? "10";
    var outputArg  = args.FirstOrDefault(a => a.StartsWith("--output="))?.Split('=')[1];
    var weekArg    = args.FirstOrDefault(a => a.StartsWith("--week="))?.Split('=')[1];

    int topN = int.TryParse(topArg, out var t) ? t : 10;

    // Default week: last Monday → Sunday
    var today     = DateOnly.FromDateTime(DateTime.Today);
    var dayOfWeek = (int)today.DayOfWeek;
    var lastMonday = today.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));
    if (weekArg != null && DateOnly.TryParse(weekArg, out var parsedWeek))
        lastMonday = parsedWeek;
    var weekEnd = lastMonday.AddDays(6);

    var outputPath = outputArg
        ?? Path.Combine(Directory.GetCurrentDirectory(),
            $"TenderBrief_{sectorArg.Replace(" ", "_")}_{lastMonday:yyyy-MM-dd}.pdf");

    Console.WriteLine("╔════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     TenderEngine - Weekly Intelligence Brief          ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine($"Sector:     {sectorArg} (CPV {cpvArg}*)");
    Console.WriteLine($"Week:       {lastMonday:dd MMM yyyy} – {weekEnd:dd MMM yyyy}");
    Console.WriteLine($"Top:        {topN} tenders");
    Console.WriteLine($"Output:     {outputPath}");
    Console.WriteLine();

    using var scope = host.Services.CreateScope();
    var dbContext    = scope.ServiceProvider.GetRequiredService<TenderDbContext>();
    var briefService = scope.ServiceProvider.GetRequiredService<WeeklyBriefService>();

    // ── Query tenders from DB ────────────────────────────────────────────
    Console.WriteLine("🔍 Querying database...");

    var weekStartDt = lastMonday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    var weekEndDt   = weekEnd.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

    var tenders = await dbContext.Tenders
        .Where(t =>
            t.CpvCode != null && t.CpvCode.StartsWith(cpvArg) &&
            t.PublicationDate >= weekStartDt &&
            t.PublicationDate <= weekEndDt &&
            (t.NoticeStatus == "Active" || t.NoticeStatus == "Amendment" || t.NoticeStatus == null))
        .OrderByDescending(t => t.SuitabilityScore ?? 0)
        .ThenByDescending(t => t.ValueEuro ?? 0)
        .Take(topN)
        .ToListAsync();

    if (!tenders.Any())
    {
        // Widen search to last 30 days if the week has no data yet
        Console.WriteLine($"⚠️  No tenders found for that week. Widening to last 30 days...");
        var cutoff = DateTime.UtcNow.AddDays(-30);
        tenders = await dbContext.Tenders
            .Where(t => t.CpvCode != null && t.CpvCode.StartsWith(cpvArg) && t.PublicationDate >= cutoff &&
                        (t.NoticeStatus == "Active" || t.NoticeStatus == "Amendment" || t.NoticeStatus == null))
            .OrderByDescending(t => t.SuitabilityScore ?? 0)
            .ThenByDescending(t => t.ValueEuro ?? 0)
            .Take(topN)
            .ToListAsync();

        if (!tenders.Any())
        {
            Console.WriteLine("❌ No tenders found for this sector. Run 'ingest' first to populate data.");
            return;
        }

        // Adjust week label to reflect actual data range
        var minDate = tenders.Min(t => t.PublicationDate ?? DateTime.UtcNow);
        var maxDate = tenders.Max(t => t.PublicationDate ?? DateTime.UtcNow);
        lastMonday = DateOnly.FromDateTime(minDate);
        weekEnd    = DateOnly.FromDateTime(maxDate);
        Console.WriteLine($"   Found {tenders.Count} tenders ({lastMonday:dd MMM} – {weekEnd:dd MMM yyyy})");
    }
    else
    {
        Console.WriteLine($"   ✓ Found {tenders.Count} tenders in database");
    }

    // ── Generate PDF ─────────────────────────────────────────────────────
    Console.WriteLine("📄 Generating PDF...");
    try
    {
        briefService.GenerateBrief(tenders, sectorArg, lastMonday, weekEnd, outputPath);
        Console.WriteLine();
        Console.WriteLine($"✅ Brief generated: {outputPath}");
        Console.WriteLine();

        // Print a quick text summary to console too
        Console.WriteLine("TENDERS INCLUDED:");
        int i = 1;
        foreach (var tender in tenders)
        {
            var score = tender.SuitabilityScore.HasValue ? $"  Score: {tender.SuitabilityScore}/10" : "";
            var value = tender.ValueEuro.HasValue ? $"  Value: €{tender.ValueEuro.Value:N0}" : "";
            Console.WriteLine($"  [{i++}] {tender.TitleEn ?? tender.TitleDe}{score}{value}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Failed to generate PDF: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"   {ex.InnerException.Message}");
    }
}

static async Task RunRetroFillNoticeStatusAsync(IHost host)
{
    Console.WriteLine("╔════════════════════════════════════════════════════════╗");
    Console.WriteLine("║   TenderEngine - Retro-fill NoticeStatus from RawXml  ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════╝");
    Console.WriteLine();

    using var scope = host.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TenderDbContext>();

    // Load all rows that have RawXml — we'll refresh status, NoticeId, NoticeVersion and SourceId format
    var allRows = await dbContext.Tenders
        .Where(t => t.RawXml != null)
        .ToListAsync();

    Console.WriteLine($"Found {allRows.Count} row(s) with RawXml to process.");

    if (allRows.Count == 0)
    {
        Console.WriteLine("✅ Nothing to do.");
        return;
    }

    int updatedStatus = 0, updatedMeta = 0, skipped = 0, errored = 0;
    var statusCounts = new Dictionary<string, int> { ["Active"] = 0, ["Amendment"] = 0, ["Awarded"] = 0 };

    foreach (var row in allRows)
    {
        if (string.IsNullOrEmpty(row.RawXml))
        {
            skipped++;
            continue;
        }

        try
        {
            // ── 1. Derive NoticeStatus ────────────────────────────────
            var status = GermanEformsProvider.DetermineNoticeStatusFromXml(row.RawXml);
            if (row.NoticeStatus != status)
            {
                row.NoticeStatus = status;
                updatedStatus++;
            }
            statusCounts[status] = statusCounts.GetValueOrDefault(status) + 1;

            // ── 2. Backfill NoticeVersion from XML VersionID ──────────
            if (string.IsNullOrEmpty(row.NoticeVersion))
            {
                var version = GermanEformsProvider.ExtractVersionFromXml(row.RawXml);
                row.NoticeVersion = version;
                updatedMeta++;
            }

            // ── 3. Backfill NoticeId (bare GUID = everything before the last '-vXX') ─
            if (string.IsNullOrEmpty(row.NoticeId))
            {
                // Try to derive NoticeId from existing SourceId.
                // Old format: "{OCID}-{LotId}"  e.g. "abc123-LOT-0001"
                // New format: "{OCID}-{LotId}-v{version}"
                // OCID is the notice GUID — extract from RawXml for accuracy.
                row.NoticeId = GermanEformsProvider.ExtractNoticeIdFromXml(row.RawXml);
                updatedMeta++;
            }

            // ── 4. Migrate SourceId to versioned format if not already ─
            // Old: "abc123-LOT-0001"   New: "abc123-LOT-0001-v01"
            if (!row.SourceId.Contains("-v") && !string.IsNullOrEmpty(row.NoticeId) && !string.IsNullOrEmpty(row.LotId))
            {
                var newSourceId = $"{row.NoticeId}-{row.LotId}-v{row.NoticeVersion ?? "01"}";
                row.SourceId = newSourceId;
                updatedMeta++;
            }
        }
        catch
        {
            row.NoticeStatus ??= "Active";
            errored++;
        }
    }

    await dbContext.SaveChangesAsync();

    Console.WriteLine();
    Console.WriteLine($"✅ Done.");
    Console.WriteLine($"   Rows processed:         {allRows.Count}");
    Console.WriteLine($"   NoticeStatus updated:   {updatedStatus}");
    Console.WriteLine($"   Metadata backfilled:    {updatedMeta}");
    Console.WriteLine($"   Active:                 {statusCounts["Active"]}");
    Console.WriteLine($"   Amendment:              {statusCounts["Amendment"]}");
    Console.WriteLine($"   Awarded:                {statusCounts["Awarded"]}");
    if (skipped > 0)  Console.WriteLine($"   Skipped (no RawXml):    {skipped}");
    if (errored > 0)  Console.WriteLine($"   Defaulted (parse err):  {errored}");
    Console.WriteLine();
}

