Project: Pontis Intelligence (The Bilingual Bridge)
Status: Architecture Design Phase (2026)

Objective: Automate the extraction, analysis, and scoring of German public IT tenders (Division 72) for UK-based SMEs.

1. System Architecture
Language: C# / .NET 8+

Primary Data Source: oeffentlichevergabe.de Open Data API (eForms-DE standard).

Hybrid Storage:

PostgreSQL (Source of Truth): Managed metadata, business logic, user watchlists, and calculated scores.

OpenSearch (Search Engine): Vector embeddings for Semantic Search, raw German technical annexes, and AI-generated English summaries.

AI Integration: OpenAI GPT-4o for "Librarian-style" analysis of German procurement documents.

2. Core Domain Logic: BidSuitabilityScorer
The scoring logic prioritizes "Quality" over "Price" and flags "Location Friction."

C#
public class BidSuitabilityScorer
{
    public struct BidFactors
    {
        public bool IsOpenProcedure;    // 'Offenes Verfahren'
        public bool AcceptsEnglishDocs; // High value for UK firms
        public double PriceWeighting;   // e.g., 0.6 = 60% price / 40% quality
        public int RequiredTeamSize;
        public bool RequireOnSite;      // Location friction
    }

    public double CalculateScore(BidFactors factors, int clientTeamSize)
    {
        double score = 0;
        if (factors.IsOpenProcedure) score += 20;
        if (factors.AcceptsEnglishDocs) score += 10;
        if (clientTeamSize >= factors.RequiredTeamSize) score += 25;
        if (!factors.RequireOnSite) score += 20;
        if (factors.PriceWeighting <= 0.5) score += 25; // Favor quality-based bids
        return Math.Clamp(score, 0, 100) / 10.0;
    }
}
3. The "Librarian" AI Interface
The AI must act as a Bilingual Information Scientist. It does not just translate; it audits for risk.

C#
public interface ILlmSummarizer
{
    /// <summary>
    /// Audits German tender text for 'Ausschlusskriterien' (Exclusion Criteria).
    /// Identifies mandatory certifications (BSI C5, ISO) and project risks.
    /// </summary>
    Task<RedFlagReport> GenerateRedFlagReportAsync(string rawGermanText);
}

public class RedFlagReport
{
    public string EnglishExecutiveSummary { get; set; }
    public List<string> FatalFlaws { get; set; }       // Instant No-Bids (e.g. German Tax Audit required)
    public List<string> HardCertifications { get; set; } // Specific BSI/ISO standards
    public List<string> TechStack { get; set; }        // Detected languages/frameworks
    public double EligibilityProbability { get; set; } // 0.0 - 1.0
}
4. SQL Schema (PostgreSQL)
SQL
CREATE TABLE Tenders (
    TenderID SERIAL PRIMARY KEY,
    OCID VARCHAR(100) UNIQUE,
    Title_DE TEXT,
    Title_EN TEXT,
    BuyerName VARCHAR(255),
    ValueEuro DECIMAL(18, 2),
    Deadline TIMESTAMP,
    SuitabilityScore DECIMAL(3, 1),
    LibrarianReviewStatus INT DEFAULT 0 -- 0:New, 1:AuditInProgress, 2:Verified
);