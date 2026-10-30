using System.Globalization;
using System.Text;
using MyCatalog.Models;

namespace MyCatalog.Services;

public static class CatalogCsvExporter
{
    public static byte[] Export(IEnumerable<CatalogItem> items)
    {
        var csv = new StringBuilder();
        csv.Append("Código;Título;Tipo;Autor ou diretor;Ano de lançamento;Gênero;Descrição;Favorito\r\n");

        foreach (var item in items)
        {
            string?[] values =
            [
                item.Id.ToString(CultureInfo.InvariantCulture), item.Title, item.Type,
                item.AuthorOrDirector, item.ReleaseYear?.ToString(CultureInfo.InvariantCulture),
                item.Genre, item.Description, item.IsFavorite ? "Sim" : "Não"
            ];
            csv.AppendJoin(';', values.Select(Escape));
            csv.Append("\r\n");
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return [.. encoding.GetPreamble(), .. encoding.GetBytes(csv.ToString())];
    }

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        // Aspas CSV não impedem fórmulas. Prefixa apóstrofo somente no arquivo,
        // inclusive quando espaços ou controles antecedem o caractere perigoso.
        var start = value.AsSpan().TrimStart();
        if (value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n') ||
            (!start.IsEmpty && "=+-@＝＋－＠".Contains(start[0])))
            value = "'" + value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
