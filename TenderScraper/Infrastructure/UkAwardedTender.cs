namespace TenderScraper.Infrastructure;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents a single award release from the UK Contracts Finder OCDS API.
/// One row per OCID — if a release has multiple awards (rare) the first is used
/// for scalar columns; all awards are preserved in RawJson.
/// </summary>
public class UkAwardedTender
{
    [Key]
    public int Id { get; set; }

    // ── OCDS Identity ─────────────────────────────────────────────────────
    /// <summary>Open Contracting ID — globally unique, used as the dedup key.</summary>
    [MaxLength(120)]
    public string Ocid { get; set; } = string.Empty;

    /// <summary>Release ID (OCID + sequence suffix).</summary>
    [MaxLength(200)]
    public string ReleaseId { get; set; } = string.Empty;

    // ── Tender details ────────────────────────────────────────────────────
    [MaxLength(1000)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    /// <summary>Primary CPV code from tender.classification.id</summary>
    [MaxLength(20)]
    public string? CpvCode { get; set; }

    /// <summary>CPV description text</summary>
    [MaxLength(255)]
    public string? CpvDescription { get; set; }

    /// <summary>Additional CPV codes (comma-separated)</summary>
    public string? AdditionalCpvCodes { get; set; }

    [MaxLength(100)]
    public string? ProcurementMethod { get; set; }

    [MaxLength(255)]
    public string? ProcurementMethodDetails { get; set; }

    [MaxLength(50)]
    public string? MainProcurementCategory { get; set; }

    /// <summary>tender.value.amount (contract estimate at tender stage)</summary>
    [Column(TypeName = "numeric(18,2)")]
    public decimal? TenderValueAmount { get; set; }

    [MaxLength(10)]
    public string? TenderValueCurrency { get; set; }

    /// <summary>SME suitable flag from tender.suitability.sme</summary>
    public bool? SuitableSme { get; set; }

    /// <summary>VCSE suitable flag from tender.suitability.vcse</summary>
    public bool? SuitableVcse { get; set; }

    public DateTime? TenderDeadline { get; set; }
    public DateTime? TenderContractStart { get; set; }
    public DateTime? TenderContractEnd { get; set; }

    // ── Delivery location (from tender.items[0].deliveryAddresses[0]) ─────
    [MaxLength(200)]
    public string? DeliveryRegion { get; set; }

    [MaxLength(20)]
    public string? DeliveryPostalCode { get; set; }

    [MaxLength(100)]
    public string? DeliveryCountry { get; set; }

    // ── Buyer (party with role=buyer) ─────────────────────────────────────
    [MaxLength(500)]
    public string? BuyerName { get; set; }

    [MaxLength(200)]
    public string? BuyerStreetAddress { get; set; }

    [MaxLength(100)]
    public string? BuyerLocality { get; set; }

    [MaxLength(20)]
    public string? BuyerPostalCode { get; set; }

    [MaxLength(100)]
    public string? BuyerCountry { get; set; }

    [MaxLength(255)]
    public string? BuyerContactName { get; set; }

    [MaxLength(255)]
    public string? BuyerContactEmail { get; set; }

    [MaxLength(50)]
    public string? BuyerContactPhone { get; set; }

    // ── Award details (first/primary award) ───────────────────────────────
    [MaxLength(200)]
    public string? AwardId { get; set; }

    [MaxLength(20)]
    public string? AwardStatus { get; set; }

    public DateTime? AwardDate { get; set; }

    public DateTime? AwardDatePublished { get; set; }

    /// <summary>Awarded contract value (may differ from tender estimate)</summary>
    [Column(TypeName = "numeric(18,2)")]
    public decimal? AwardValueAmount { get; set; }

    [MaxLength(10)]
    public string? AwardValueCurrency { get; set; }

    public DateTime? AwardContractStart { get; set; }
    public DateTime? AwardContractEnd { get; set; }

    // ── Supplier (first supplier on the primary award) ────────────────────
    /// <summary>Comma-separated list of all supplier names on this release.</summary>
    public string? SupplierNames { get; set; }

    /// <summary>Comma-separated list of all supplier company IDs.</summary>
    public string? SupplierIds { get; set; }

    [MaxLength(50)]
    public string? SupplierScale { get; set; }   // sme / large / micro

    // ── Portal link ───────────────────────────────────────────────────────
    /// <summary>URL of the award notice on Contracts Finder (documentType=awardNotice)</summary>
    [MaxLength(2048)]
    public string? NoticeUrl { get; set; }

    // ── Release metadata ──────────────────────────────────────────────────
    public DateTime? ReleaseDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Raw data ──────────────────────────────────────────────────────────
    /// <summary>Full JSON of the release object for backfilling.</summary>
    public string? RawJson { get; set; }
}

