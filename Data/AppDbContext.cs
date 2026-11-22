using Microsoft.EntityFrameworkCore;

namespace PaperlessDashBlazor.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PageEntity> Pages { get; set; }
    public DbSet<CardEntity> Cards { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PageEntity>(entity =>
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

        modelBuilder.Entity<CardEntity>(entity =>
        {
            entity.HasKey(c => c.Id);
        });
    }
}
