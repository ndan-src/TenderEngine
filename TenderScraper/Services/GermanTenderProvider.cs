using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using TenderScraper.Models;

namespace TenderScraper.Services;

// CSV Record Classes for type safety
internal class ClassificationRecord
{
    public string noticeIdentifier { get; set; } = "";
    public string mainClassificationCode { get; set; } = "";
}

internal class ProcedureRecord
{
    public string noticeIdentifier { get; set; } = "";
    public string procedureType { get; set; } = "";
}

internal class ProcedureLotResultRecord
{
    public string noticeIdentifier { get; set; } = "";
    public string lotIdentifier { get; set; } = "";
    public string frameworkEstimatedValue { get; set; } = "";
    public string tenderValueHighest { get; set; } = "";
}

internal class PurposeRecord
{
    public string noticeIdentifier { get; set; } = "";
    public string lotIdentifier { get; set; } = "";
    public string title { get; set; } = "";
    public string description { get; set; } = "";
}

internal class OrganisationRecord
{
    public string noticeIdentifier { get; set; } = "";
    public string organisationRole { get; set; } = "";
    public string organisationInternetAddress { get; set; } = "";
    public string buyerProfileURL { get; set; } = "";
}

public class GermanTenderProvider : ITenderProvider
{
    public string ProviderName => "Germany_Bekanntmachungsservice";
    private readonly HttpClient _httpClient;

