using Microsoft.EntityFrameworkCore;
using MyCatalog.Models;

namespace MyCatalog.Data;

// Representa a conexão e as tabelas que o EF Core manipula.
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
}
