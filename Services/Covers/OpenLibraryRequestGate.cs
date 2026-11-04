namespace MyCatalog.Services.Covers;

// Singleton: limita todas as chamadas desta instância a uma por segundo, incluindo retries.
public sealed class OpenLibraryRequestGate(TimeProvider clock) : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private DateTimeOffset _next = DateTimeOffset.MinValue;

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var delay = _next - clock.GetUtcNow();
            if (delay > TimeSpan.FromSeconds(3))
                throw new CoverProviderException("A Open Library pediu uma pausa. Aguarde antes de tentar novamente.");
            if (delay > TimeSpan.Zero) await Task.Delay(delay, clock, cancellationToken);
            _next = clock.GetUtcNow().AddSeconds(1);
        }
        finally { _semaphore.Release(); }
    }

    public async Task PauseAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var until = clock.GetUtcNow() + delay;
            if (until > _next) _next = until;
        }
        finally { _semaphore.Release(); }
    }

    public void Dispose() => _semaphore.Dispose();
}
