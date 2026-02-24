namespace TenderScraper.Services;

using System.Text.RegularExpressions;

public class TenderUrlExtractor
{
    // Regex to find standard web URLs
    private static readonly Regex _urlRegex = new Regex(@"https?://[^\s]+", RegexOptions.Compiled);

    public string GetTenderPortalUrl(string description)
    {
        var match = _urlRegex.Match(description);
        return match.Success ? match.Value : null;
    }
}