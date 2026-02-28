namespace TenderScraper.Models;


public class UnifiedTenderAnalysis
{
    public ProjectMetadata Metadata { get; set; } = new();
    public RedFlagReport RedFlags { get; set; } = new();
    public TechnicalAudit Technical { get; set; } = new();
    public Scoring DecisionSupport { get; set; } = new();
    
    public void PrintAnalysis()
    {
        Console.WriteLine();
        Console.BackgroundColor = ConsoleColor.Blue;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($" TENDER: {this.Metadata.Title.ToUpper()} ");
        Console.ResetColor();

        // 1. Executive Summary
        Console.WriteLine("\nSummary:");
        foreach (var line in Metadata.Summary)
        {
            Console.WriteLine($" • {line}");
        }

        // 2. Red Flags (The most important part!)
        if (RedFlags.FatalFlaws.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[!] FATAL FLAWS DETECTED:");
            foreach (var flaw in RedFlags.FatalFlaws)
            {
                Console.WriteLine($" >> {flaw}");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[✓] No immediate fatal flaws detected.");
        }
        Console.ResetColor();

        // 3. Location & Bidding
        Console.WriteLine($"\nLocation: {RedFlags.LocationFriction}");
        Console.WriteLine($"English Bidding: {(RedFlags.EnglishBiddingAllowed ? "YES" : "NO")}");

        // 4. Tech Stack
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nTech Stack: {string.Join(", ", Technical.TechStack)}");
        Console.ResetColor();

        // 5. The "CEO" Advice
        Console.WriteLine("\n------------------------------------------------");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"STRATEGIC ADVICE (Score: {DecisionSupport.AccessibilityScore}/10)");
        Console.WriteLine(DecisionSupport.StrategicAdvice);
        Console.ResetColor();
        Console.WriteLine("------------------------------------------------\n");
    }
}

public class ProjectMetadata
{
    public string Title { get; set; } = string.Empty;
    public List<string> Summary { get; set; } = new();
    public string BuyerNameEn { get; set; } = string.Empty;
}

public class RedFlagReport
{
    public List<string> FatalFlaws { get; set; } = new();
    public string ReciprocityRisk { get; set; } = string.Empty;
    public bool EnglishBiddingAllowed { get; set; }
    public string LocationFriction { get; set; } = string.Empty;
    public string CyberSecurity { get; set; } = string.Empty;
}

public class TechnicalAudit
{
    public List<string> TechStack { get; set; } = new();
    public List<string> Certifications { get; set; } = new();
}

public class Scoring
{
    public double AccessibilityScore { get; set; }
    public string EffortEstimate { get; set; } = string.Empty;
    public string StrategicAdvice { get; set; } = string.Empty;
}