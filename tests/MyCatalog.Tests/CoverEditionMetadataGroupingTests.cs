using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using MyCatalog.Models;
using MyCatalog.Services.Covers;

namespace MyCatalog.Tests;

public class CoverEditionMetadataGroupingTests
{
    // Metadados públicos consultados em 04/11/2026. Nenhuma imagem foi baixada ou analisada.
    // https://openlibrary.org/works/OL24143668W/editions.json?limit=10&offset=0
    private const string ActualMetadata = """
        {"size":3,"entries":[
          {"key":"/books/OL47123394M","title":"1984","covers":[13509851],"isbn_10":["8467958820"],"isbn_13":["9788467958829"],"publishers":["NORMA EDITORIAL, S.A."],"publish_date":"Nov 25, 2022"},
          {"key":"/books/OL31851689M","title":"1984","covers":[10541664],"isbn_10":["2377315119"],"isbn_13":["9782377315116"],"publishers":["SARBACANE"],"publish_date":"16/02/2021","languages":[{"key":"/languages/fre"}]},
          {"key":"/books/OL31851688M","title":"1984","covers":[10541663],"isbn_10":["2377315119"],"isbn_13":["9782377315116"],"publishers":["SARBACANE"]}
        ]}
        """;

    private sealed class Handler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.Equal("/works/OL24143668W/editions.json", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ActualMetadata) });
        }
    }

    [Fact]
    public async Task Actual1984MetadataGroupsTwoSarbacaneRecordsAndKeepsNormaSeparate()
    {
        using var handler = new Handler();
        using var http = new HttpClient(handler);
        using var gate = new OpenLibraryRequestGate(TimeProvider.System);
        using var cache = new OpenLibraryCache();
        var provider = new OpenLibraryCoverProvider(http, Options.Create(new BookCoverOptions
        { Enabled = true, ContactEmail = "tests@example.invalid" }), gate, cache, TimeProvider.System);
        var page = await provider.EditionsAsync("OL24143668W", 1, default);
        Assert.Equal(3, page.Items.Count);
        var item = new CatalogItem { Id = 1, Type = "Livro", Title = "1984" };
        var tokens = new CoverSelectionTokens(new EphemeralDataProtectionProvider(), TimeProvider.System);
        var groups = CoverEditionGrouping.Group(page.Items.Select(x => (x, tokens.ForImage(item, x))), "1984");
        Assert.Equal(2, groups.Count);
        var french = Assert.Single(groups, x => x.Editions.Any(e => e.Cover.EditionId == "OL31851689M"));
        Assert.Equal(2, french.EditionCount);
        Assert.Equal(2, french.ImageCount);
        Assert.Equal(new long[] { 10541664, 10541663 }, french.Editions.Select(x => x.Cover.CoverId));
        Assert.All(french.Editions, edition =>
        {
            Assert.Contains("9782377315116", edition.Cover.Isbn);
            Assert.Contains("2377315119", edition.Cover.Isbn);
            var selection = tokens.Read(edition.Token, "image")!;
            Assert.Equal(edition.Cover.CoverId, selection.CoverId);
            Assert.Equal(edition.Cover.EditionId, selection.EditionId);
        });
        var incomplete = Assert.Single(french.Editions, x => x.Cover.EditionId == "OL31851688M");
        Assert.Equal("Não informado", incomplete.Cover.PublishDate);
        Assert.Equal("Não informado", incomplete.Cover.Languages);
        var norma = Assert.Single(groups, x => x.CoverId == 13509851);
        Assert.Single(norma.Editions);
        Assert.Equal("NORMA EDITORIAL, S.A.", norma.Primary.Cover.Publishers);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public void DifferentImagesForSameEditionAreGroupedWithoutLosingVariants()
    {
        var first = Candidate("OL1M", 1);
        var second = first with { CoverId = 2 };
        var group = Assert.Single(Group(first, second));
        Assert.Equal(1, group.EditionCount);
        Assert.Equal(2, group.ImageCount);
        Assert.Equal(2, group.Editions.Count);
    }

    [Fact]
    public void Isbn10And13WithFormattingAreEquivalentWhenPublisherConfirmsEdition()
    {
        var first = Candidate("OL1M", 1) with { Isbn = "2-37731-511-9" };
        var second = Candidate("OL2M", 2) with { Isbn = "978 2 37731 511 6", Publishers = "Sarbacane." };
        Assert.Single(Group(first, second));
    }

    [Theory]
    [InlineData("9782377315117")]
    [InlineData("2377315110")]
    [InlineData("ISBN não informado")]
    public void InvalidIsbnDoesNotProvideIdentityEvidence(string isbn)
    {
        Assert.Empty(EditionMetadataIdentity.Isbns(isbn));
        Assert.Equal(2, Group(Candidate("OL1M", 1) with { Isbn = isbn }, Candidate("OL2M", 2) with { Isbn = isbn }).Count);
    }

    [Fact]
    public void SameTitleAndPublisherWithDifferentValidIsbnsStaySeparate()
    {
        Assert.Equal(2, Group(Candidate("OL1M", 1), Candidate("OL2M", 2) with { Isbn = "9788467958829" }).Count);
    }

    [Theory]
    [InlineData("publisher")]
    [InlineData("missing-publisher")]
    [InlineData("date")]
    [InlineData("language")]
    public void IsbnGroupingRequiresPublisherAndRejectsKnownMetadataConflicts(string difference)
    {
        var first = Candidate("OL1M", 1);
        var second = Candidate("OL2M", 2);
        second = difference switch
        {
            "publisher" => second with { Publishers = "Outra editora" },
            "missing-publisher" => second with { Publishers = "Não informado" },
            "date" => second with { PublishDate = "2022" },
            _ => second with { Languages = "por" }
        };
        Assert.Equal(2, Group(first, second).Count);
    }

    [Fact]
    public void IncompleteRecordDoesNotBridgeConflictingEditions()
    {
        var incomplete = Candidate("OL1M", 1) with { PublishDate = "Não informado", Languages = "Não informado" };
        var first = Candidate("OL2M", 2);
        var later = Candidate("OL3M", 3) with { PublishDate = "2022" };
        var groups = Group(incomplete, first, later);
        Assert.Equal(2, groups.Count);
        Assert.DoesNotContain(groups, group => group.Editions.Any(x => x.Cover.EditionId == "OL2M") &&
            group.Editions.Any(x => x.Cover.EditionId == "OL3M"));
    }

    [Fact]
    public void SameImageRemainsGroupedAcrossDistinctEditionsEvenWhenMetadataDiffers()
    {
        var group = Assert.Single(Group(Candidate("OL1M", 1), Candidate("OL2M", 1) with
        { Isbn = "9788467958829", Publishers = "NORMA", PublishDate = "2022" }));
        Assert.Equal(2, group.EditionCount);
        Assert.Equal(1, group.ImageCount);
    }

    [Fact]
    public void MetadataVariantsOfSameEditionAndImageArePreserved()
    {
        var first = Candidate("OL1M", 1);
        var richer = first with { PublishDate = "16/02/2021", Languages = "fre, eng" };
        var group = Assert.Single(Group(first, richer, first));
        Assert.Equal(2, group.Editions.Count);
        Assert.Contains(group.Editions, x => x.Cover.PublishDate == "16/02/2021");
    }

    [Fact]
    public void SameIsbnDoesNotJoinDifferentWorksAndLanguageAliasesDoNotConflict()
    {
        Assert.Equal(2, Group(Candidate("OL1M", 1), Candidate("OL2M", 2) with { WorkId = "OL2W" }).Count);
        Assert.Single(Group(Candidate("OL1M", 1) with { Languages = "fre, eng" },
            Candidate("OL2M", 2) with { Languages = "eng, fra" }));
    }

    private static CoverCandidate Candidate(string editionId, long imageId) => new("OL1W", editionId, imageId,
        "1984", "SARBACANE", "2021", "fre", "9782377315116");

    private static IReadOnlyList<CoverEditionGroup> Group(params CoverCandidate[] candidates) =>
        CoverEditionGrouping.Group(candidates.Select(x => (x, $"token-{x.EditionId}-{x.CoverId}")), "1984");
}
