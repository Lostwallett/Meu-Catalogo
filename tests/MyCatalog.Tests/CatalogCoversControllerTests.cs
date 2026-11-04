using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using MyCatalog.Controllers;
using MyCatalog.Data;
using MyCatalog.Models;
using MyCatalog.Services.Covers;

namespace MyCatalog.Tests;

public class CatalogCoversControllerTests
{
    private sealed class Provider : ICoverProvider
    {
        public bool IsConfigured { get; set; }
        public int Calls { get; private set; }
        public string? LastAuthor { get; private set; }
        public IReadOnlyList<BookWork> Works { get; set; } = [];
        public IReadOnlyList<CoverCandidate> Editions { get; set; } = [];
        public Task<CoverPage<BookWork>> SearchAsync(string title, string? author, int page, CancellationToken cancellationToken)
        { Calls++; LastAuthor = author; return Task.FromResult(new CoverPage<BookWork>(Works, page, false)); }
        public Task<CoverPage<CoverCandidate>> EditionsAsync(string workId, int page, CancellationToken cancellationToken)
        { Calls++; return Task.FromResult(new CoverPage<CoverCandidate>(Editions, page, false)); }
    }

    [Fact]
    public async Task DisabledFeatureDoesNotEvenQueryDatabaseOrProvider()
    {
        // Banco propositalmente sem esquema: a flag precisa funcionar antes da migration.
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
        var provider = new Provider();
        var controller = Create(context, provider, enabled: false);
        var result = Assert.IsType<ObjectResult>(await controller.Index(1, null, null));
        Assert.Equal(503, result.StatusCode);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task MissingContactShowsPendingConfigurationWithoutHttp()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var item = new CatalogItem { Title = "Livro", Type = "Livro" };
        context.Add(item);
        await context.SaveChangesAsync();
        var provider = new Provider();
        var controller = Create(context, provider, true);
        var result = Assert.IsType<ViewResult>(await controller.Index(item.Id, null, null, search: true));
        var model = Assert.IsType<BookCoverSearchViewModel>(result.Model);
        Assert.False(model.Configured);
        Assert.Contains("contato válido", model.Message);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task SelectionRejectsTokenForAnotherItemAndOnlyUpdatesCover()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var item = new CatalogItem { Title = "Livro", Type = "Livro", IsFavorite = true };
        context.Add(item);
        await context.SaveChangesAsync();
        var tokens = new CoverSelectionTokens(new EphemeralDataProtectionProvider(), TimeProvider.System);
        var controller = Create(context, new Provider(), true, tokens);
        var token = tokens.ForImage(item, new CoverCandidate("OL1W", "OL2M", 3, "Edição", "Editora", "2020", "por", ""));
        Assert.IsType<BadRequestObjectResult>(await controller.Select(item.Id + 1, token, default));
        Assert.IsType<RedirectToActionResult>(await controller.Select(item.Id, token, default));
        Assert.True((await context.CatalogItems.AsNoTracking().SingleAsync()).IsFavorite);
        Assert.Single(await context.CatalogItemCovers.ToListAsync());
    }

    [Fact]
    public async Task ExistingListAndDetailsWorkWithoutCoverTableWhenDisabled()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        // Replica somente o esquema antigo; nenhum comando de exclusão é necessário.
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE CatalogItems (Id INTEGER PRIMARY KEY, Title TEXT NOT NULL, Type TEXT NOT NULL,
            AuthorOrDirector TEXT, ReleaseYear INTEGER, Genre TEXT, Description TEXT, IsFavorite INTEGER NOT NULL);
            INSERT INTO CatalogItems (Id, Title, Type, IsFavorite) VALUES (1, 'Livro existente', 'Livro', 1);
            """);
        var controller = new CatalogItemsController(context, Options.Create(new BookCoverOptions()));
        Assert.IsType<ViewResult>(await controller.Index(null, null, null));
        var detail = Assert.IsType<ViewResult>(await controller.Details(1));
        Assert.Equal("Livro existente", Assert.IsType<CatalogItem>(detail.Model).Title);
    }

    [Fact]
    public void MutatingActionsRequireAntiforgeryValidation()
    {
        foreach (var action in new[] { nameof(CatalogCoversController.Select), nameof(CatalogCoversController.Remove) })
        {
            var method = typeof(CatalogCoversController).GetMethod(action)!;
            Assert.NotNull(Attribute.GetCustomAttribute(method, typeof(HttpPostAttribute)));
            Assert.NotNull(Attribute.GetCustomAttribute(method, typeof(ValidateAntiForgeryTokenAttribute)));
        }
    }

    [Fact]
    public async Task SearchWorkEditionAndConfirmationFlowUsesProtectedSelectionsOffline()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var item = new CatalogItem { Title = "Livro", Type = "Livro", AuthorOrDirector = "Autor" };
        context.Add(item);
        await context.SaveChangesAsync();
        var provider = new Provider
        {
            IsConfigured = true,
            Works = [new BookWork("OL1W", "Livro da fonte", "Autor da fonte", 1)],
            Editions = [new CoverCandidate("OL1W", "OL2M", 123, "Edição da fonte", "Editora", "2020", "por", "ISBN")]
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllersWithViews();
        using var serviceProvider = services.BuildServiceProvider();
        var controller = Create(context, provider, true);
        controller.ObjectValidator = serviceProvider.GetRequiredService<IObjectModelValidator>();
        var searchView = Assert.IsType<ViewResult>(await controller.Index(item.Id, "Livro", null, search: true));
        Assert.Null(provider.LastAuthor); // O usuário pode limpar o autor somente na pesquisa.
        var search = Assert.IsType<BookCoverSearchViewModel>(searchView.Model);
        var work = Assert.Single(search.Works);
        var editionsView = Assert.IsType<ViewResult>(await controller.Index(item.Id, null, null, workToken: work.Token));
        var editions = Assert.IsType<BookCoverSearchViewModel>(editionsView.Model);
        Assert.Equal("Autor da fonte", editions.SelectedWorkAuthors);
        var edition = Assert.Single(editions.Editions);
        Assert.Empty(await context.CatalogItemCovers.ToListAsync());
        Assert.IsType<RedirectToActionResult>(await controller.Select(item.Id, edition.Token, default));
        Assert.Equal("OL2M", (await context.CatalogItemCovers.SingleAsync()).ExternalItemId);
        Assert.Equal(2, provider.Calls);
    }

    private static CatalogCoversController Create(ApplicationDbContext context, Provider provider, bool enabled,
        CoverSelectionTokens? tokens = null) => new(context, provider,
            tokens ?? new CoverSelectionTokens(new EphemeralDataProtectionProvider(), TimeProvider.System),
            new CatalogCoverService(context), Options.Create(new BookCoverOptions { Enabled = enabled }))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TempProvider())
        };

    private sealed class TempProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
