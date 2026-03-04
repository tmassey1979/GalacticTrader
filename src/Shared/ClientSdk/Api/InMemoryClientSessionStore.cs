namespace GalacticTrader.Desktop.Api;

public sealed class InMemoryClientSessionStore : IClientSessionStore
{
    private DesktopSession? _session;

    public Task<DesktopSession?> LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_session);
    }

    public Task SaveAsync(DesktopSession session, CancellationToken cancellationToken = default)
    {
        _session = session;
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _session = null;
        return Task.CompletedTask;
    }
}
