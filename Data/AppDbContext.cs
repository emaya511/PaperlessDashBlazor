using Microsoft.EntityFrameworkCore;
using PaperlessDashBlazor.Services;

namespace PaperlessDashBlazor.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DynamicPageEntry> Pages { get; set; }
    public DbSet<CardEntry> Cards { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DynamicPageEntry>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Slug).IsUnique();
            entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Slug).IsRequired().HasMaxLength(200);

            entity.HasMany(p => p.Cards)
                  .WithOne(c => c.Page)
                  .HasForeignKey(c => c.PageId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CardEntry>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Size)
                  .HasConversion<string>(); // store as "Square"/"Wide" string
        });
    }
}
