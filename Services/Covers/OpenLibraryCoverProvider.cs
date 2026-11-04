using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace MyCatalog.Services.Covers;

public class OpenLibraryCoverProvider(HttpClient http, IOptions<BookCoverOptions> options,
    OpenLibraryRequestGate gate, OpenLibraryCache cache, TimeProvider clock) : ICoverProvider
{
    private const int PageSize = 10;
    public bool IsConfigured => options.Value.Enabled && options.Value.HasValidContact;

    public async Task<CoverPage<BookWork>> SearchAsync(string title, string? author, int page, CancellationToken cancellationToken)
    {
        CheckConfiguration();
        ValidatePage(page);
        title = title.Trim();
        author = author?.Trim();
        if (title.Length is < 2 or > 150 || author?.Length > 150)
            throw new CoverProviderException("Informe um título entre 2 e 150 caracteres e um autor com até 150 caracteres.");
        var path = $"search.json?title={Uri.EscapeDataString(title)}&author={Uri.EscapeDataString(author ?? "")}&lang=pt&fields=key,title,author_name,edition_count&limit={PageSize}&page={page}";
        if (cache.TryGet<CoverPage<BookWork>>(path, out var cached)) return cached!;
        var result = await GetAsync<SearchResponse>(path, cancellationToken);
        var items = (result.Docs ?? []).OfType<Work>().Take(PageSize).Select(work => new BookWork(
            (work.Key ?? "").Replace("/works/", ""), Text(work.Title), Join(work.Authors), work.EditionCount))
            .Where(work => OpenLibraryReferences.IsWork(work.WorkId)).ToList();
        var response = new CoverPage<BookWork>(BookCoverRelevance.RankWorks(items, title, author), page,
            (long)page * PageSize < (result.NumFound ?? result.NumFoundLegacy));
        cache.Set(path, response, items.Count == 0);
        return response;
    }

    public async Task<CoverPage<CoverCandidate>> EditionsAsync(string workId, int page, CancellationToken cancellationToken)
    {
        CheckConfiguration();
        ValidatePage(page);
        if (!OpenLibraryReferences.IsWork(workId)) throw new CoverProviderException("Obra inválida.");
        var path = $"works/{workId}/editions.json?limit={PageSize}&offset={(page - 1) * PageSize}";
        if (cache.TryGet<CoverPage<CoverCandidate>>(path, out var cached)) return cached!;
        var result = await GetAsync<EditionResponse>(path, cancellationToken);
        var items = (result.Entries ?? []).OfType<Edition>().Take(PageSize)
            .Where(edition => OpenLibraryReferences.IsEdition((edition.Key ?? "").Replace("/books/", "")))
            .SelectMany(edition => (edition.Covers ?? []).Where(id => id > 0).Distinct().Take(3)
                .Select(id => new CoverCandidate(workId, edition.Key!.Replace("/books/", ""), id,
                    Text(edition.Title), Join(edition.Publishers), Text(edition.PublishDate),
                    Join(edition.Languages?.OfType<Language>().Select(x => x.Key?.Replace("/languages/", "") ?? "").ToArray()),
                    Join((edition.Isbn13 ?? []).Concat(edition.Isbn10 ?? []).Distinct().ToArray()))))
            .OrderByDescending(edition => CoverLanguages.IsPortuguese(edition.Languages))
            .ToList();
        var response = new CoverPage<CoverCandidate>(items, page, (long)page * PageSize < result.Size);
        cache.Set(path, response, items.Count == 0);
        return response;
    }

    private void CheckConfiguration()
    {
        if (!IsConfigured) throw new CoverProviderException("Pesquisa indisponível: configure a habilitação e um contato válido da Open Library.");
    }

    private static void ValidatePage(int page)
    {
        if (page is < 1 or > 100) throw new CoverProviderException("Página inválida. Refine a pesquisa para consultar outros resultados.");
    }
    private static string Text(string? value) => string.IsNullOrWhiteSpace(value) ? "Não informado" : value[..Math.Min(value.Length, 300)];
    private static string Join(string[]? values) => Text(values is null ? null : string.Join(", ", values.Take(8)));

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        // Endereço e caminhos controlados pela aplicação; não recebe URLs do usuário.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                await gate.WaitAsync(cancellationToken);
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                deadline.CancelAfter(TimeSpan.FromSeconds(8));
                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri("https://openlibrary.org/"), path));
                request.Headers.UserAgent.ParseAdd($"MyCatalog/1.0 ({options.Value.ContactEmail})");
                request.Headers.From = options.Value.ContactEmail;
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var delay = response.Headers.RetryAfter?.Delta ??
                        (response.Headers.RetryAfter?.Date - clock.GetUtcNow()) ?? TimeSpan.FromSeconds(5);
                    await gate.PauseAsync(delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1), cancellationToken);
                    throw new CoverProviderException("A Open Library pediu uma pausa. Aguarde antes de pesquisar novamente.");
                }
                if ((int)response.StatusCode >= 500 && attempt == 0) continue;
                if (!response.IsSuccessStatusCode) throw new CoverProviderException("Não foi possível consultar a Open Library. Tente novamente mais tarde.");
                await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
                using var buffer = new MemoryStream();
                var chunk = new byte[8192];
                int read;
                while ((read = await stream.ReadAsync(chunk, deadline.Token)) > 0)
                {
                    if (buffer.Length + read > 1_048_576) throw new CoverProviderException("A resposta recebida excedeu o limite permitido. Refine a pesquisa.");
                    buffer.Write(chunk, 0, read);
                }
                return JsonSerializer.Deserialize<T>(buffer.ToArray(), new JsonSerializerOptions { MaxDepth = 32 })
                    ?? throw new CoverProviderException("A Open Library retornou uma resposta vazia.");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            { throw new CoverProviderException("A consulta demorou mais que o esperado. Tente novamente."); }
            catch (HttpRequestException) when (attempt == 0) { }
            catch (HttpRequestException) { throw new CoverProviderException("A Open Library está indisponível. Tente novamente mais tarde."); }
            catch (IOException) when (attempt == 0) { }
            catch (IOException) { throw new CoverProviderException("A conexão com a Open Library foi interrompida. Tente novamente mais tarde."); }
            catch (JsonException) { throw new CoverProviderException("Não foi possível interpretar a resposta da Open Library."); }
        }
        throw new CoverProviderException("A Open Library está indisponível.");
    }

    private sealed class SearchResponse
    {
        [JsonPropertyName("numFound")] public int? NumFound { get; set; }
        [JsonPropertyName("num_found")] public int NumFoundLegacy { get; set; }
        [JsonPropertyName("docs")] public List<Work>? Docs { get; set; }
    }
    private sealed class Work
    {
        [JsonPropertyName("key")] public string? Key { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("author_name")] public string[]? Authors { get; set; }
        [JsonPropertyName("edition_count")] public int EditionCount { get; set; }
    }
    private sealed class EditionResponse
    {
        [JsonPropertyName("size")] public int Size { get; set; }
        [JsonPropertyName("entries")] public List<Edition>? Entries { get; set; }
    }
    private sealed class Edition
    {
        [JsonPropertyName("key")] public string? Key { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("covers")] public long[]? Covers { get; set; }
        [JsonPropertyName("publishers")] public string[]? Publishers { get; set; }
        [JsonPropertyName("publish_date")] public string? PublishDate { get; set; }
        [JsonPropertyName("languages")] public Language[]? Languages { get; set; }
        [JsonPropertyName("isbn_13")] public string[]? Isbn13 { get; set; }
        [JsonPropertyName("isbn_10")] public string[]? Isbn10 { get; set; }
    }
    private sealed class Language { [JsonPropertyName("key")] public string? Key { get; set; } }
}
