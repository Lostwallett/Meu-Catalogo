namespace MyCatalog.Services.Covers;

public record CoverPage<T>(IReadOnlyList<T> Items, int Page, bool HasNext);
public record BookWork(string WorkId, string Title, string Authors, int EditionCount);
public record CoverCandidate(string WorkId, string EditionId, long CoverId,
    string Title, string Publishers, string PublishDate, string Languages, string Isbn);

public interface ICoverProvider
{
    bool IsConfigured { get; }
    Task<CoverPage<BookWork>> SearchAsync(string title, string? author, int page, CancellationToken cancellationToken);
    Task<CoverPage<CoverCandidate>> EditionsAsync(string workId, int page, CancellationToken cancellationToken);
}

public class CoverProviderException(string message) : Exception(message);
