using System.Globalization;
using System.Text.RegularExpressions;
using MyCatalog.Models;

namespace MyCatalog.Services.Covers;

public static partial class OpenLibraryReferences
{
    public const string Provider = "OpenLibrary";
    [GeneratedRegex("^OL[0-9]+W$", RegexOptions.CultureInvariant)] private static partial Regex WorkPattern();
    [GeneratedRegex("^OL[0-9]+M$", RegexOptions.CultureInvariant)] private static partial Regex EditionPattern();
    public static bool IsWork(string? value) => value is { Length: <= 80 } && WorkPattern().IsMatch(value);
    public static bool IsEdition(string? value) => value is { Length: <= 80 } && EditionPattern().IsMatch(value);
    public static string ImageUrl(long coverId, bool thumbnail = false) => coverId > 0
        ? $"https://covers.openlibrary.org/b/id/{coverId.ToString(CultureInfo.InvariantCulture)}-{(thumbnail ? "M" : "L")}.jpg?default=false"
        : throw new ArgumentOutOfRangeException(nameof(coverId));
    public static string? ImageUrl(CatalogItemCover? cover) => IsValid(cover)
        ? ImageUrl(long.Parse(cover!.ExternalImageId, CultureInfo.InvariantCulture)) : null;
    public static string? SourceUrl(CatalogItemCover? cover) => IsValid(cover)
        ? $"https://openlibrary.org/books/{cover!.ExternalItemId}" : null;
    private static bool IsValid(CatalogItemCover? cover) => cover?.Provider == Provider && IsEdition(cover.ExternalItemId) &&
        long.TryParse(cover.ExternalImageId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0;
}
