﻿using TenderScraper.Models;

namespace TenderScraper.Services;

public class TranslationService(ILlmSummarizer llm)
{
    public async Task<UnifiedTenderAnalysis> GetSmartSummaryAsync(string procedureType, string germanDescription, string? buyerName = null)
    {
        var prompt = PromptLibrary.GetGermanTenderAnalyst(procedureType, germanDescription, buyerName);
        var result = await llm.AnalyzeTenderAsync(prompt); 
        return result;
    }

    /// <summary>
    /// Translates a German organisation name to its natural English equivalent.
    /// Returns the original name if translation fails or the name is already in English.
    /// </summary>
    public async Task<string?> TranslateBuyerNameAsync(string? germanName)
    {
        if (string.IsNullOrWhiteSpace(germanName)) return null;

        var prompt =
            $"""
            Translate the following German government/organisation name to English.
            Rules:
            - If it is already in English, return it unchanged.
            - Return ONLY the translated name — no explanation, no quotes, no punctuation.
            - Preserve acronyms (e.g. "GmbH", "AG", "e.V.") as-is.

            German name: {germanName}
            """;

        try
        {
            return await llm.TranslateAsync(prompt);
        }
        catch
        {
            // If translation fails, fall back to the original name
            return germanName;
        }
    }
}