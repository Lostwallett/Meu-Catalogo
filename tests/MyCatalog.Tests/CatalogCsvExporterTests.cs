using System.Text;
using MyCatalog.Models;
using MyCatalog.Services;

namespace MyCatalog.Tests;

public class CatalogCsvExporterTests
{
    private const string Header = "Código;Título;Tipo;Autor ou diretor;Ano de lançamento;Gênero;Descrição;Favorito\r\n";

    private static string Read(byte[] bytes) => new UTF8Encoding(false, true).GetString(bytes, 3, bytes.Length - 3);

    [Fact]
    public void EmptyExportHasUtf8BomAndPortugueseHeaderOnly()
    {
        var bytes = CatalogCsvExporter.Export([]);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        Assert.Equal(Header, Read(bytes));
    }

    [Theory]
    [InlineData(true, "Sim")]
    [InlineData(false, "Não")]
    public void ExportsAllColumnsWithSemicolonsAccentsAndFavorite(bool favorite, string expected)
    {
        var item = new CatalogItem
        {
            Id = 42, Title = "Título", Type = "Série", AuthorOrDirector = "José",
            ReleaseYear = 2020, Genre = "Ficção", Description = "Ação e emoção", IsFavorite = favorite
        };
        Assert.Equal(Header + $"\"42\";\"Título\";\"Série\";\"José\";\"2020\";\"Ficção\";\"Ação e emoção\";\"{expected}\"\r\n",
            Read(CatalogCsvExporter.Export([item])));
    }

    [Fact]
    public void QuotesSemicolonsAndLineBreaksArePreservedInsideEscapedFields()
    {
        var item = new CatalogItem { Title = "Um; \"título\"", Description = "Linha 1\r\nLinha 2\nLinha 3" };
        var csv = Read(CatalogCsvExporter.Export([item]));
        Assert.Contains("\"Um; \"\"título\"\"\"", csv);
        Assert.Contains("\"Linha 1\r\nLinha 2\nLinha 3\"", csv);
    }

    [Fact]
    public void NullOptionalFieldsAreEmptyAndRowsKeepInputOrder()
    {
        var csv = Read(CatalogCsvExporter.Export([
            new CatalogItem { Id = 2, Title = "Segundo", Type = "Livro" },
            new CatalogItem { Id = 1, Title = "Primeiro", Type = "Filme" }
        ]));
        Assert.Equal(Header +
            "\"2\";\"Segundo\";\"Livro\";\"\";\"\";\"\";\"\";\"Não\"\r\n" +
            "\"1\";\"Primeiro\";\"Filme\";\"\";\"\";\"\";\"\";\"Não\"\r\n", csv);
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+SUM(A1)")]
    [InlineData("-1+1")]
    [InlineData("@SUM(A1)")]
    [InlineData("  =1")]
    [InlineData("\ttexto")]
    [InlineData("\rtexto")]
    [InlineData("\ntexto")]
    [InlineData("＝1")]
    [InlineData("＋1")]
    [InlineData("－1")]
    [InlineData("＠SUM(A1)")]
    public void FormulaProtectionAppliesToEveryTextColumnWithoutMutatingItem(string value)
    {
        var item = new CatalogItem
        {
            Title = value, Type = value, AuthorOrDirector = value, Genre = value, Description = value
        };
        var csv = Read(CatalogCsvExporter.Export([item]));
        Assert.Equal(Header + $"\"0\";\"'{value}\";\"'{value}\";\"'{value}\";\"\";\"'{value}\";\"'{value}\";\"Não\"\r\n", csv);
        Assert.All(new[] { item.Title, item.Type, item.AuthorOrDirector, item.Genre, item.Description },
            actual => Assert.Equal(value, actual));
    }
}
