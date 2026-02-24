using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using TenderScraper.Models;

namespace TenderScraper.Services;

/// <summary>
/// Provides German tender data from EFORMS XML files.
/// This replaces the CSV-based approach with direct XML parsing.
/// </summary>
public class GermanEformsProvider : ITenderProvider
{
    public string ProviderName => "Germany_EFORMS_XML";
    private readonly HttpClient _httpClient;
    private readonly ILogger<GermanEformsProvider> _logger;

    // XML Namespaces used in EFORMS
    private static readonly XNamespace CacNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace CbcNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace CnNs = "urn:oasis:names:specification:ubl:schema:xsd:ContractNotice-2";

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
        {
            throw new Exception($"Failed to download EFORMS ZIP: HTTP {response.StatusCode}");
        }
        
        using var stream = await response.Content.ReadAsStreamAsync();
        using var archive = new ZipArchive(stream);

        _logger.LogInformation("📦 ZIP contains {Count} XML files", archive.Entries.Count);

        var results = new List<RawTender>();
        int processedCount = 0;
        int itTenderCount = 0;

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                using var xmlStream = entry.Open();
                var xmlDoc = await XDocument.LoadAsync(xmlStream, LoadOptions.None, CancellationToken.None);

                // Parse the XML and extract tender data
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
                // Continue processing other files
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
            // Handle both with and without namespace prefix
            var root = xmlDoc.Root;
            if (root == null) return null;

            // Get the actual namespace from the root element
            var ns = root.Name.Namespace;
            var cbcNs = root.GetNamespaceOfPrefix("cbc") ?? CbcNs;
            var cacNs = root.GetNamespaceOfPrefix("cac") ?? CacNs;

            // Extract Notice ID
            var noticeId = root.Element(cbcNs + "ID")?.Value 
                          ?? root.Element("ID")?.Value;
            
            if (string.IsNullOrEmpty(noticeId))
                return null;

            // Extract Issue Date
            var issueDateStr = root.Element(cbcNs + "IssueDate")?.Value 
                              ?? root.Element("IssueDate")?.Value;
            DateTime.TryParse(issueDateStr, out DateTime issueDate);

            // Extract Procurement Project data
            var procurementProject = root.Descendants(cacNs + "ProcurementProject").FirstOrDefault()
                                    ?? root.Descendants("ProcurementProject").FirstOrDefault();

            if (procurementProject == null)
                return null;

            // Title
            var title = procurementProject.Element(cbcNs + "Name")?.Value 
                       ?? procurementProject.Element("Name")?.Value 
                       ?? "Untitled";

            // Description
            var description = procurementProject.Element(cbcNs + "Description")?.Value 
                            ?? procurementProject.Element("Description")?.Value 
                            ?? "";

            // CPV Code (Main Classification)
            var cpvCode = procurementProject
                .Descendants(cbcNs + "ItemClassificationCode")
                .Concat(procurementProject.Descendants("ItemClassificationCode"))
                .FirstOrDefault(e => e.Attribute("listName")?.Value == "cpv")?.Value 
                ?? "";

            // Procedure Type
            var procedureType = root.Descendants(cbcNs + "ProcedureCode").FirstOrDefault()?.Value 
                               ?? root.Descendants("ProcedureCode").FirstOrDefault()?.Value 
                               ?? "";

            // Buyer Website
            var buyerWebsite = root.Descendants(cbcNs + "WebsiteURI").FirstOrDefault()?.Value 
                              ?? root.Descendants("WebsiteURI").FirstOrDefault()?.Value;

            // Portal URL (from CallForTendersDocumentReference)
            var portalUrl = root.Descendants(cbcNs + "URI").FirstOrDefault()?.Value 
                           ?? root.Descendants("URI").FirstOrDefault()?.Value;

            // Lot ID
            var lotId = root.Descendants(cacNs + "ProcurementProjectLot")
                .Concat(root.Descendants("ProcurementProjectLot"))
                .FirstOrDefault()
                ?.Element(cbcNs + "ID")?.Value 
                ?? root.Descendants(cacNs + "ProcurementProjectLot")
                .Concat(root.Descendants("ProcurementProjectLot"))
                .FirstOrDefault()
                ?.Element("ID")?.Value 
                ?? "LOT-0000";

            // Ensure portal URL has protocol
            if (!string.IsNullOrEmpty(portalUrl))
            {
                if (!portalUrl.StartsWith("http://") && !portalUrl.StartsWith("https://"))
                    portalUrl = "https://" + portalUrl;
            }
            // Fallback to buyer website if no portal URL
            else if (!string.IsNullOrEmpty(buyerWebsite))
            {
                portalUrl = buyerWebsite;
                if (!portalUrl.StartsWith("http://") && !portalUrl.StartsWith("https://"))
                    portalUrl = "https://" + portalUrl;
            }

            return new RawTender
            {
                OCID = noticeId,
                Title = title,
                Description = description,
                CpvCode = cpvCode,
                PublicationDate = issueDate,
                LotId = lotId,
                ProcedureType = procedureType,
                EstimatedValue = null, // Not easily extractable from XML, would need complex parsing
                BuyerPortalUrl = portalUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error parsing tender XML: {Error}", ex.Message);
            return null;
        }
    }
}

