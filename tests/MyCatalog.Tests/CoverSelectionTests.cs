using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyCatalog.Data;
using MyCatalog.Models;
using MyCatalog.Services;
using MyCatalog.Services.Covers;

namespace MyCatalog.Tests;

public class CoverSelectionTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static readonly CoverCandidate Candidate = new("OL1W", "OL2M", 123, "Edição", "Editora", "2020", "por", "ISBN");

    [Fact]
    public void TokensRejectTamperingExpirationAndDifferentPurpose()
    {
        var clock = new Clock();
        var tokens = new CoverSelectionTokens(new EphemeralDataProtectionProvider(), clock);
        var item = new CatalogItem { Id = 1, Title = "Livro", Type = "Livro" };
        var token = tokens.ForImage(item, Candidate);
        Assert.NotNull(tokens.Read(token, "image"));
        Assert.Null(tokens.Read(token, "remove"));
        Assert.Null(tokens.Read("tampered" + token, "image"));
        Assert.Null(tokens.Read(new string('a', 8193), "image"));
        clock.Now += TimeSpan.FromMinutes(21);
        Assert.Null(tokens.Read(token, "image"));
    }

    [Fact]
    public async Task SelectionSurvivesNewContextWithoutChangingCatalogDataOrCsvAndCanBeReplacedAndRemoved()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        var tokens = new CoverSelectionTokens(new EphemeralDataProtectionProvider(), TimeProvider.System);
        string token;
        byte[] before;
        await using (var context = new ApplicationDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            await CatalogSeedData.InitializeAsync(context);
            var item = await context.CatalogItems.SingleAsync(x => x.Title == "Dom Casmurro");
            before = CatalogCsvExporter.Export(await context.CatalogItems.OrderBy(x => x.Id).ToListAsync());
            token = tokens.ForImage(item, Candidate);
        }
        await using (var context = new ApplicationDbContext(options))
        {
            Assert.True(await new CatalogCoverService(context).SaveAsync(tokens.Read(token, "image")!, false, default));
        }
        await using (var context = new ApplicationDbContext(options))
        {
            var item = await context.CatalogItems.Include(x => x.Cover).SingleAsync(x => x.Title == "Dom Casmurro");
            Assert.Equal("OL2M", item.Cover!.ExternalItemId);
            Assert.Equal("123", item.Cover.ExternalImageId);
            Assert.Equal("https://covers.openlibrary.org/b/id/123-L.jpg?default=false", OpenLibraryReferences.ImageUrl(item.Cover));
            Assert.Equal(25, await context.CatalogItems.CountAsync());
            Assert.Equal(before, CatalogCsvExporter.Export(await context.CatalogItems.OrderBy(x => x.Id).ToListAsync()));
            var replacement = tokens.Read(tokens.ForImage(item, Candidate with { CoverId = 456 }), "image")!;
            Assert.False(await new CatalogCoverService(context).SaveAsync(tokens.Read(token, "image")!, false, default));
            Assert.True(await new CatalogCoverService(context).SaveAsync(replacement, false, default));
        }
        await using (var context = new ApplicationDbContext(options))
        {
            var item = await context.CatalogItems.Include(x => x.Cover).SingleAsync(x => x.Title == "Dom Casmurro");
            Assert.Equal("456", item.Cover!.ExternalImageId);
            Assert.True(await new CatalogCoverService(context).SaveAsync(tokens.Read(tokens.ForRemoval(item), "remove")!, true, default));
        }
        await using (var context = new ApplicationDbContext(options))
        {
            Assert.Empty(await context.CatalogItemCovers.ToListAsync());
            Assert.Equal(before, CatalogCsvExporter.Export(await context.CatalogItems.OrderBy(x => x.Id).ToListAsync()));
        }
    }

    [Theory]
    [InlineData("title")]
    [InlineData("author")]
    [InlineData("type")]
    public async Task ItemEditsInvalidateOldSelection(string change)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var item = new CatalogItem { Title = "Livro", Type = "Livro", AuthorOrDirector = "Autor" };
        context.Add(item);
        await context.SaveChangesAsync();
        var tokens = new CoverSelectionTokens(new EphemeralDataProtectionProvider(), TimeProvider.System);
        var selection = tokens.Read(tokens.ForImage(item, Candidate), "image")!;
        if (change == "title") item.Title = "Alterado";
        if (change == "author") item.AuthorOrDirector = "Alterado";
        if (change == "type") item.Type = "Filme";
        await context.SaveChangesAsync();
        Assert.False(await new CatalogCoverService(context).SaveAsync(selection, false, default));
        Assert.Empty(await context.CatalogItemCovers.ToListAsync());
    }

    [Fact]
    public void StoredReferencesNeverProduceArbitraryUrls()
    {
        var invalid = new CatalogItemCover { Provider = "OpenLibrary", ExternalItemId = "https://bad.invalid", ExternalImageId = "123" };
        Assert.Null(OpenLibraryReferences.ImageUrl(invalid));
        Assert.Null(OpenLibraryReferences.SourceUrl(invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => OpenLibraryReferences.ImageUrl(-1));
    }

    [Fact]
    public void ModelKeepsCoversInSeparateTableWithoutAddingCatalogColumns()
    {
        // Não abre conexão nem altera variáveis globais do processo de testes.
        var model = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=unused;Database=UnusedDesignCheck;Trusted_Connection=True").Options);
        using (model)
        {
            var script = model.Database.GenerateCreateScript();
            Assert.Contains("CREATE TABLE [CatalogItemCovers]", script);
            Assert.DoesNotContain("ALTER TABLE [CatalogItems]", script);
        }
    }
}
