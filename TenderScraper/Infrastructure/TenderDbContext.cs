namespace TenderScraper.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class TenderDbContext : DbContext
{
    public TenderDbContext(DbContextOptions<TenderDbContext> options) : base(options)
    {
    }

    public DbSet<Tender> Tenders { get; set; }
    public DbSet<UkAwardedTender> UkAwardedTenders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tender>()
            .HasIndex(t => t.SourceId)
            .IsUnique();

        // Non-unique index on NoticeId — allows fast queries for all versions of a tender
        modelBuilder.Entity<Tender>()
            .HasIndex(t => t.NoticeId);

        // UK awarded tenders — unique per OCID
        modelBuilder.Entity<UkAwardedTender>()
            .HasIndex(u => u.Ocid)
            .IsUnique();

        // Ensure every DateTime property is stored as UTC.
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            d => d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d.ToUniversalTime(),
            d => DateTime.SpecifyKind(d, DateTimeKind.Utc));

        var utcNullableConverter = new ValueConverter<DateTime?, DateTime?>(
            d => d == null ? null : d.Value.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc)
                            : d.Value.ToUniversalTime(),
            d => d == null ? null : DateTime.SpecifyKind(d.Value, DateTimeKind.Utc));

        var tender = modelBuilder.Entity<Tender>();
        tender.Property(t => t.CreatedAt).HasConversion(utcConverter);
        tender.Property(t => t.PublicationDate).HasConversion(utcNullableConverter);
        tender.Property(t => t.SubmissionDeadline).HasConversion(utcNullableConverter);
        tender.Property(t => t.Deadline).HasConversion(utcNullableConverter);
        tender.Property(t => t.ContractStartDate).HasConversion(utcNullableConverter);
        tender.Property(t => t.ContractEndDate).HasConversion(utcNullableConverter);

        var uk = modelBuilder.Entity<UkAwardedTender>();
        uk.Property(u => u.CreatedAt).HasConversion(utcConverter);
        uk.Property(u => u.ReleaseDate).HasConversion(utcNullableConverter);
        uk.Property(u => u.TenderDeadline).HasConversion(utcNullableConverter);
        uk.Property(u => u.TenderContractStart).HasConversion(utcNullableConverter);
        uk.Property(u => u.TenderContractEnd).HasConversion(utcNullableConverter);
        uk.Property(u => u.AwardDate).HasConversion(utcNullableConverter);
        uk.Property(u => u.AwardDatePublished).HasConversion(utcNullableConverter);
        uk.Property(u => u.AwardContractStart).HasConversion(utcNullableConverter);
        uk.Property(u => u.AwardContractEnd).HasConversion(utcNullableConverter);
    }
}