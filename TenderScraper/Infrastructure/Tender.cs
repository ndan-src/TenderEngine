namespace TenderScraper.Infrastructure;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Tender
{
    [Key]
    public int TenderID { get; set; }
    
    [MaxLength(100)]
    public string SourceId { get; set; } = string.Empty;
    
    public string? TitleDe { get; set; }
    
    public string? TitleEn { get; set; }
    
    [MaxLength(255)]
    public string? BuyerName { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal? ValueEuro { get; set; }
    
    public DateTime? Deadline { get; set; }
    
    [Column(TypeName = "decimal(3, 1)")]
    public decimal? SuitabilityScore { get; set; }
    
    public string? RawXml { get; set; }
    
    public string? EnglishExecutiveSummary { get; set; }
    
    public string? FatalFlaws { get; set; }
    
    public string? HardCertifications { get; set; }
    
    public string? TechStack { get; set; }
    
    [Column(TypeName = "decimal(3, 2)")]
    public decimal? EligibilityProbability { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

