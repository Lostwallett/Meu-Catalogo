using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using MyCatalog.Models;

namespace MyCatalog.Services.Covers;

public record CoverSelection(int ItemId, string ItemTitle, string ItemType, string? ItemAuthor,
    Guid? ExpectedVersion, string WorkId, string? EditionId, long? CoverId, string Purpose,
    DateTimeOffset ExpiresAt, string? WorkTitle = null, string? WorkAuthors = null);

// Token assinado: o POST não aceita URLs nem identificadores sem prova de seleção.
public class CoverSelectionTokens(IDataProtectionProvider protection, TimeProvider clock)
{
    private readonly IDataProtector _protector = protection.CreateProtector("MyCatalog.BookCovers.v1");
    public string ForWork(CatalogItem item, BookWork work) => Protect(item, work.WorkId, null, null, "work", work.Title, work.Authors);
    public string ForImage(CatalogItem item, CoverCandidate image) => Protect(item, image.WorkId, image.EditionId, image.CoverId, "image");
    public string ForRemoval(CatalogItem item) => Protect(item, "", null, null, "remove");

    private string Protect(CatalogItem item, string workId, string? editionId, long? coverId, string purpose, string? workTitle = null, string? workAuthors = null) =>
        _protector.Protect(JsonSerializer.Serialize(new CoverSelection(item.Id, item.Title, item.Type,
            item.AuthorOrDirector, item.Cover?.Version, workId, editionId, coverId, purpose, clock.GetUtcNow().AddMinutes(20), workTitle, workAuthors)));

    public CoverSelection? Read(string? token, string purpose)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 8192) return null;
        try
        {
            var result = JsonSerializer.Deserialize<CoverSelection>(_protector.Unprotect(token));
            return result?.Purpose == purpose && result.ExpiresAt > clock.GetUtcNow() ? result : null;
        }
        catch (CryptographicException) { return null; }
        catch (JsonException) { return null; }
    }

    public static bool Matches(CoverSelection selection, CatalogItem item) => selection.ItemId == item.Id &&
        selection.ItemTitle == item.Title && selection.ItemType == item.Type &&
        selection.ItemAuthor == item.AuthorOrDirector && selection.ExpectedVersion == item.Cover?.Version;
}
