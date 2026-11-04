using Microsoft.EntityFrameworkCore;
using MyCatalog.Models;

namespace MyCatalog.Data;

// Representa a conexão e as tabelas que o EF Core manipula.
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<CatalogItemCover> CatalogItemCovers => Set<CatalogItemCover>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<CatalogItemCover>(cover =>
        {
            cover.HasKey(x => x.CatalogItemId);
            cover.HasOne<CatalogItem>().WithOne(x => x.Cover)
                .HasForeignKey<CatalogItemCover>(x => x.CatalogItemId).OnDelete(DeleteBehavior.Cascade);
            cover.Property(x => x.Version).IsConcurrencyToken();
        });
    }
}
