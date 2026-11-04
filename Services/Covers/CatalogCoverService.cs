using System.Globalization;
using System.Data;
using Microsoft.EntityFrameworkCore;
using MyCatalog.Data;
using MyCatalog.Models;

namespace MyCatalog.Services.Covers;

public class CatalogCoverService(ApplicationDbContext context)
{
    public async Task<bool> SaveAsync(CoverSelection selection, bool remove, CancellationToken cancellationToken)
    {
        if (selection.Purpose != (remove ? "remove" : "image")) return false;
        if (!remove && (!OpenLibraryReferences.IsWork(selection.WorkId) ||
            !OpenLibraryReferences.IsEdition(selection.EditionId) || selection.CoverId is not > 0)) return false;

        // A chamada externa já terminou. O snapshot evita aplicar uma pesquisa antiga
        // a um item editado e a transação protege a verificação de concorrência.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var item = await context.CatalogItems.AsNoTracking().Include(x => x.Cover)
            .SingleOrDefaultAsync(x => x.Id == selection.ItemId, cancellationToken);
        if (item is null || item.Type != "Livro" || !CoverSelectionTokens.Matches(selection, item)) return false;

        var cover = await context.CatalogItemCovers.SingleOrDefaultAsync(x => x.CatalogItemId == item.Id, cancellationToken);
        if (remove)
        {
            if (cover is not null) context.CatalogItemCovers.Remove(cover);
        }
        else
        {
            if (cover is null)
            {
                cover = new CatalogItemCover { CatalogItemId = item.Id };
                context.CatalogItemCovers.Add(cover);
            }
            cover.Provider = OpenLibraryReferences.Provider;
            cover.ExternalItemId = selection.EditionId!;
            cover.ExternalImageId = selection.CoverId!.Value.ToString(CultureInfo.InvariantCulture);
            cover.ExternalWorkId = selection.WorkId;
            cover.Version = Guid.NewGuid();
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException) { return false; }
        catch (DbUpdateException) { return false; }
    }
}
