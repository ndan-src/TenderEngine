using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using TenderScraper.Models;

namespace TenderScraper.Services;

/// <summary>
/// Provides German tender data from EFORMS XML files (eforms-de-2.x, above-threshold EU notices).
/// </summary>
public class GermanEformsProvider : ITenderProvider
{
    public string ProviderName => "Germany_EFORMS_XML";
    private readonly HttpClient _httpClient;
    private readonly ILogger<GermanEformsProvider> _logger;

    // Standard UBL namespaces
    private static readonly XNamespace CacNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace CbcNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    // eForms extension namespaces
    private static readonly XNamespace EfacNs = "http://data.europa.eu/p27/eforms-ubl-extension-aggregate-components/1";
    private static readonly XNamespace EfbcNs = "http://data.europa.eu/p27/eforms-ubl-extension-basic-components/1";

    public GermanEformsProvider(HttpClient httpClient, ILogger<GermanEformsProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<RawTender>> FetchLatestNoticesAsync(DateTime date)
    {
        string dateStr = date.ToString("yyyy-MM-dd");
        _logger.LogInformation("Fetching EFORMS XML for date: {Date}", dateStr);

        var response = await _httpClient.GetAsync($"https://oeffentlichevergabe.de/api/notice-exports?pubDay={dateStr}&format=eforms.zip");

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to download EFORMS ZIP: HTTP {response.StatusCode}");

        using var stream = await response.Content.ReadAsStreamAsync();
        using var archive = new ZipArchive(stream);

        _logger.LogInformation("📦 ZIP contains {Count} entries", archive.Entries.Count);

        var results = new List<RawTender>();
        int processedCount = 0;
        int itTenderCount = 0;

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                continue;

            // Skip integer-named files (legacy eforms-sdk-0.1 / below-threshold VOB notices)
            // GUID files: {uuid}-{version}.xml  → 6 segments when split by '-'
            // Integer files: {integer}-{version}.xml → 2 segments
            var segments = Path.GetFileNameWithoutExtension(entry.Name).Split('-');
            if (segments.Length < 6)
                continue;

            try
            {
                using var xmlStream = entry.Open();
                var xmlDoc = await XDocument.LoadAsync(xmlStream, LoadOptions.None, CancellationToken.None);

                var tender = ParseXmlTender(xmlDoc);
                if (tender != null)
                {
                    processedCount++;

                    // Filter for IT tenders (CPV code starting with 72)
                    if (tender.CpvCode != null && tender.CpvCode.StartsWith("72"))
                    {
                        results.Add(tender);
                        itTenderCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to parse XML file {FileName}: {Error}", entry.FullName, ex.Message);
            }
        }

        _logger.LogInformation("✓ Processed {Processed} XML files, found {IT} IT tenders (CPV 72*)",
            processedCount, itTenderCount);

        return results;
    }

    private RawTender? ParseXmlTender(XDocument xmlDoc)
    {
        try
        {
            var root = xmlDoc.Root;
            if (root == null) return null;

            // Resolve namespace prefixes from the document itself
            var cbcNs = root.GetNamespaceOfPrefix("cbc") ?? CbcNs;
            var cacNs = root.GetNamespaceOfPrefix("cac") ?? CacNs;
            var efacNs = root.GetNamespaceOfPrefix("efac") ?? EfacNs;
            var efbcNs = root.GetNamespaceOfPrefix("efbc") ?? EfbcNs;

            // ── Notice ID ────────────────────────────────────────────────
            var noticeId = root.Descendants(cbcNs + "ID")
                               .FirstOrDefault(e => e.Attribute("schemeName")?.Value == "notice-id")?.Value
                           ?? root.Element(cbcNs + "ID")?.Value;
            if (string.IsNullOrEmpty(noticeId)) return null;

            // ── Notice metadata ──────────────────────────────────────────
            var noticeType = root.Element(cbcNs + "NoticeTypeCode")?.Value;
            var noticeVersion = root.Element(cbcNs + "VersionID")?.Value?.Trim() ?? "01";

            var issueDateStr = root.Element(cbcNs + "IssueDate")?.Value;
            var issueDate = ParseUtc(issueDateStr) ?? DateTime.UtcNow;

            // ── Procedure type (BT-105) ──────────────────────────────────
            var procedureType = root.Descendants(cbcNs + "ProcedureCode")
                                    .FirstOrDefault(e => e.Attribute("listName")?.Value == "procurement-procedure-type")?.Value ?? "";

            // ── Buyer resolution ─────────────────────────────────────────
            // The ContractingParty/Party references an ORG-xxxx id.
            // We look up that org in the efac:Organizations section.
            string? buyerOrgRef = root.Descendants(cacNs + "ContractingParty")
                .FirstOrDefault()
                ?.Descendants(cacNs + "Party").FirstOrDefault()
                ?.Descendants(cacNs + "PartyIdentification").FirstOrDefault()
                ?.Element(cbcNs + "ID")?.Value;

            // Find the matching organization element
            var orgs = root.Descendants(efacNs + "Organization").ToList();
            XElement? buyerOrg = null;
            if (buyerOrgRef != null)
            {
                buyerOrg = orgs.FirstOrDefault(o =>
                    o.Descendants(cacNs + "PartyIdentification")
                     .Any(pi => pi.Element(cbcNs + "ID")?.Value == buyerOrgRef));
            }
            // Fallback: first organisation
            buyerOrg ??= orgs.FirstOrDefault();

            var buyerCompany = buyerOrg?.Element(efacNs + "Company");
            var buyerName = buyerCompany?.Descendants(cacNs + "PartyName")
                                         .FirstOrDefault()
                                         ?.Element(cbcNs + "Name")?.Value;
            var buyerWebsite = buyerCompany?.Element(cbcNs + "WebsiteURI")?.Value;
            var buyerContact = buyerCompany?.Element(cacNs + "Contact");
            var buyerEmail = buyerContact?.Element(cbcNs + "ElectronicMail")?.Value;
            var buyerPhone = buyerContact?.Element(cbcNs + "Telephone")?.Value;
            var buyerAddress = buyerCompany?.Element(cacNs + "PostalAddress");
            var buyerCity = buyerAddress?.Element(cbcNs + "CityName")?.Value;
            var buyerCountry = buyerAddress?.Element(cacNs + "Country")
                                            ?.Element(cbcNs + "IdentificationCode")?.Value;

            // ── Top-level ProcurementProject (procedure-level) ───────────
            // The procedure-level ProcurementProject is a direct child of the root,
            // NOT nested inside a ProcurementProjectLot.
            var procProject = root.Elements(cacNs + "ProcurementProject").FirstOrDefault()
                           ?? root.Descendants(cacNs + "ProcurementProject").FirstOrDefault();
            if (procProject == null) return null;

            var title = procProject.Element(cbcNs + "Name")?.Value ?? "Untitled";
            var description = procProject.Element(cbcNs + "Description")?.Value ?? "";
            var contractNature = procProject.Element(cbcNs + "ProcurementTypeCode")?.Value;

            // ── Estimated value (BT-27) ───────────────────────────────────
            // Strategy: scan ALL ProcurementProject elements in the document
            // (procedure-level AND inside lots) for the first non-zero value.
            // Also fall back to FrameworkMaximumAmount (efbc:) when no EstimatedOverall is present.
            decimal? estimatedValue = null;
            var allProcProjects = root.Descendants(cacNs + "ProcurementProject").ToList();
            foreach (var pp in allProcProjects)
            {
                var reqTotal = pp.Element(cacNs + "RequestedTenderTotal");
                if (reqTotal == null) continue;

                // Primary: EstimatedOverallContractAmount
                var estStr = reqTotal.Element(cbcNs + "EstimatedOverallContractAmount")?.Value;
                if (decimal.TryParse(estStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var est) && est > 1)
                {
                    estimatedValue = est;
                    break;
                }

                // Fallback: FrameworkMaximumAmount (efbc: namespace)
                var fwkStr = reqTotal.Descendants(EfbcNs + "FrameworkMaximumAmount").FirstOrDefault()?.Value;
                if (decimal.TryParse(fwkStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var fwk) && fwk > 1)
                {
                    estimatedValue ??= fwk; // only use as fallback, keep searching for a better value
                }
            }

            // CPV (BT-262 main, plus additional)
            var cpvCode = procProject.Descendants(cbcNs + "ItemClassificationCode")
                                     .FirstOrDefault(e => e.Attribute("listName")?.Value == "cpv")?.Value ?? "";
            var additionalCpvCodes = procProject.Descendants(cacNs + "AdditionalCommodityClassification")
                                                .Select(a => a.Element(cbcNs + "ItemClassificationCode")?.Value)
                                                .Where(v => !string.IsNullOrEmpty(v))
                                                .ToList();

            // NUTS location (BT-507 / BT-5071)
            var nutsCode = procProject.Descendants(cbcNs + "CountrySubentityCode")
                                      .FirstOrDefault(e => (e.Attribute("listName")?.Value ?? "").StartsWith("nuts"))?.Value;

            // ── Lot-level dates & portal URL ─────────────────────────────
            // Take from the first ProcurementProjectLot
            var firstLot = root.Descendants(cacNs + "ProcurementProjectLot").FirstOrDefault();
            var lotId = firstLot?.Element(cbcNs + "ID")?.Value ?? "LOT-0000";

            // Submission deadline (BT-131)
            var lotTenderingProcess = firstLot?.Descendants(cacNs + "TenderingProcess").FirstOrDefault();
            var deadlinePeriod = lotTenderingProcess?.Element(cacNs + "TenderSubmissionDeadlinePeriod");
            DateTime? submissionDeadline = null;
            if (deadlinePeriod != null)
            {
                var dDateStr = deadlinePeriod.Element(cbcNs + "EndDate")?.Value;
                var dTimeStr = deadlinePeriod.Element(cbcNs + "EndTime")?.Value;
                // Combine date + time if both present, e.g. "2026-03-13+01:00" + "12:00:00+01:00"
                var combined = string.IsNullOrEmpty(dTimeStr)
                    ? dDateStr
                    : $"{dDateStr?.Split('+')[0].Split('-')[..3].Aggregate((a,b) => a+"-"+b)}T{dTimeStr}";
                submissionDeadline = ParseUtc(combined);
            }

            // Planned contract start/end dates (from lot ProcurementProject/PlannedPeriod)
            var lotProcProject = firstLot?.Elements(cacNs + "ProcurementProject").FirstOrDefault()
                               ?? firstLot?.Descendants(cacNs + "ProcurementProject").FirstOrDefault();
            var plannedPeriod = lotProcProject?.Element(cacNs + "PlannedPeriod");
            DateTime? contractStart = null, contractEnd = null;
            if (plannedPeriod != null)
            {
                contractStart = ParseUtc(plannedPeriod.Element(cbcNs + "StartDate")?.Value);
                contractEnd   = ParseUtc(plannedPeriod.Element(cbcNs + "EndDate")?.Value);
            }

            // Portal URL — try sources in priority order:
            // 1. BT-15: URI inside CallForTendersDocumentReference (direct link to tender docs)
            // 2. AccessToolsURI / AccessToolName (eSender platform URL)
            // 3. BuyerProfileURI on ContractingParty (buyer's general procurement portal)
            // 4. Buyer's own website
            var portalUrl =
                firstLot?.Descendants(cacNs + "CallForTendersDocumentReference")
                         .FirstOrDefault()
                         ?.Descendants(cbcNs + "URI").FirstOrDefault()?.Value
             ?? root.Descendants(cacNs + "TenderingProcess")
                    .FirstOrDefault()
                    ?.Element(cbcNs + "AccessToolsURI")?.Value
             ?? root.Descendants(EfbcNs + "AccessToolName").FirstOrDefault()?.Value
             ?? root.Descendants(cacNs + "ContractingParty")
                    .FirstOrDefault()
                    ?.Element(cbcNs + "BuyerProfileURI")?.Value
             ?? root.Descendants(cbcNs + "URI").FirstOrDefault()?.Value;

            // Normalise portal URL
            if (!string.IsNullOrEmpty(portalUrl) && !portalUrl.StartsWith("http"))
                portalUrl = "https://" + portalUrl;
            // Fallback to buyer website
            if (string.IsNullOrEmpty(portalUrl) && !string.IsNullOrEmpty(buyerWebsite))
                portalUrl = buyerWebsite.StartsWith("http") ? buyerWebsite : "https://" + buyerWebsite;

            return new RawTender
            {
                OCID = noticeId,
                LotId = lotId,
                NoticeType = noticeType,
                NoticeVersion = noticeVersion,
                Title = title,
                Description = description,
                BuyerName = buyerName,
                BuyerWebsite = buyerWebsite,
                BuyerContactEmail = buyerEmail,
                BuyerContactPhone = buyerPhone,
                BuyerCity = buyerCity,
                BuyerCountry = buyerCountry,
                CpvCode = cpvCode,
                AdditionalCpvCodes = additionalCpvCodes.Count > 0 ? string.Join(",", additionalCpvCodes) : null,
                NutsCode = nutsCode,
                ContractNature = contractNature,
                ProcedureType = procedureType,
                EstimatedValue = estimatedValue,
                PublicationDate = issueDate,
                SubmissionDeadline = submissionDeadline,
                ContractStartDate = contractStart,
                ContractEndDate = contractEnd,
                BuyerPortalUrl = portalUrl,
                NoticeStatus = DetermineNoticeStatus(root, cbcNs, efbcNs),
                RawXml = xmlDoc.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error parsing tender XML: {Error}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Parses an eForms date/datetime string (which may carry a timezone offset like +01:00)
    /// and returns a UTC DateTime, or null if the string is empty/unparseable.
    /// Using DateTimeOffset.TryParse ensures the offset is respected before converting to UTC,
    /// avoiding the Npgsql "Kind=Unspecified" error on timestamp with time zone columns.
    /// </summary>
    private static DateTime? ParseUtc(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTimeOffset.TryParse(value, null,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var dto))
            return dto.UtcDateTime;
        // Fallback: plain date with no offset — treat as UTC
        if (DateTime.TryParse(value, null,
                System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return null;
    }

    /// <summary>
    /// Determines the notice status from the root XML element and its contents.
    /// Rules:
    ///   Awarded  — root local name is "ContractAwardNotice"
    ///   Amendment — root is ContractNotice AND contains a ChangedNoticeIdentifier element
    ///   Active   — root is ContractNotice with no ChangedNoticeIdentifier (new open tender)
    /// </summary>
    private static string DetermineNoticeStatus(XElement root, XNamespace cbcNs, XNamespace efbcNs)
    {
        var localName = root.Name.LocalName;

        if (localName == "ContractAwardNotice")
            return "Awarded";

        // Check for ChangedNoticeIdentifier — present in corrigenda/amendments
        bool hasChangedId = root.Descendants(efbcNs + "ChangedNoticeIdentifier").Any()
                         || root.Descendants(cbcNs + "ChangedNoticeIdentifier").Any()
                         || root.Descendants("ChangedNoticeIdentifier").Any(); // legacy no-ns variant

        return hasChangedId ? "Amendment" : "Active";
    }

    /// <summary>
    /// Re-derives NoticeStatus from a raw XML string already stored in the database.
    /// Used for the retro-fill command.
    /// </summary>
    public static string DetermineNoticeStatusFromXml(string rawXml)
    {
        try
        {
            var doc = XDocument.Parse(rawXml);
            if (doc.Root == null) return "Active";

            var root = doc.Root;
            var cbcNs = root.GetNamespaceOfPrefix("cbc") ?? CbcNs;
            var efbcNs = root.GetNamespaceOfPrefix("efbc") ?? EfbcNs;

            return DetermineNoticeStatus(root, cbcNs, efbcNs);
        }
        catch
        {
            return "Active"; // safe default if XML can't be parsed
        }
    }

    /// <summary>Extracts the VersionID string from stored RawXml (e.g. "01", "02").</summary>
    public static string ExtractVersionFromXml(string rawXml)
    {
        try
        {
            var doc = XDocument.Parse(rawXml);
            if (doc.Root == null) return "01";
            var cbcNs = doc.Root.GetNamespaceOfPrefix("cbc") ?? CbcNs;
            return doc.Root.Element(cbcNs + "VersionID")?.Value?.Trim() ?? "01";
        }
        catch { return "01"; }
    }

    /// <summary>Extracts the bare notice GUID (NoticeId) from stored RawXml.</summary>
    public static string? ExtractNoticeIdFromXml(string rawXml)
    {
        try
        {
            var doc = XDocument.Parse(rawXml);
            if (doc.Root == null) return null;
            var cbcNs = doc.Root.GetNamespaceOfPrefix("cbc") ?? CbcNs;
            // Prefer the element with schemeName="notice-id", fall back to bare <ID>
            return doc.Root.Descendants(cbcNs + "ID")
                           .FirstOrDefault(e => e.Attribute("schemeName")?.Value == "notice-id")?.Value
                   ?? doc.Root.Element(cbcNs + "ID")?.Value;
        }
        catch { return null; }
    }
}

