using Microsoft.Extensions.Options;
using TenderScraper.Models;

namespace TenderScraper.Services;

public class TenderFilterOptions
{
    public List<string> HighValueKeywords { get; set; } = new();
    public List<string> ExclusionKeywords { get; set; } = new();
    public decimal MinEstimatedValue { get; set; }
}

public class TenderFilterService
{
    private readonly TenderFilterOptions _options;

    public TenderFilterService(IOptions<TenderFilterOptions> options)
    {
        _options = options.Value;
    }

    public bool IsHighValue(RawTender tender)
    {
        /*
         * Code,German Term,Meaning in English,Suitability for UK SMEs
                de-open,Offenes Verfahren,Open Procedure,High. Anyone can submit a full bid. No pre-qualification stage.
                neg-w-call,Verhandlungsverfahren mit Teilnahmewettbewerb,Negotiated with Call,"Medium. Two-stage. First, you prove you're qualified (Selection); then, the top 3-5 are invited to bid/negotiate."
                beabsichtigte...,Beabsichtigte beschränkte Ausschreibung,Intended Restricted Tender,"Warning. This is a ""Heads Up."" The buyer is announcing that they will invite specific firms soon. You need to act now to get on that list."
                oth-single,Sonstiges / Einstufig,Other Single-Stage,"Variable. Usually simplified procedures for lower-value contracts (under ""thresholds""). Less red tape, but often very short deadlines."
         */
        if(tender.ProcedureType != null && tender.ProcedureType.Contains("open", StringComparison.OrdinalIgnoreCase) )
            return true;
        // 1. Exclusion Check (Immediate "No")
        // e.g., if it mentions 'Hardware maintenance' and you only do 'Cyber'
        if (_options.ExclusionKeywords.Any(k => 
                tender.Description.Contains(k, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // 2. High-Value Keyword Check
        // Does it mention 'Cyber', 'Sicherheit', 'Cloud', 'Zero-Trust'?
        bool hasKeywords = _options.HighValueKeywords.Any(k => 
            tender.Title.Contains(k, StringComparison.OrdinalIgnoreCase) || 
            tender.Description.Contains(k, StringComparison.OrdinalIgnoreCase));

        return hasKeywords;
    }
}