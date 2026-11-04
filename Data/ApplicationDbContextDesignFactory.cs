using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyCatalog.Data;

// Ferramentas EF não iniciam Program.cs nem executam seed. A conexão é explícita
// e, nesta etapa de validação, nunca pode apontar para o catálogo principal.
public class ApplicationDbContextDesignFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("MYCATALOG_MIGRATION_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Defina MYCATALOG_MIGRATION_CONNECTION para um banco isolado antes de usar as ferramentas EF.");
        var parsed = new SqlConnectionStringBuilder(connection);
        if (string.IsNullOrWhiteSpace(parsed.InitialCatalog) ||
            parsed.InitialCatalog.Equals("MyCatalogCompletoDb", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("As ferramentas de migration desta etapa exigem um banco isolado; MyCatalogCompletoDb está bloqueado.");
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
    }
}
