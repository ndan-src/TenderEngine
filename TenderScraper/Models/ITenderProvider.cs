

namespace TenderScraper.Models;

public interface ITenderProvider
{
    string ProviderName { get; }
    // Fetches and returns a normalized list of "Raw" tenders
    Task<IEnumerable<RawTender>> FetchLatestNoticesAsync(DateTime date);
}

public class RawTender
{
    // ── Identity ────────────────────────────────────────────────────────────
    public string OCID { get; set; } = string.Empty;
    public string LotId { get; set; } = string.Empty;

    // ── Title & Description (original language, expected German) ─────────
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // ── Buyer / Contracting Authority ────────────────────────────────────
    /// <summary>Legal name of the buying organisation (BT-500)</summary>
    public string? BuyerName { get; set; }
    /// <summary>Buyer's own website (BT-505)</summary>
    public string? BuyerWebsite { get; set; }
    /// <summary>Buyer contact email (BT-506)</summary>
    public string? BuyerContactEmail { get; set; }
    /// <summary>Buyer contact phone (BT-503)</summary>
    public string? BuyerContactPhone { get; set; }
    /// <summary>City of the buyer (BT-513)</summary>
    public string? BuyerCity { get; set; }
    /// <summary>Country code of the buyer (BT-514)</summary>
    public string? BuyerCountry { get; set; }

    // ── Classification ────────────────────────────────────────────────────
    /// <summary>Primary CPV code (BT-262)</summary>
    public string CpvCode { get; set; } = string.Empty;
    /// <summary>Additional CPV codes, comma-separated</summary>
    public string? AdditionalCpvCodes { get; set; }
    /// <summary>NUTS location code (BT-507 / BT-5071)</summary>
    public string? NutsCode { get; set; }
    /// <summary>Contract nature: services / supplies / works (BT-23)</summary>
    public string? ContractNature { get; set; }
    /// <summary>Procedure type: open / restricted / neg-w-call etc. (BT-105)</summary>
    public string? ProcedureType { get; set; }
    /// <summary>Notice type code: cn-standard / pin-only etc. (BT-02)</summary>
    public string? NoticeType { get; set; }

    /// <summary>Active | Amendment | Awarded — derived from root element and ChangedNoticeIdentifier presence</summary>
    public string? NoticeStatus { get; set; }

    /// <summary>Version string from XML VersionID, e.g. "01", "02"</summary>
    public string? NoticeVersion { get; set; }

    // ── Financials ────────────────────────────────────────────────────────
    /// <summary>Estimated total contract value in EUR (BT-27)</summary>
    public decimal? EstimatedValue { get; set; }

    // ── Dates ─────────────────────────────────────────────────────────────
    /// <summary>Date the notice was issued (BT-05a)</summary>
    public DateTime PublicationDate { get; set; }
    /// <summary>Tender submission deadline date+time (BT-131)</summary>
    public DateTime? SubmissionDeadline { get; set; }
    /// <summary>Planned contract start date (from PlannedPeriod/StartDate)</summary>
    public DateTime? ContractStartDate { get; set; }
    /// <summary>Planned contract end date (from PlannedPeriod/EndDate)</summary>
    public DateTime? ContractEndDate { get; set; }

    // ── Portal / Documents ────────────────────────────────────────────────
    /// <summary>URL to the buyer's portal where tender documents can be accessed (BT-15)</summary>
    public string? BuyerPortalUrl { get; set; }

    // ── Raw data ──────────────────────────────────────────────────────────
    /// <summary>Original XML content from EFORMS</summary>
    public string? RawXml { get; set; }
}