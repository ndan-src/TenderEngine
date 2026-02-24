namespace TenderScraper.Services;

using HtmlAgilityPack; // dotnet add package HtmlAgilityPack

public class TenderDocumentDownloader
{
    private readonly HttpClient _httpClient;

    public TenderDocumentDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // Portals sometimes block generic "HttpClient" agents. 
        // Pretend to be a browser to avoid 403 errors.
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<byte[]> DownloadTenderZipAsync(string portalUrl)
    {
        // Step 1: Fetch the Landing Page
        var html = await _httpClient.GetStringAsync(portalUrl);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Step 2: Find the "Download All" or "ZIP" link
        // We look for <a> tags where the href or text contains keywords
        var downloadNode = doc.DocumentNode.Descendants("a")
            .FirstOrDefault(a => 
                a.GetAttributeValue("href", "").Contains("zip", StringComparison.OrdinalIgnoreCase) ||
                a.InnerText.Contains("Unterlagen herunterladen", StringComparison.OrdinalIgnoreCase));

        if (downloadNode == null) return null;

        string downloadUrl = downloadNode.GetAttributeValue("href", "");
        
        // Handle relative URLs
        if (!downloadUrl.StartsWith("http"))
        {
            var uri = new Uri(portalUrl);
            downloadUrl = $"{uri.Scheme}://{uri.Host}{downloadUrl}";
        }

        // Step 3: Direct Download
        return await _httpClient.GetByteArrayAsync(downloadUrl);
    }
}