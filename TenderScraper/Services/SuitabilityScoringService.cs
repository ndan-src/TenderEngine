using TenderScraper.Models;

namespace TenderScraper.Services;

public class SuitabilityScoringService
{
    public double CalculateScore(RawTender metadata) //, AiAnalysisResult aiResults)
    {
        double score = 0;

        // 1. Procedure Weighting (CSVs)
        // 'Open' procedures are much easier for UK firms than 'Restricted'
        if (metadata.ProcedureType == "open") score += 20;

        // 2. Financial Fit (CSVs)
        // If the estimated value matches the client's "Sweet Spot"
        if (metadata.EstimatedValue >= 100000 && metadata.EstimatedValue <= 500000) score += 20;

        // 3. Technical Fit (From AI PDF Analysis)
        // AI flags if the tech stack matches the UK firm (e.g., .NET, Azure, ISO 27001)
     //   if (aiResults.TechStackMatch) score += 30;

        // 4. Location/Bureaucracy Friction (From AI PDF Analysis)
        // If AI finds "Must have physical office in Germany," subtract points
    //    if (aiResults.PhysicalPresenceRequired) score -= 15;
     //   if (aiResults.EnglishDocumentationAccepted) score += 15;

        // Normalize to a 0.0 - 10.0 scale
        return Math.Clamp(score, 0, 100) / 10.0;
    }
}