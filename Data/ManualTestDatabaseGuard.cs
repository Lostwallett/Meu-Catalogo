using Microsoft.Data.SqlClient;

namespace MyCatalog.Data;

// Usado somente quando um perfil informa ManualTest:ExpectedDatabase.
// Valida a configuração antes de abrir conexões ou executar o seed.
public static class ManualTestDatabaseGuard
{
    public static (string Server, string Database) Validate(string? connection, string expectedDatabase)
    {
        var parsed = new SqlConnectionStringBuilder(connection ?? "");
        if (!expectedDatabase.StartsWith("MyCatalogManualTest_", StringComparison.Ordinal) ||
            !string.Equals(parsed.InitialCatalog, expectedDatabase, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(parsed.DataSource, @"(localdb)\MSSQLLocalDB", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Perfil de teste interrompido: a conexão não corresponde ao LocalDB e ao banco isolado esperados. Nenhum seed foi executado.");

        return (parsed.DataSource, parsed.InitialCatalog);
    }
}
