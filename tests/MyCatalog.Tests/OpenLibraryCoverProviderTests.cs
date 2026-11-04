using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using MyCatalog.Services.Covers;

namespace MyCatalog.Tests;

public class OpenLibraryCoverProviderTests
{
    private const string SearchJson = """
        {"numFound":12,"docs":[{"key":"/works/OL100W","title":"Dom Casmurro","author_name":["Machado de Assis"],"edition_count":22},{"key":"https://bad.invalid","title":"Inválido"}]}
        """;
    private const string EditionsJson = """
        {"size":21,"entries":[
        {"key":"/books/OL200M","title":"Dom Casmurro","covers":[123,123,-1,456],"publishers":["Editora"],"publish_date":"2020","languages":[{"key":"/languages/por"}],"isbn_13":["9780000000000"]},
        {"key":"/books/OL201M","title":"Sem imagem","covers":[-1]},
        {"key":"/books/invalid","covers":[789]}]}
        """;

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        public int Count { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Count++; return action(request, token); }
    }

    private sealed class Fixture : IDisposable
    {
        public Handler Handler { get; }
        public BookCoverOptions Options { get; } = new() { Enabled = true, ContactEmail = "tests@example.invalid" };
        private readonly OpenLibraryRequestGate _gate = new(TimeProvider.System);
        private readonly OpenLibraryCache _cache = new();
        private readonly HttpClient _http;
        public OpenLibraryCoverProvider Provider { get; }
        public Fixture(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action)
        {
            Handler = new Handler(action);
            _http = new HttpClient(Handler);
            Provider = new OpenLibraryCoverProvider(_http, Microsoft.Extensions.Options.Options.Create(Options), _gate, _cache, TimeProvider.System);
        }
        public Fixture(string json) : this((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) })) { }
        public void Dispose() { _http.Dispose(); _gate.Dispose(); _cache.Dispose(); }
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("a@example.invalid\r\nInjected: header")]
    public async Task MissingOrInvalidContactBlocksAllHttpCalls(string contact)
    {
        using var f = new Fixture(SearchJson);
        f.Options.ContactEmail = contact;
        Assert.False(f.Provider.IsConfigured);
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.SearchAsync("Dom", "Machado", 1, default));
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.EditionsAsync("OL100W", 1, default));
        Assert.Equal(0, f.Handler.Count);
    }

    [Fact]
    public async Task FeatureDisabledBlocksCallsEvenWithValidContact()
    {
        using var f = new Fixture(SearchJson);
        f.Options.Enabled = false;
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.SearchAsync("Dom", null, 1, default));
        Assert.Equal(0, f.Handler.Count);
    }

    [Fact]
    public async Task SearchEscapesParametersIdentifiesClientAndCachesMappedResults()
    {
        using var f = new Fixture((request, _) =>
        {
            Assert.Equal("openlibrary.org", request.RequestUri!.Host);
            Assert.Equal("https", request.RequestUri.Scheme);
            Assert.Contains("title=A%20%26%20B", request.RequestUri.Query);
            Assert.Contains("author=Jos%C3%A9", request.RequestUri.Query);
            Assert.Contains("tests@example.invalid", request.Headers.UserAgent.ToString());
            Assert.Equal("tests@example.invalid", request.Headers.From);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SearchJson) });
        });
        var result = await f.Provider.SearchAsync(" A & B ", "José", 1, default);
        Assert.Single(result.Items);
        Assert.Equal("OL100W", result.Items[0].WorkId);
        Assert.Equal("Machado de Assis", result.Items[0].Authors);
        Assert.True(result.HasNext);
        Assert.Equal(result, await f.Provider.SearchAsync("A & B", "José", 1, default));
        Assert.Equal(1, f.Handler.Count);
    }

    [Fact]
    public async Task EditionsUsePagedEndpointAndRejectMissingDuplicateOrInvalidImages()
    {
        using var f = new Fixture((request, _) =>
        {
            Assert.EndsWith("/works/OL100W/editions.json?limit=10&offset=10", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(EditionsJson) });
        });
        var page = await f.Provider.EditionsAsync("OL100W", 2, default);
        Assert.Equal(new long[] { 123, 456 }, page.Items.Select(x => x.CoverId));
        Assert.All(page.Items, edition =>
        {
            Assert.Equal("OL200M", edition.EditionId);
            Assert.Equal("Editora", edition.Publishers);
            Assert.Equal("2020", edition.PublishDate);
            Assert.Equal("por", edition.Languages);
            Assert.Equal("9780000000000", edition.Isbn);
        });
        Assert.True(page.HasNext);
    }

    [Theory]
    [InlineData("{\"docs\":[]}")]
    [InlineData("{}")]
    public async Task EmptyOrOptionalSearchFieldsAreSafe(string json)
    {
        using var f = new Fixture(json);
        Assert.Empty((await f.Provider.SearchAsync("Livro", null, 1, default)).Items);
    }

    [Fact]
    public async Task MissingEditionMetadataIsDisplayedAsNotInformed()
    {
        using var f = new Fixture("{\"entries\":[{\"key\":\"/books/OL1M\",\"covers\":[1]}]}");
        var edition = Assert.Single((await f.Provider.EditionsAsync("OL1W", 1, default)).Items);
        Assert.Equal("Não informado", edition.Title);
        Assert.Equal("Não informado", edition.Publishers);
        Assert.Equal("Não informado", edition.Isbn);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"docs\":\"unexpected\"}")]
    [InlineData("null")]
    public async Task InvalidJsonHasFriendlyError(string json)
    {
        using var f = new Fixture(json);
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.SearchAsync("Livro", null, 1, default));
    }

    [Theory]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(302)]
    public async Task UnsuccessfulResponsesAreNotCachedOrFollowed(int status)
    {
        using var f = new Fixture((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)));
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.SearchAsync("Livro", null, 1, default));
        Assert.Equal(1, f.Handler.Count);
    }

    [Fact]
    public async Task TooManyRequestsHonorsCooldownWithoutRetrying()
    {
        using var f = new Fixture((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(1));
            return Task.FromResult(response);
        });
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.SearchAsync("Livro", null, 1, default));
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.SearchAsync("Livro", null, 1, default));
        Assert.Equal(1, f.Handler.Count);
    }

    [Fact]
    public async Task TransientServerFailureRetriesOnceAndThenSucceeds()
    {
        var calls = 0;
        using var f = new Fixture((_, _) => Task.FromResult(++calls == 1
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SearchJson) }));
        Assert.Single((await f.Provider.SearchAsync("Livro", null, 1, default)).Items);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task NetworkFailureHasAtMostOneRetry()
    {
        using var f = new Fixture((_, _) => throw new HttpRequestException("offline"));
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.SearchAsync("Livro", null, 1, default));
        Assert.Equal(2, f.Handler.Count);
    }

    [Fact]
    public async Task TimeoutIsFriendlyButCallerCancellationPropagates()
    {
        using var f = new Fixture((_, _) => throw new TaskCanceledException());
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.SearchAsync("Livro", null, 1, default));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Provider.SearchAsync("Outro", null, 1, cancellation.Token));
    }

    [Fact]
    public async Task OversizedResponsesAreRejected()
    {
        using var f = new Fixture(new string('x', 1_048_577));
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.SearchAsync("Livro", null, 1, default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task InvalidPaginationNeverSendsHttp(int page)
    {
        using var f = new Fixture(SearchJson);
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.SearchAsync("Livro", null, page, default));
        Assert.Equal(0, f.Handler.Count);
    }

    [Fact]
    public async Task InvalidWorkNeverSendsHttp()
    {
        using var f = new Fixture(SearchJson);
        await Assert.ThrowsAsync<CoverProviderException>(() => f.Provider.EditionsAsync("../../bad", 1, default));
        Assert.Equal(0, f.Handler.Count);
    }

    [Fact]
    public async Task GateQueuesSecondRequestAndSupportsCancellation()
    {
        using var gate = new OpenLibraryRequestGate(TimeProvider.System);
        await gate.WaitAsync(default);
        using var cancellation = new CancellationTokenSource();
        var second = gate.WaitAsync(cancellation.Token);
        Assert.False(second.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
    }
}
