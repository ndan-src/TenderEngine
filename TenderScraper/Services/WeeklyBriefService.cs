using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TenderScraper.Infrastructure;

namespace TenderScraper.Services;

/// <summary>
/// Generates a weekly intelligence brief PDF for a given sector.
/// </summary>
public class WeeklyBriefService
{
    // Brand colours
    private static readonly string ColourNavy  = "#1a2744";
    private static readonly string ColourBlue  = "#2563eb";
    private static readonly string ColourGreen = "#16a34a";
    private static readonly string ColourAmber = "#d97706";
    private static readonly string ColourRed   = "#dc2626";
    private static readonly string ColourGrey  = "#64748b";
    private static readonly string ColourLight = "#f1f5f9";

    public string GenerateBrief(
        IList<Tender> tenders,
        string sector,
        DateOnly weekStart,
        DateOnly weekEnd,
        string outputPath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor("#1e293b"));

                page.Content().Column(col =>
                {
                    // ── Cover banner ─────────────────────────────────────
                    col.Item().Background(ColourNavy).Padding(36).Column(banner =>
                    {
                        banner.Item()
                            .Text("GERMAN PROCUREMENT INTELLIGENCE")
                            .FontSize(11).FontColor("#94a3b8").LetterSpacing(0.15f).Bold();

                        banner.Item().PaddingTop(6)
                            .Text($"Weekly Intelligence Brief")
                            .FontSize(28).FontColor(Colors.White).Bold();

                        banner.Item().PaddingTop(4)
                            .Text($"{sector} Sector  ·  {weekStart:dd MMM} – {weekEnd:dd MMM yyyy}")
                            .FontSize(13).FontColor("#93c5fd");

                        banner.Item().PaddingTop(20).Row(row =>
                        {
                            var amendCount = tenders.Count(t => t.NoticeStatus == "Amendment");
                            AddStatPill(row, tenders.Count.ToString(), "Tenders");
                            AddStatPill(row, FormatValue(tenders.Sum(t => t.ValueEuro ?? 0)), "Total Value (Est.)");
                            if (amendCount > 0)
                                AddStatPillAmber(row, amendCount.ToString(), "Amendments");
                        });
                    });

                    // ── Body ──────────────────────────────────────────────
                    col.Item().Padding(32).Column(body =>
                    {
                        // Section header
                        body.Item().PaddingBottom(16).Text("TOP TENDERS THIS WEEK")
                            .FontSize(11).Bold().FontColor(ColourNavy).LetterSpacing(0.1f);

                        // Tender cards
                        int rank = 1;
                        foreach (var tender in tenders)
                        {
                            body.Item().PaddingBottom(14).Border(1).BorderColor("#e2e8f0")
                                .Column(card =>
                                {
                                    // Amendment banner strip — shown only for amended notices
                                    if (tender.NoticeStatus == "Amendment")
                                    {
                                        card.Item()
                                            .Background(ColourAmber).PaddingHorizontal(12).PaddingVertical(5)
                                            .Row(ab =>
                                            {
                                                ab.ConstantItem(14).Text("✎").FontSize(9).FontColor(Colors.White);
                                                ab.RelativeItem().PaddingLeft(4)
                                                    .Text("AMENDED NOTICE — This is an update to a previously published tender")
                                                    .FontSize(8.5f).Bold().FontColor(Colors.White);
                                            });
                                    }

                                    // Card header
                                    card.Item().Background(ColourLight).Padding(12).Row(hdr =>
                                    {
                                        // Rank badge — amber for amendments, navy for new
                                        var badgeColour = tender.NoticeStatus == "Amendment" ? ColourAmber : ColourNavy;
                                        hdr.ConstantItem(32).Height(32)
                                            .Background(badgeColour)
                                            .AlignCenter().AlignMiddle()
                                            .Text($"#{rank}").FontSize(11).Bold().FontColor(Colors.White);

                                        hdr.RelativeItem().PaddingLeft(10).Column(title =>
                                        {
                                            title.Item().Text(tender.TitleEn ?? tender.TitleDe ?? "Untitled")
                                                .FontSize(11).Bold().FontColor(ColourNavy);
                                            if (!string.IsNullOrEmpty(tender.TitleDe) && !string.IsNullOrEmpty(tender.TitleEn))
                                                title.Item().Text($"🇩🇪 {tender.TitleDe}")
                                                    .FontSize(8.5f).FontColor(ColourGrey).Italic();
                                        });

                                        // Score badge
                                        if (tender.SuitabilityScore.HasValue)
                                        {
                                            var score = (double)tender.SuitabilityScore.Value;
                                            var scoreColour = score >= 7 ? ColourGreen : score >= 4 ? ColourAmber : ColourRed;
                                            hdr.ConstantItem(52).AlignRight().AlignMiddle()
                                                .Background(scoreColour).Padding(6)
                                                .Text($"{score:F1}/10").FontSize(10).Bold().FontColor(Colors.White).AlignCenter();
                                        }
                                    });

                                    // Card body
                                    card.Item().Padding(12).Column(body2 =>
                                    {
                                        // Two-column facts grid
                                        body2.Item().PaddingBottom(8).Row(facts =>
                                        {
                                            facts.RelativeItem().Column(left =>
                                            {
                                                AddFact(left, "🏛️ Authority", tender.BuyerNameEn ?? tender.BuyerName ?? "—");
                                                AddFact(left, "📍 Location", FormatLocation(tender));
                                                AddFact(left, "📋 CPV Code", tender.CpvCode ?? "—");
                                            });
                                            facts.RelativeItem().Column(right =>
                                            {
                                                AddFact(right, "💶 Value (Est.)", FormatValue(tender.ValueEuro));
                                                AddFact(right, "📅 Published", tender.PublicationDate?.ToString("dd MMM yyyy") ?? "—");
                                                AddFact(right, "⏰ Deadline", (tender.SubmissionDeadline ?? tender.Deadline)?.ToString("dd MMM yyyy HH:mm") ?? "—");
                                            });
                                        });

                                        // Summary
                                        if (!string.IsNullOrEmpty(tender.EnglishExecutiveSummary))
                                        {
                                            body2.Item().PaddingBottom(6).Column(s =>
                                            {
                                                s.Item().Text("Summary").FontSize(9).Bold().FontColor(ColourBlue);
                                                s.Item().PaddingTop(2).Text(Truncate(tender.EnglishExecutiveSummary, 350))
                                                    .FontSize(9).LineHeight(1.4f);
                                            });
                                        }
                                        else if (!string.IsNullOrEmpty(tender.DescriptionEn))
                                        {
                                            body2.Item().PaddingBottom(6).Column(s =>
                                            {
                                                s.Item().Text("Description").FontSize(9).Bold().FontColor(ColourBlue);
                                                s.Item().PaddingTop(2).Text(Truncate(tender.DescriptionEn, 350))
                                                    .FontSize(9).LineHeight(1.4f);
                                            });
                                        }

                                        // Risk flags
                                        if (!string.IsNullOrEmpty(tender.FatalFlaws))
                                        {
                                            body2.Item().PaddingBottom(6)
                                                .Background("#fef2f2").Border(1).BorderColor("#fecaca")
                                                .Padding(8).Row(risk =>
                                                {
                                                    risk.ConstantItem(14).Text("⚠").FontSize(10).FontColor(ColourRed);
                                                    risk.RelativeItem().PaddingLeft(4).Column(rc =>
                                                    {
                                                        rc.Item().Text("Risk Flags").FontSize(9).Bold().FontColor(ColourRed);
                                                        rc.Item().Text(tender.FatalFlaws).FontSize(9).LineHeight(1.3f);
                                                    });
                                                });
                                        }

                                        // Tech + certs row
                                        body2.Item().Row(bottom =>
                                        {
                                            if (!string.IsNullOrEmpty(tender.TechStack))
                                            {
                                                bottom.RelativeItem().Column(tc =>
                                                {
                                                    tc.Item().Text("Tech Stack").FontSize(9).Bold().FontColor(ColourGrey);
                                                    tc.Item().Text(tender.TechStack).FontSize(9).FontColor(ColourGrey);
                                                });
                                            }
                                            if (!string.IsNullOrEmpty(tender.HardCertifications))
                                            {
                                                bottom.RelativeItem().Column(cc =>
                                                {
                                                    cc.Item().Text("Required Certifications").FontSize(9).Bold().FontColor(ColourAmber);
                                                    cc.Item().Text(tender.HardCertifications).FontSize(9).FontColor(ColourAmber);
                                                });
                                            }
                                        });
                                    });
                                });

                            rank++;
                        }

                        // ── Footer summary stats ──────────────────────────
                        body.Item().PaddingTop(8).Background(ColourLight).Padding(16)
                            .Column(summary =>
                            {
                                summary.Item().PaddingBottom(8).Text("WEEK AT A GLANCE")
                                    .FontSize(10).Bold().FontColor(ColourNavy).LetterSpacing(0.08f);

                                summary.Item().Row(stats =>
                                {
                                    AddSummaryBlock(stats, "Avg Contract Value",
                                        FormatValue(tenders.Any(t => t.ValueEuro.HasValue)
                                            ? tenders.Where(t => t.ValueEuro.HasValue).Average(t => (double)t.ValueEuro!.Value)
                                            : 0));
                                    AddSummaryBlock(stats, "Highest Value",
                                        FormatValue(tenders.Max(t => t.ValueEuro ?? 0)));
                                });
                            });
                    });
                });

