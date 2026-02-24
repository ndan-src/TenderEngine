namespace TenderScraper.Services;

public static class PromptLibrary
{
    public const string GermanTenderAnalyst = @"### CONTEXT
            - Year: 2026. 
            - Target: UK SMEs (Non-EU status).
            - Critical Hurdles: BSI C5:2025, NIS2, and IT-Sicherheitskatalog (IT-SiKat).

            ### TASK
            Analyze the provided German tender description. You MUST return a JSON object. 
            If data is missing, provide your best professional estimate based on the procedure type.
When summarizing German standards, always provide the UK/International equivalent in parentheses.
'BSI IT-Grundschutz' -> 'BSI IT-Grundschutz (German ISO 27001 equivalent)'
'IT-SiKat' -> 'IT-SiKat (Sector-specific security catalog)'
'Geheimschutz' -> 'Security Clearance / Official Secrets Act'


            ### FIELD-SPECIFIC INSTRUCTIONS
            1. Metadata.Title: Professional English translation. 
            2. Metadata.Summary: 3 bullets. Map German acronyms to UK/Global equivalents (e.g., ""IT-Grundschutz"" -> ""BSI baseline security, similar to NIST/ISO"").
            3. RedFlags.FatalFlaws: List dealbreakers. Look for ""Deutsche Steuer-ID"", ""Sicherheitsüberprüfung (Ü2/Ü3)"", or ""Ausschließliche Vertragssprache Deutsch"".
            4. RedFlags.ReciprocityRisk: Based on the UK's non-EU status in 2026, evaluate if this tender is protected by GPA or strictly ""EU-only"".
            5. Technical.TechStack: Include METHODOLOGIES (ISO 27001, IT-Grundschutz), COMPLIANCE (NIS2, BSI C5), and SOFTWARE (GRC Tools, ServiceNow, etc.).
            6. DecisionSupport.AccessibilityScore: A numeric value 1-10. 
               - Deduct 4 points if ""Bidding Language: German"" is mandatory.
               - Deduct 2 points if ""On-site presence"" is required.
               - Never return 0.
            7. DecisionSupport.StrategicAdvice: High-value recommendation. Should they:
               - ""Bid solo"" (Rare for UK)
               - ""Find a German partner"" (Common for ISMS)
               - ""Walk away"" (If reciprocity/language risk is too high)

            ### JSON SCHEMA
            {
              ""Metadata"": { ""Title"": """", ""Summary"": [] },
              ""RedFlags"": {
                ""FatalFlaws"": [],
                ""ReciprocityRisk"": """",
                ""EnglishBiddingAllowed"": false,
                ""LocationFriction"": """",
                ""CyberSecurity"": """"
              },
              ""Technical"": { ""TechStack"": [], ""Certifications"": [] },
              ""DecisionSupport"": {
                ""AccessibilityScore"": 0.0,
                ""EffortEstimate"": """",
                ""StrategicAdvice"": """"
              }
            }

            GERMAN DESCRIPTION:
            [DESCRIPTION]";
    
    public static string GetGermanTenderAnalyst(string procedureType, string description)
    {
        return GermanTenderAnalyst
            .Replace("[PROCEDURE_TYPE]", procedureType)
            .Replace("[DESCRIPTION]", description);
    }
}