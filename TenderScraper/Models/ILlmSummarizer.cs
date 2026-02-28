namespace TenderScraper.Models;

/// <summary>
/// Interface for AI-powered tender document analysis.
/// Acts as a "Bilingual Information Scientist" to audit German tenders for UK SMEs.
/// </summary>
public interface ILlmSummarizer
{
    /// <summary>
    /// Audits German tender text for 'Ausschlusskriterien' (Exclusion Criteria).
    /// Identifies mandatory certifications (BSI C5, ISO) and project risks.
    /// </summary>
    /// <param name="rawGermanText">Raw German text from tender documents</param>
    /// <returns>Comprehensive red flag report with English summary and risk analysis</returns>
//    Task<RedFlagReport> GenerateRedFlagReportAsync(string rawGermanText);

    Task<UnifiedTenderAnalysis> AnalyzeTenderAsync(string userPrompt);

    /// <summary>
    /// Sends a plain prompt and returns the raw text response.
    /// Used for simple tasks like translating an organisation name.
    /// </summary>
    Task<string> TranslateAsync(string prompt);
}

