using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TenderScraper.Models;

namespace TenderScraper.Services;

/// <summary>
/// Implementation of ILlmSummarizer using OpenAI GPT-4o.
/// Acts as a "Bilingual Information Scientist" to analyze German tender documents.
/// </summary>
public class LlmSummarizer : ILlmSummarizer
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LlmSummarizer> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    
    public LlmSummarizer(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<LlmSummarizer> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["OpenAI:ApiKey"] 
                  ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        
        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    /// <summary>
    /// Analyzes German tender text and generates a comprehensive Red Flag Report.
    /// Uses GPT-4o to identify exclusion criteria, certifications, tech stack, and eligibility.
    /// </summary>
    /*public async Task<RedFlagReport> GenerateRedFlagReportAsync(string rawGermanText)
    {
        _logger.LogInformation("Starting Red Flag analysis for tender text ({Length} chars)", rawGermanText.Length);
        
        try
        {
            
            var responseJson = await CallOpenAiAsync(prompt);
            
            
            _logger.LogInformation(
                "Red Flag analysis complete. Eligibility: {Probability:P0}, Fatal Flaws: {FlawCount}", 
                report.EligibilityProbability, 
                report.FatalFlaws.Count);
            
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Red Flag report");
            return CreateErrorReport(ex.Message);
        }
    }
    */
    
    private string BuildAnalysisPrompt(string rawGermanText)
    {
        // Truncate if too long (GPT-4o has token limits)
        var textSample = rawGermanText.Length > 12000 
            ? rawGermanText.Substring(0, 12000) + "\n[...truncated...]" 
            : rawGermanText;

        return $@"{PromptLibrary.GermanTenderAnalyst}

INPUT DOCUMENT (German):
{textSample}

OUTPUT FORMAT (JSON):
{{
    ""englishExecutiveSummary"": ""2-3 sentence summary of the project"",
    ""fatalFlaws"": [""list of showstopper requirements that UK SMEs cannot meet""],
    ""hardCertifications"": [""list of required certifications like BSI C5, ISO 27001, etc.""],
    ""techStack"": [""detected programming languages, frameworks, cloud platforms""],
    ""eligibilityProbability"": 0.75,
    ""additionalNotes"": ""Other important context or warnings""
}}

ANALYSIS:";
    }

    public async Task<UnifiedTenderAnalysis> AnalyzeTenderAsync(string userPrompt)
    {
        // 1. Build the modern 2026 Prompt
        var systemPrompt = "ROLE: You are a Senior Information Scientist and DACH Procurement Strategist. \nMISSION: Deconstruct complex German \"Amtsdeutsch\" tenders for a UK-based SME CEO.";
    
        // 2. Configure the Request with JSON Mode
        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" }, // Forces JSON output
            temperature = 0.2, // Even lower for higher reliability
            max_tokens = 2000
        };

        // 3. Execute the Call
        var response = await _httpClient.PostAsJsonAsync("chat/completions", requestBody);
        response.EnsureSuccessStatusCode();
    
        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
    
        var jsonContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        // 4. Deserialize directly into your Unified DTO
        if (string.IsNullOrEmpty(jsonContent)) 
            throw new InvalidOperationException("AI returned an empty response.");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var analysis = JsonSerializer.Deserialize<UnifiedTenderAnalysis>(jsonContent, options);

        return analysis ?? throw new Exception("Failed to map AI response to UnifiedTenderAnalysis.");
    }
    private async Task<string> CallOpenAiAsync(string prompt)
    {
        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = "You are a specialized procurement analyst for German public tenders. Always respond with valid JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0.3, // Lower temperature for more consistent analysis
            max_tokens = 1500
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("chat/completions", httpContent);
        response.EnsureSuccessStatusCode();
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var responseObj = JsonDocument.Parse(responseBody);
        
        var assistantMessage = responseObj.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
        
        return assistantMessage ?? throw new InvalidOperationException("Empty response from OpenAI");
    }

    /*
    private RedFlagReport ParseRedFlagReport(string jsonResponse)
    {
        // Extract JSON from response (sometimes model adds markdown)
        var jsonStart = jsonResponse.IndexOf('{');
        var jsonEnd = jsonResponse.LastIndexOf('}');
        
        if (jsonStart == -1 || jsonEnd == -1)
            throw new InvalidOperationException("No valid JSON found in LLM response");
        
        var cleanJson = jsonResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var report = JsonSerializer.Deserialize<RedFlagReport>(cleanJson, options);
        
        if (report == null)
            throw new InvalidOperationException("Failed to deserialize Red Flag report");
        
        // Ensure lists are never null
        report.FatalFlaws ??= new List<string>();
        report.HardCertifications ??= new List<string>();
        report.TechStack ??= new List<string>();
        
        return report;
    }

    private RedFlagReport CreateErrorReport(string errorMessage)
    {
        return new RedFlagReport
        {
            EnglishExecutiveSummary = "Error: Unable to analyze this tender document.",
            FatalFlaws = new List<string> { $"Analysis failed: {errorMessage}" },
            EligibilityProbability = 0.0,
            AdditionalNotes = "Please manually review this tender or retry analysis."
        };
    }
    */
}