                // ── Page footer ───────────────────────────────────────────
                page.Footer().Background(ColourNavy).PaddingHorizontal(32).PaddingVertical(10).Row(footer =>
                {
                    footer.RelativeItem()
                        .Text($"ND Consulting  ·  German Procurement Intelligence Brief  ·  Generated {DateTime.Now:dd MMM yyyy HH:mm}")
                        .FontSize(8).FontColor("#94a3b8");
                    footer.ConstantItem(60).AlignRight()
                        .Text(x =>
                        {
                            x.Span("Page ").FontSize(8).FontColor("#94a3b8");
                            x.CurrentPageNumber().FontSize(8).FontColor("#94a3b8");
                            x.Span(" of ").FontSize(8).FontColor("#94a3b8");
                            x.TotalPages().FontSize(8).FontColor("#94a3b8");
                        });
                });
            });
        });

        document.GeneratePdf(outputPath);
        return outputPath;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void AddStatPill(RowDescriptor row, string value, string label)
    {
        row.ConstantItem(95).PaddingRight(8).Background("#1e3a6e").Padding(10).Column(p =>
        {
            p.Item().Text(value).FontSize(16).Bold().FontColor(Colors.White);
            p.Item().Text(label).FontSize(8).FontColor("#93c5fd");
        });
    }

    private static void AddStatPillAmber(RowDescriptor row, string value, string label)
    {
        row.ConstantItem(95).PaddingRight(8).Background("#92400e").Padding(10).Column(p =>
        {
            p.Item().Text(value).FontSize(16).Bold().FontColor(Colors.White);
            p.Item().Text(label).FontSize(8).FontColor("#fcd34d");
        });
    }

    private static void AddFact(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingBottom(4).Row(r =>
        {
            r.ConstantItem(90).Text(label).FontSize(8.5f).FontColor("#64748b");
            r.RelativeItem().Text(value).FontSize(8.5f).Bold();
        });
    }

    private static void AddSummaryBlock(RowDescriptor row, string label, string value)
    {
        row.RelativeItem().PaddingRight(12).Column(c =>
        {
            c.Item().Text(label).FontSize(8).FontColor(ColourGrey);
            c.Item().Text(value).FontSize(13).Bold().FontColor(ColourNavy);
        });
    }

    private static string FormatValue(decimal? value) => FormatValue((double)(value ?? 0));
    private static string FormatValue(double value)
    {
        if (value == 0) return "Not disclosed";
        if (value >= 1_000_000) return $"€{value / 1_000_000:F1}M";
        if (value >= 1_000) return $"€{value / 1_000:F0}K";
        return $"€{value:F0}";
    }

    private static string FormatLocation(Tender t)
    {
        var parts = new[] { t.BuyerCity, t.NutsCode, t.BuyerCountry }
            .Where(p => !string.IsNullOrEmpty(p)).ToList();
        return parts.Any() ? string.Join(", ", parts) : "—";
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
}

