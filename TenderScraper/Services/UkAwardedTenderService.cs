using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenderScraper.Infrastructure;

namespace TenderScraper.Services;

/// <summary>
/// Fetches UK government awarded contract notices from the Contracts Finder OCDS API,
/// filters to CPV 72* (IT services) and persists each release as a UkAwardedTender row.
/// </summary>
public class UkAwardedTenderService
{
    private const string BaseUrl = "https://www.contractsfinder.service.gov.uk/Published/Notices/OCDS/Search";
    private const int PageSize = 100;

    private readonly HttpClient _http;
    private readonly TenderDbContext _db;
    private readonly ILogger<UkAwardedTenderService> _logger;

    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public UkAwardedTenderService(HttpClient http, TenderDbContext db, ILogger<UkAwardedTenderService> logger)
    {
        _http = http;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Fetches all CPV-72* award releases published on the given date,
    /// pages through the API, and upserts into the database.
    /// Returns (fetched, inserted, updated, skipped).
    /// </summary>
    public async Task<(int fetched, int inserted, int updated, int skipped)> IngestAsync(
        DateTime date, string cpvPrefix = "72", bool allCpv = false)
    {
        var publishedFrom = date.ToString("yyyy-MM-dd");
        var publishedTo   = date.AddDays(1).ToString("yyyy-MM-dd");

        _logger.LogInformation("Fetching UK award notices published {Date}", publishedFrom);

        var releases = await FetchAllReleasesAsync(publishedFrom, publishedTo);
        _logger.LogInformation("Total releases fetched: {Count}", releases.Count);

        // Filter to CPV prefix unless caller wants all
        if (!allCpv)
        {
            releases = releases
                .Where(r => GetCpvCode(r)?.StartsWith(cpvPrefix) == true)
                .ToList();
            _logger.LogInformation("After CPV {Prefix}* filter: {Count} releases", cpvPrefix, releases.Count);
        }

        int inserted = 0, updated = 0, skipped = 0;

        foreach (var release in releases)
        {
            try
            {
                var ocid = release["ocid"]?.GetValue<string>();
                if (string.IsNullOrEmpty(ocid)) { skipped++; continue; }

                var entity = Parse(release);

                var existing = await _db.UkAwardedTenders
                    .FirstOrDefaultAsync(u => u.Ocid == ocid);

                if (existing == null)
                {
                    _db.UkAwardedTenders.Add(entity);
                    inserted++;
                }
                else
                {
                    // Update all fields but preserve the original CreatedAt
                    entity.Id = existing.Id;
                    entity.CreatedAt = existing.CreatedAt;
                    _db.Entry(existing).CurrentValues.SetValues(entity);
                    updated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to parse/save release: {Error}", ex.Message);
                skipped++;
            }
        }

        await _db.SaveChangesAsync();
        return (releases.Count, inserted, updated, skipped);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task<List<JsonObject>> FetchAllReleasesAsync(string publishedFrom, string publishedTo)
    {
        var all = new List<JsonObject>();

        // First page URL — the API uses cursor-based pagination (OCDS pagination extension).
        // Subsequent pages are provided via links.next in the response; we follow that URL
        // directly rather than incrementing a page counter.
        string? nextUrl = $"{BaseUrl}?publishedFrom={publishedFrom}&publishedTo={publishedTo}" +
                          $"&stages=award&limit={PageSize}";

        var seen = new HashSet<string>(); // guard against the API repeating the same URL

        while (nextUrl != null)
        {
            if (!seen.Add(nextUrl))
            {
                _logger.LogWarning("Pagination loop detected — same URL returned twice. Stopping.");
                break;
            }

            _logger.LogDebug("GET {Url}", nextUrl);
            var response = await _http.GetAsync(nextUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HTTP {Status} from Contracts Finder API", response.StatusCode);
                break;
            }

            var root = await response.Content.ReadFromJsonAsync<JsonObject>(_jsonOpts);
            if (root == null) break;

            var releases = root["releases"]?.AsArray();
            if (releases == null || releases.Count == 0) break;

            foreach (var r in releases)
            {
                if (r is JsonObject obj) all.Add(obj);
            }

            _logger.LogDebug("{Count} releases on this page (total so far: {Total})",
                releases.Count, all.Count);

            // Follow links.next if present — absence means this is the last page
            nextUrl = root["links"]?["next"]?.GetValue<string>();
        }

        return all;
    }

    private static UkAwardedTender Parse(JsonObject r)
    {
        var tender  = r["tender"]  as JsonObject;
        var buyer   = r["buyer"]   as JsonObject;
        var parties = r["parties"] as JsonArray;
        var awards  = r["awards"]  as JsonArray;

        // ── Buyer party ─────────────────────────────────────────────────
        var buyerId = buyer?["id"]?.GetValue<string>();
        var buyerParty = parties?
            .OfType<JsonObject>()
            .FirstOrDefault(p => p["id"]?.GetValue<string>() == buyerId
                              || (p["roles"] as JsonArray)?.GetValues<string>().Contains("buyer") == true);
        var buyerAddr    = buyerParty?["address"]    as JsonObject;
        var buyerContact = buyerParty?["contactPoint"] as JsonObject;

        // ── Primary award ───────────────────────────────────────────────
        var award = awards?.OfType<JsonObject>().FirstOrDefault();
        var awardPeriod = award?["contractPeriod"] as JsonObject;
        var awardDocs   = award?["documents"] as JsonArray;

        // Award notice URL (documentType = "awardNotice")
        var noticeUrl = awardDocs?.OfType<JsonObject>()
            .FirstOrDefault(d => d["documentType"]?.GetValue<string>() == "awardNotice")?
            ["url"]?.GetValue<string>();
        // Fallback: first doc URL
        noticeUrl ??= awardDocs?.OfType<JsonObject>().FirstOrDefault()?["url"]?.GetValue<string>();

        // ── All suppliers across all awards ─────────────────────────────
        var allSupplierNames = awards?.OfType<JsonObject>()
            .SelectMany(a => (a["suppliers"] as JsonArray)?.OfType<JsonObject>() ?? [])
            .Select(s => s["name"]?.GetValue<string>())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList() ?? [];

        var allSupplierIds = awards?.OfType<JsonObject>()
            .SelectMany(a => (a["suppliers"] as JsonArray)?.OfType<JsonObject>() ?? [])
            .Select(s => s["id"]?.GetValue<string>())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList() ?? [];

        // Scale of the first supplier party with role=supplier
        var firstSupplierParty = parties?.OfType<JsonObject>()
            .FirstOrDefault(p => (p["roles"] as JsonArray)?.GetValues<string>().Contains("supplier") == true);
        var supplierScale = firstSupplierParty?["details"]?["scale"]?.GetValue<string>();

        // ── Delivery location (first item, first address) ───────────────
        var firstItem    = (tender?["items"] as JsonArray)?.OfType<JsonObject>().FirstOrDefault();
        var firstAddr    = (firstItem?["deliveryAddresses"] as JsonArray)?.OfType<JsonObject>().FirstOrDefault();

        // ── Additional CPV codes ────────────────────────────────────────
        var addCpvs = (tender?["additionalClassifications"] as JsonArray)?
            .OfType<JsonObject>()
            .Select(c => c["id"]?.GetValue<string>())
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList() ?? [];

        return new UkAwardedTender
        {
            Ocid      = r["ocid"]?.GetValue<string>() ?? "",
            ReleaseId = r["id"]?.GetValue<string>()   ?? "",
            ReleaseDate = ParseUtc(r["date"]?.GetValue<string>()),

            // Tender
            Title       = tender?["title"]?.GetValue<string>(),
            Description = tender?["description"]?.GetValue<string>(),
            CpvCode     = tender?["classification"]?["id"]?.GetValue<string>(),
            CpvDescription = tender?["classification"]?["description"]?.GetValue<string>(),
            AdditionalCpvCodes = addCpvs.Count > 0 ? string.Join(",", addCpvs) : null,
            ProcurementMethod  = tender?["procurementMethod"]?.GetValue<string>(),
            ProcurementMethodDetails = tender?["procurementMethodDetails"]?.GetValue<string>(),
            MainProcurementCategory  = tender?["mainProcurementCategory"]?.GetValue<string>(),
            TenderValueAmount   = ParseDecimal(tender?["value"]?["amount"]),
            TenderValueCurrency = tender?["value"]?["currency"]?.GetValue<string>(),
            SuitableSme  = tender?["suitability"]?["sme"]?.GetValue<bool>(),
            SuitableVcse = tender?["suitability"]?["vcse"]?.GetValue<bool>(),
            TenderDeadline      = ParseUtc(tender?["tenderPeriod"]?["endDate"]?.GetValue<string>()),
            TenderContractStart = ParseUtc(tender?["contractPeriod"]?["startDate"]?.GetValue<string>()),
            TenderContractEnd   = ParseUtc(tender?["contractPeriod"]?["endDate"]?.GetValue<string>()),

            // Delivery
            DeliveryRegion     = firstAddr?["region"]?.GetValue<string>(),
            DeliveryPostalCode = firstAddr?["postalCode"]?.GetValue<string>(),
            DeliveryCountry    = firstAddr?["countryName"]?.GetValue<string>(),

            // Buyer
            BuyerName          = buyerParty?["name"]?.GetValue<string>() ?? buyer?["name"]?.GetValue<string>(),
            BuyerStreetAddress = buyerAddr?["streetAddress"]?.GetValue<string>(),
            BuyerLocality      = buyerAddr?["locality"]?.GetValue<string>(),
            BuyerPostalCode    = buyerAddr?["postalCode"]?.GetValue<string>(),
            BuyerCountry       = buyerAddr?["countryName"]?.GetValue<string>(),
            BuyerContactName   = buyerContact?["name"]?.GetValue<string>(),
            BuyerContactEmail  = buyerContact?["email"]?.GetValue<string>(),
            BuyerContactPhone  = buyerContact?["telephone"]?.GetValue<string>(),

            // Award
            AwardId            = award?["id"]?.GetValue<string>(),
            AwardStatus        = award?["status"]?.GetValue<string>(),
            AwardDate          = ParseUtc(award?["date"]?.GetValue<string>()),
            AwardDatePublished = ParseUtc(award?["datePublished"]?.GetValue<string>()),
            AwardValueAmount   = ParseDecimal(award?["value"]?["amount"]),
            AwardValueCurrency = award?["value"]?["currency"]?.GetValue<string>(),
            AwardContractStart = ParseUtc(awardPeriod?["startDate"]?.GetValue<string>()),
            AwardContractEnd   = ParseUtc(awardPeriod?["endDate"]?.GetValue<string>()),

            // Suppliers
            SupplierNames = allSupplierNames.Count > 0 ? string.Join("; ", allSupplierNames) : null,
            SupplierIds   = allSupplierIds.Count > 0   ? string.Join("; ", allSupplierIds)   : null,
            SupplierScale = supplierScale,

            NoticeUrl = noticeUrl,
            RawJson   = r.ToJsonString(),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string? GetCpvCode(JsonObject release) =>
        (release["tender"] as JsonObject)?["classification"]?["id"]?.GetValue<string>();

    private static DateTime? ParseUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTimeOffset.TryParse(value, null,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var dto))
            return dto.UtcDateTime;
        return null;
    }

    private static decimal? ParseDecimal(JsonNode? node)
    {
        if (node == null) return null;
        try { return node.GetValue<decimal>(); } catch { return null; }
    }
}

