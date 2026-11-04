using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyCatalog.Controllers;
using MyCatalog.Data;
using MyCatalog.Models;
using MyCatalog.Services.Covers;

namespace MyCatalog.Tests;

public class BookCoverSearchRefinementTests
{
    private sealed class JsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
    }

    private sealed class ApiFixture(string json) : IDisposable
    {
        private readonly HttpClient _http = new(new JsonHandler(json));
        private readonly OpenLibraryCache _cache = new();
        private readonly OpenLibraryRequestGate _gate = new(TimeProvider.System);
        public OpenLibraryCoverProvider Provider => new(_http, Options.Create(new BookCoverOptions
        {
            Enabled = true, ContactEmail = "tests@example.invalid"
        }), _gate, _cache, TimeProvider.System);
        public void Dispose() { _http.Dispose(); _cache.Dispose(); _gate.Dispose(); }
    }

    [Fact]
    public async Task SimulatedSearchRanksExactTitleAndAuthorBeforeNearMatchesAndDifferentAuthors()
    {
        using var api = new ApiFixture("""
            {"numFound":4,"docs":[
                {"key":"/works/OL1W","title":"1984","author_name":["Outro autor"]},
                {"key":"/works/OL2W","title":"1984: edição comentada","author_name":["George Orwell"]},
                {"key":"/works/OL3W","title":"1985","author_name":["George Orwell"]},
                {"key":"/works/OL4W","title":"1984","author_name":["George Orwell"]}
            ]}
            """);
        var result = await api.Provider.SearchAsync("1984", "George Orwell", 1, default);
        Assert.Equal(new[] { "OL4W", "OL2W", "OL1W", "OL3W" }, result.Items.Select(x => x.WorkId));
        Assert.Equal("1984: edição comentada", result.Items[1].Title);
    }

    [Fact]
    public async Task SimulatedSearchWithoutAuthorStillPrioritizesExactTitle()
    {
        using var api = new ApiFixture("""
            {"numFound":2,"docs":[
                {"key":"/works/OL1W","title":"Dom Casmurro: análise"},
                {"key":"/works/OL2W","title":"Dom Casmurro"}
            ]}
            """);
        Assert.Equal("OL2W", (await api.Provider.SearchAsync("Dom Casmurro", null, 1, default)).Items[0].WorkId);
    }

    [Theory]
    [InlineData("A FALÊNCIA", "a falencia")]
    [InlineData("Orgulho e Preconceito!", "Orgulho e Preconceito")]
    [InlineData("  George   Orwell ", "george orwell")]
    public void ComparisonIgnoresCaseAccentsPunctuationAndRepeatedSpaces(string left, string right)
    {
        Assert.Equal(1, BookCoverRelevance.Similarity(left, right));
    }

    [Fact]
    public void CloseSpellingRanksAboveUnrelatedTextAndNumericTitlesStayDistinct()
    {
        Assert.True(BookCoverRelevance.Similarity("Frankenstein", "Frankenstien") >
            BookCoverRelevance.Similarity("Frankenstein", "Outro livro"));
        Assert.Equal(0, BookCoverRelevance.Similarity("1984", "1985"));
    }

    [Fact]
    public void EqualScoresPreserveApiOrderAndDuplicateWorkIdsAreNotRepeated()
    {
        BookWork[] works = [new("OL2W", "Livro", "Autor", 1), new("OL1W", "Livro", "Autor", 2), new("OL2W", "Livro", "Autor", 1)];
        Assert.Equal(new[] { "OL2W", "OL1W" }, BookCoverRelevance.RankWorks(works, "Livro", "Autor").Select(x => x.WorkId));
    }

    [Theory]
    [InlineData("por", "Português")]
    [InlineData("pt", "Português")]
    [InlineData("pt-BR", "Português")]
    [InlineData("eng", "Inglês")]
    [InlineData("spa", "Espanhol")]
    [InlineData("fre", "Francês")]
    [InlineData("fra", "Francês")]
    [InlineData("/languages/por", "Português")]
    [InlineData("eng, por, pt, fra, fre", "Português, Inglês, Francês")]
    [InlineData("", "Não informado")]
    [InlineData("Não informado", "Não informado")]
    [InlineData("xyz", "Outro idioma (xyz)")]
    public void LanguageNamesAreFriendlyWithoutInventingUnknownLanguages(string codes, string expected)
    {
        Assert.Equal(expected, CoverLanguages.Display(codes));
    }

    [Theory]
    [InlineData("por", true)]
    [InlineData("eng, por", true)]
    [InlineData("pt-PT", true)]
    [InlineData("xpor", false)]
    [InlineData("eng", false)]
    [InlineData("Não informado", false)]
    public void PortuguesePreferenceMatchesWholeLanguageIdentifiers(string codes, bool expected)
    {
        Assert.Equal(expected, CoverLanguages.IsPortuguese(codes));
    }

    [Fact]
    public async Task SimulatedEditionsPrioritizePortugueseWithoutDroppingOtherLanguages()
    {
        using var api = new ApiFixture("""
            {"size":3,"entries":[
                {"key":"/books/OL1M","title":"1984","covers":[100],"languages":[{"key":"/languages/eng"}],"publishers":["English publisher"],"publish_date":"2001","isbn_13":["9781000000000"]},
                {"key":"/books/OL2M","title":"1984","covers":[200],"languages":[{"key":"/languages/por"}],"publishers":["Editora brasileira"],"publish_date":"2020","isbn_13":["9782000000000"]},
                {"key":"/books/OL3M","title":"1984","covers":[300],"languages":[{"key":"/languages/spa"}]}
            ]}
            """);
        var result = await api.Provider.EditionsAsync("OL1W", 1, default);
        Assert.Equal(new[] { "OL2M", "OL1M", "OL3M" }, result.Items.Select(x => x.EditionId));
        Assert.Equal("English publisher", result.Items[1].Publishers);
        Assert.Equal("2001", result.Items[1].PublishDate);
        Assert.Equal("9781000000000", result.Items[1].Isbn);
        Assert.Equal("eng", result.Items[1].Languages); // Os identificadores da fonte não são alterados.
    }

    [Fact]
    public void SameCoverIsGroupedButDistinctEditionsKeepMetadataAndOwnSelectionTokens()
    {
        var english = Candidate("OL1M", 100, "eng") with { Publishers = "English publisher", Isbn = "ISBN inglês" };
        var portuguese = Candidate("OL2M", 100, "por") with { Publishers = "Editora brasileira", Isbn = "ISBN português" };
        var otherImage = Candidate("OL2M", 200, "por");
        var groups = CoverEditionGrouping.Group([(english, "token-en"), (portuguese, "token-pt"),
            (english, "duplicate-token"), (otherImage, "other-image")], "1984");
        Assert.Single(groups); // Mesmo ID de edição: as duas referências de imagem ficam acessíveis no grupo.
        var shared = Assert.Single(groups, x => x.CoverId == 100);
        Assert.Equal(3, shared.Editions.Count);
        Assert.Equal("token-pt", shared.Primary.Token);
        var alternative = Assert.Single(shared.Editions, x => x.Cover.EditionId == "OL1M");
        Assert.Equal("token-en", alternative.Token);
        Assert.Equal("ISBN inglês", alternative.Cover.Isbn);
        Assert.Equal("English publisher", alternative.Cover.Publishers);
        Assert.Equal("2020", alternative.Cover.PublishDate);
    }

    [Fact]
    public void PortugueseCoverGroupPrecedesExactTitleInAnotherLanguage()
    {
        var groups = CoverEditionGrouping.Group([
            (Candidate("OL1M", 100, "eng"), "en"),
            (Candidate("OL2M", 200, "por") with { Title = "1984: nova edição" }, "pt")], "1984");
        Assert.Equal(200, groups[0].CoverId);
        Assert.Equal(2, groups.Count);
    }

    private static CoverCandidate Candidate(string edition, long image, string language) =>
        new("OL1W", edition, image, "1984", "Editora", "2020", language, "ISBN");

    private sealed class PagedProvider : ICoverProvider
    {
        public bool IsConfigured => true;
        public List<int> RequestedPages { get; } = [];
        public Task<CoverPage<BookWork>> SearchAsync(string title, string? author, int page, CancellationToken token) => throw new NotSupportedException();
        public Task<CoverPage<CoverCandidate>> EditionsAsync(string workId, int page, CancellationToken token)
        {
            RequestedPages.Add(page);
            return Task.FromResult(page == 1
                ? new CoverPage<CoverCandidate>([Candidate("OL1M", 100, "eng")], page, true)
                : new CoverPage<CoverCandidate>([Candidate("OL2M", 100, "por"), Candidate("OL3M", 200, "spa")], page, false));
        }
    }

    [Fact]
    public async Task LoadingMoreGroupsAcrossPagesAndDoesNotChangeExistingAssociation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var cover = new CatalogItemCover { Provider = "OpenLibrary", ExternalItemId = "OL99M", ExternalImageId = "999", ExternalWorkId = "OL1W" };
        var item = new CatalogItem { Title = "1984", Type = "Livro", AuthorOrDirector = "George Orwell", Cover = cover };
        context.Add(item);
        await context.SaveChangesAsync();
        var version = cover.Version;
        var tokens = new CoverSelectionTokens(new EphemeralDataProtectionProvider(), TimeProvider.System);
        var provider = new PagedProvider();
        var controller = new CatalogCoversController(context, provider, tokens, new CatalogCoverService(context), Options.Create(new BookCoverOptions { Enabled = true }));
        var workToken = tokens.ForWork(item, new BookWork("OL1W", "1984", "George Orwell", 20));
        var view = Assert.IsType<ViewResult>(await controller.Index(item.Id, null, null, workToken: workToken, page: 2));
        var model = Assert.IsType<BookCoverSearchViewModel>(view.Model);
        Assert.Equal(new[] { 1, 2 }, provider.RequestedPages);
        Assert.Equal(2, model.EditionGroups.Count);
        var shared = Assert.Single(model.EditionGroups, x => x.CoverId == 100);
        Assert.Equal("OL2M", shared.Primary.Cover.EditionId);
        Assert.All(shared.Editions, edition => Assert.Equal(edition.Cover.EditionId, tokens.Read(edition.Token, "image")!.EditionId));
        var saved = await context.CatalogItemCovers.AsNoTracking().SingleAsync();
        Assert.Equal("999", saved.ExternalImageId);
        Assert.Equal("OL99M", saved.ExternalItemId);
        Assert.Equal(version, saved.Version);
    }

    [Fact]
    public async Task RepeatedEditionPagesUseCacheWithoutRepeatedHttp()
    {
        using var api = new ApiFixture("{\"size\":1,\"entries\":[{\"key\":\"/books/OL1M\",\"covers\":[1]}]}");
        var first = await api.Provider.EditionsAsync("OL1W", 1, default);
        Assert.Same(first, await api.Provider.EditionsAsync("OL1W", 1, default));
    }
}