    public GermanTenderProvider(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IEnumerable<RawTender>> FetchLatestNoticesAsync(DateTime date)
    {
        string dateStr = date.ToString("yyyy-MM-dd");
        var response = await _httpClient.GetAsync($"https://oeffentlichevergabe.de/api/notice-exports?pubDay={dateStr}&format=csv.zip");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to download ZIP: HTTP {response.StatusCode}");
        }
        
        using var stream = await response.Content.ReadAsStreamAsync();
        using var archive = new ZipArchive(stream);

        // DEBUG: List all files in the ZIP
        Console.WriteLine($"📦 ZIP contains {archive.Entries.Count} files:");
        foreach (var entry in archive.Entries)
        {
            Console.WriteLine($"   - {entry.FullName} ({entry.Length} bytes)");
        }
        Console.WriteLine();

        // 1. Load Classifications (CPV Codes) to identify IT tenders
        var itNoticeIds = new HashSet<string>();
        try
        {
            var classificationEntry = archive.GetEntry("classification.csv");
            if (classificationEntry == null)
            {
                throw new Exception("classification.csv not found in ZIP archive");
            }

            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null,
                ReadingExceptionOccurred = args => false // Skip bad rows
            };
            
            using (var reader = new StreamReader(classificationEntry.Open()))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<ClassificationRecord>();
                foreach (var r in records)
                {
                    if (!string.IsNullOrEmpty(r.mainClassificationCode) && 
                        r.mainClassificationCode.StartsWith("72") && 
                        !string.IsNullOrEmpty(r.noticeIdentifier))
                    {
                        itNoticeIds.Add(r.noticeIdentifier);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading classification.csv: {ex.Message}", ex);
        }

        // 2. Load Procedure Types
        var procedureTypes = new Dictionary<string, string>();
        try
        {
            var procedureEntry = archive.GetEntry("procedure.csv");
            if (procedureEntry == null)
            {
                throw new Exception("procedure.csv not found in ZIP archive");
            }

            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null,
                ReadingExceptionOccurred = args => false // Skip bad rows
            };
            
            using (var reader = new StreamReader(procedureEntry.Open()))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<ProcedureRecord>();
                foreach (var r in records)
                {
                    if (!string.IsNullOrEmpty(r.noticeIdentifier))
                    {
                        procedureTypes[r.noticeIdentifier] = r.procedureType;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading procedure.csv: {ex.Message}", ex);
        }

        // 3. Load Estimated Values (per lot)
        var estimatedValues = new Dictionary<string, decimal?>();
        try
        {
            var lotResultEntry = archive.GetEntry("procedureLotResult.csv");
            if (lotResultEntry == null)
            {
                throw new Exception("procedureLotResult.csv not found in ZIP archive");
            }

            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null,
                ReadingExceptionOccurred = args => false // Skip bad rows
            };
            
            using (var reader = new StreamReader(lotResultEntry.Open()))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<ProcedureLotResultRecord>();
                foreach (var r in records)
                {
                    if (string.IsNullOrEmpty(r.noticeIdentifier) || string.IsNullOrEmpty(r.lotIdentifier))
                        continue;
                    
                    string key = $"{r.noticeIdentifier}_{r.lotIdentifier}";
                    
                    // Try frameworkEstimatedValue first, then tenderValueHighest
                    decimal? estimatedValue = null;
                    
                    if (!string.IsNullOrEmpty(r.frameworkEstimatedValue))
                    {
                        if (decimal.TryParse(r.frameworkEstimatedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
                            estimatedValue = val;
                    }
                    else if (!string.IsNullOrEmpty(r.tenderValueHighest))
                    {
                        if (decimal.TryParse(r.tenderValueHighest, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
                            estimatedValue = val;
                    }
                    
                    estimatedValues[key] = estimatedValue;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading procedureLotResult.csv: {ex.Message}", ex);
        }

        // 4. Load Purpose (Descriptions) and join with our IT IDs
        var results = new List<RawTender>();
        try
        {
            var purposeEntry = archive.Entries
                .FirstOrDefault(e => e.FullName.EndsWith("purpose.csv", StringComparison.OrdinalIgnoreCase));
            
            if (purposeEntry == null)
            {
                throw new Exception("purpose.csv not found in ZIP archive");
            }

            Console.WriteLine($"✓ Found: {purposeEntry.FullName}");

            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null,
                ReadingExceptionOccurred = args => false // Skip bad rows
            };
            
            using (var reader = new StreamReader(purposeEntry.Open()))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<PurposeRecord>();
                foreach (var r in records)
                {
                    if (!itNoticeIds.Contains(r.noticeIdentifier))
                        continue;
                    
                    if (string.IsNullOrEmpty(r.title))
                        continue; // Skip tenders without titles
                    
                    string key = $"{r.noticeIdentifier}_{r.lotIdentifier}";
                    
                    results.Add(new RawTender {
                        OCID = r.noticeIdentifier,
                        Title = r.title,
                        Description = r.description,
                        LotId = r.lotIdentifier,
                        ProcedureType = procedureTypes.GetValueOrDefault(r.noticeIdentifier),
                        EstimatedValue = estimatedValues.GetValueOrDefault(key),
                        BuyerPortalUrl = null // Will be populated below
                    });
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading purpose.csv: {ex.Message}", ex);
        }
        
        // 5. Load Buyer Portal URLs from organisation.csv
        try
        {
            var organisationEntry = archive.Entries
                .FirstOrDefault(e => e.FullName.EndsWith("organisation.csv", StringComparison.OrdinalIgnoreCase));
            
            if (organisationEntry != null)
            {
                Console.WriteLine($"✓ Found: {organisationEntry.FullName}");
                
                var buyerUrls = new Dictionary<string, string>();
                
                var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    BadDataFound = null,
                    ReadingExceptionOccurred = args => false
                };
                
                using (var reader = new StreamReader(organisationEntry.Open()))
                using (var csv = new CsvReader(reader, config))
                {
                    var records = csv.GetRecords<OrganisationRecord>();
                    foreach (var r in records)
                    {
                        if (r.organisationRole == "buyer" && !string.IsNullOrEmpty(r.noticeIdentifier))
                        {
                            // Prefer buyerProfileURL, fallback to organisationInternetAddress
                            string url = r.buyerProfileURL;
                            if (string.IsNullOrEmpty(url))
                                url = r.organisationInternetAddress;
                            
                            if (!string.IsNullOrEmpty(url))
                            {
                                // Ensure URL has protocol
                                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                                    url = "https://" + url;
                                
                                buyerUrls[r.noticeIdentifier] = url;
                            }
                        }
                    }
                }
                
                // Update results with buyer portal URLs
                foreach (var tender in results)
                {
                    if (buyerUrls.TryGetValue(tender.OCID, out string? portalUrl))
                    {
                        tender.BuyerPortalUrl = portalUrl;
                    }
                }
                
                Console.WriteLine($"✓ Populated {buyerUrls.Count} buyer portal URLs");
            }
            else
            {
                Console.WriteLine("⚠ organisation.csv not found - buyer portal URLs will be empty");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Error reading organisation.csv: {ex.Message} - continuing without portal URLs");
        }
        
        return results;
    }
}