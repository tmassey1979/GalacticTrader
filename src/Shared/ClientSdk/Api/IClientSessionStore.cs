namespace GalacticTrader.Desktop.Api;

public interface IClientSessionStore
{
    Task<DesktopSession?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(DesktopSession session, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
