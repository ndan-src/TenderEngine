using System.Text.Json.Serialization;

namespace TenderScraper.Models;

public interface ITenderProvider
{
    string ProviderName { get; }
    // Fetches and returns a normalized list of "Raw" tenders
    Task<IEnumerable<RawTender>> FetchLatestNoticesAsync(DateTime date);
}

public class RawTender
{
    public string OCID { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CpvCode { get; set; } = string.Empty;
    public DateTime PublicationDate { get; set; }
    public string LotId { get; set; } = string.Empty;
    
    /// <summary>
    /// Estimated value in EUR (from procedureLotResult.frameworkEstimatedValue or tender.tenderValue)
    /// </summary>
    public decimal? EstimatedValue { get; set; }
    
    /// <summary>
    /// Procedure type (e.g., "open", "neg-w-call", "restricted")
    /// </summary>
    public string? ProcedureType { get; set; }
    
    /// <summary>
    /// URL to the buyer's portal where tender documents can be accessed
    /// </summary>
    public string? BuyerPortalUrl { get; set; }
    
    /// <summary>
    /// Original XML content (if available from EFORMS provider)
    /// </summary>
    public string? RawXml { get; set; }
}