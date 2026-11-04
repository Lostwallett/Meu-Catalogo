using System.Globalization;
using System.Text;

namespace MyCatalog.Services.Covers;

// Evidência bibliográfica, nunca comparação visual de imagens.
public static class EditionMetadataIdentity
{
    public static IReadOnlySet<string> Isbns(string? value) => (value ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(CanonicalIsbn).OfType<string>().ToHashSet(StringComparer.Ordinal);

    private static string? CanonicalIsbn(string value)
    {
        value = string.Concat(value.Where(c => c != '-' && !char.IsWhiteSpace(c))).ToUpperInvariant();
        if (value.Length == 10)
        {
            if (!value.Take(9).All(char.IsAsciiDigit) || !(char.IsAsciiDigit(value[9]) || value[9] == 'X')) return null;
            var checksum = value.Select((c, index) => (10 - index) * (c == 'X' ? 10 : c - '0')).Sum();
            if (checksum % 11 != 0) return null;
            var prefix = "978" + value[..9];
            var sum = prefix.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
            return prefix + ((10 - sum % 10) % 10).ToString(CultureInfo.InvariantCulture);
        }
        if (value.Length != 13 || !value.All(char.IsAsciiDigit) ||
            !(value.StartsWith("978", StringComparison.Ordinal) || value.StartsWith("979", StringComparison.Ordinal))) return null;
        return value.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum() % 10 == 0 ? value : null;
    }

    public static bool CanShareEdition(CoverCandidate left, CoverCandidate right)
    {
        // ISBN sozinho pode conter erros na fonte. Exige também editora conhecida e igual.
        var publisher = Normalize(left.Publishers);
        return left.WorkId == right.WorkId && publisher.Length > 0 && publisher == Normalize(right.Publishers) &&
            Isbns(left.Isbn).Overlaps(Isbns(right.Isbn)) && !Conflicts(left, right);
    }

    public static bool Conflicts(CoverCandidate left, CoverCandidate right)
    {
        var leftPublisher = Normalize(left.Publishers);
        var rightPublisher = Normalize(right.Publishers);
        var leftDate = Normalize(left.PublishDate);
        var rightDate = Normalize(right.PublishDate);
        if (leftPublisher.Length > 0 && rightPublisher.Length > 0 && leftPublisher != rightPublisher) return true;
        if (leftDate.Length > 0 && rightDate.Length > 0 && leftDate != rightDate) return true;
        // Só compara idiomas quando a fonte informou ambos. Ausência não é contradição.
        var leftLanguage = Normalize(left.Languages);
        var rightLanguage = Normalize(right.Languages);
        return leftLanguage.Length > 0 && rightLanguage.Length > 0 &&
            !CoverLanguages.Display(left.Languages).Split(", ").Order(StringComparer.Ordinal)
                .SequenceEqual(CoverLanguages.Display(right.Languages).Split(", ").Order(StringComparer.Ordinal));
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("Não informado", StringComparison.OrdinalIgnoreCase)) return "";
        var result = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(character)) result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }
}
