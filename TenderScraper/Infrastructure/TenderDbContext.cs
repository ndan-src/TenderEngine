namespace TenderScraper.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class TenderDbContext : DbContext
{
    public TenderDbContext(DbContextOptions<TenderDbContext> options) : base(options)
    {
    }

    public DbSet<Tender> Tenders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tender>()
            .HasIndex(t => t.SourceId)
            .IsUnique();

        // Non-unique index on NoticeId — allows fast queries for all versions of a tender
        modelBuilder.Entity<Tender>()
            .HasIndex(t => t.NoticeId);

        // Ensure every DateTime property is stored as UTC.
        // This prevents Npgsql's "Kind=Unspecified" error on timestamp with time zone columns.
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
    }
}