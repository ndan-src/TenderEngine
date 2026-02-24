using Microsoft.Extensions.Logging;
using TenderScraper.Models;

namespace TenderScraper.Services;

using System.IO.Compression;
using UglyToad.PdfPig;
using System.Text.RegularExpressions;

public class DeepAnalysisService
{
    private readonly ILlmSummarizer _llm; // Your OpenAI/Azure wrapper
    private readonly ILogger<DeepAnalysisService> _logger;

    // Librarian's keywords to find the "Meat" of the tender
    private static readonly Regex _technicalDocRegex = new Regex(
        @"(Leistungsbeschreibung|Anforderungen|Lastenheft|Technical_Spec|Statement_of_Work)", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task ProcessTenderDocumentsAsync(RawTender tender, byte[] zipData)
    {
        using var stream = new MemoryStream(zipData);
        using var archive = new ZipArchive(stream);
        
        var extractedTexts = new List<string>();

        foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".pdf")))
        {
            // Only process PDFs that look like technical requirements
            if (_technicalDocRegex.IsMatch(entry.FullName))
            {
                _logger.LogInformation("Analyzing technical doc: {Name}", entry.FullName);
                
                using var pdfStream = entry.Open();
                using var ms = new MemoryStream();
                await pdfStream.CopyToAsync(ms);
                
                var text = ExtractTextFromPdf(ms.ToArray());
                extractedTexts.Add(text);
            }
        }

        if (extractedTexts.Any())
        {
            // Combine the text and send to AI for the "Red Flag" report
            var fullContext = string.Join("\n\n--- Next Document ---\n\n", extractedTexts);
            
            _logger.LogInformation(fullContext);
            //var summary = await _llm.GenerateRedFlagReportAsync(fullContext);
            
            // Store results in SQL/OpenSearch
            //await SaveAnalysisResults(tender, summary);
        }
    }

    private string ExtractTextFromPdf(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        return string.Join(" ", document.GetPages().Select(p => p.Text));
    }

    private Task SaveAnalysisResults(RawTender tender, RedFlagReport summary)
    {
        _logger.LogInformation(
            "Saving analysis results for tender {OCID}. Eligibility: {Probability:P0}", 
            tender.OCID, 
            null);
        
        // TODO: Implement database persistence
        // - Save to PostgreSQL (metadata, scores)
        // - Index in OpenSearch (full text + embeddings)
        // Example:
        // await _dbContext.TenderAnalyses.AddAsync(new TenderAnalysis
        // {
        //     TenderOCID = tender.OCID,
        //     ExecutiveSummary = summary.EnglishExecutiveSummary,
        //     EligibilityScore = summary.EligibilityProbability,
        //     FatalFlawsJson = JsonSerializer.Serialize(summary.FatalFlaws),
        //     AnalysisDate = DateTime.UtcNow
        // });
        // await _dbContext.SaveChangesAsync();
        
        return Task.CompletedTask;
    }
}