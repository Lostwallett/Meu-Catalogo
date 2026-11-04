using System.Globalization;
using System.Text;

namespace MyCatalog.Services.Covers;

public static class BookCoverRelevance
{
    public static IReadOnlyList<BookWork> RankWorks(IEnumerable<BookWork> works, string title, string? author) =>
        works.DistinctBy(work => work.WorkId)
            .OrderByDescending(work => Score(work, title, author)).ToList();

    private static double Score(BookWork work, string title, string? author)
    {
        var titleScore = Similarity(title, work.Title);
        if (string.IsNullOrWhiteSpace(author)) return titleScore;
        var authorScore = work.Authors == "Não informado" ? 0 : Similarity(author, work.Authors);
        return .65 * titleScore + .35 * authorScore;
    }

    public static double Similarity(string left, string right)
    {
        left = Normalize(left);
        right = Normalize(right);
        if (left.Length == 0 || right.Length == 0) return 0;
        if (left == right) return 1;
        // Não confunde títulos numéricos distintos, como 1984 e 1985.
        if (left.All(char.IsDigit) && right.All(char.IsDigit)) return 0;
        var leftWords = left.Split(' ').ToHashSet();
        var rightWords = right.Split(' ').ToHashSet();
        var overlap = 2d * leftWords.Intersect(rightWords).Count() / (leftWords.Count + rightWords.Count);
        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        for (var i = 1; i <= left.Length; i++)
        {
            var current = new int[right.Length + 1];
            current[0] = i;
            for (var j = 1; j <= right.Length; j++)
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1));
            previous = current;
        }
        var editSimilarity = 1d - (double)previous[right.Length] / Math.Max(left.Length, right.Length);
        var score = Math.Max(.95 * overlap, .9 * editSimilarity);
        if (right.StartsWith(left + " ", StringComparison.Ordinal)) score = Math.Max(score, .9);
        return score;
    }

    private static string Normalize(string text)
    {
        var result = new StringBuilder();
        foreach (var character in text[..Math.Min(text.Length, 300)].Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            result.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }
        return string.Join(' ', result.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
