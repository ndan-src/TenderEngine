namespace TenderScraper.Infrastructure;

using Microsoft.EntityFrameworkCore;

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
    }
}