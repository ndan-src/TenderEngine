using TenderScraper.Models;

namespace TenderScraper.Services;

public class TranslationService(ILlmSummarizer llm)
{
    public async Task<UnifiedTenderAnalysis> GetSmartSummaryAsync(string procedureType, string germanDescription)
    {
        // Use Semantic Kernel or OpenAI .NET SDK
        var prompt = PromptLibrary.GetGermanTenderAnalyst(procedureType, germanDescription);
        
        // This is where you'd call your ILlmSummarizer implementation
        var result = await llm.AnalyzeTenderAsync(prompt); 
        return result;
    }
}