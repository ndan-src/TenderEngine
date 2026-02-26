namespace TenderScraper.Infrastructure;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Tender
{
    [Key]
    public int TenderID { get; set; }

    // ── Identity ─────────────────────────────────────────────────────────
    [MaxLength(100)]
    public string SourceId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? LotId { get; set; }

    [MaxLength(20)]
    public string? NoticeType { get; set; }

    // ── Title ─────────────────────────────────────────────────────────────
    public string? TitleDe { get; set; }
    public string? TitleEn { get; set; }

    // ── Description (original + English translation) ──────────────────────
    public string? DescriptionDe { get; set; }
    public string? DescriptionEn { get; set; }

    // ── Buyer / Contracting Authority ─────────────────────────────────────
    [MaxLength(500)]
    public string? BuyerName { get; set; }

    [MaxLength(500)]
    public string? BuyerNameEn { get; set; }

    [MaxLength(500)]
    public string? BuyerWebsite { get; set; }

    [MaxLength(255)]
    public string? BuyerContactEmail { get; set; }

    [MaxLength(50)]
    public string? BuyerContactPhone { get; set; }

    [MaxLength(100)]
    public string? BuyerCity { get; set; }

    [MaxLength(10)]
    public string? BuyerCountry { get; set; }

    // ── Classification ────────────────────────────────────────────────────
    [MaxLength(20)]
    public string? CpvCode { get; set; }

    public string? AdditionalCpvCodes { get; set; }

    [MaxLength(20)]
    public string? NutsCode { get; set; }

    [MaxLength(50)]
    public string? ContractNature { get; set; }

    [MaxLength(50)]
    public string? ProcedureType { get; set; }

    // ── Financials ────────────────────────────────────────────────────────
    [Column(TypeName = "numeric(18,2)")]
    public decimal? ValueEuro { get; set; }

    // ── Dates ─────────────────────────────────────────────────────────────
    public DateTime? PublicationDate { get; set; }
    public DateTime? SubmissionDeadline { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }

    /// <summary>Kept for backwards-compat — same as SubmissionDeadline</summary>
    public DateTime? Deadline { get; set; }

    // ── Portal / Documents ────────────────────────────────────────────────
    [MaxLength(2048)]
    public string? BuyerPortalUrl { get; set; }

    // ── AI Analysis outputs ───────────────────────────────────────────────
    [Column(TypeName = "numeric(3,1)")]
    public decimal? SuitabilityScore { get; set; }

    public string? EnglishExecutiveSummary { get; set; }
    public string? FatalFlaws { get; set; }
    public string? HardCertifications { get; set; }
    public string? TechStack { get; set; }

    [Column(TypeName = "numeric(3,2)")]
    public decimal? EligibilityProbability { get; set; }

    // ── Raw data ──────────────────────────────────────────────────────────
    public string? RawXml { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}



