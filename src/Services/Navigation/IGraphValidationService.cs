namespace GalacticTrader.Services.Navigation;

public interface IGraphValidationService
{
    Task EnsureSectorCanBeCreatedAsync(string sectorName, CancellationToken cancellationToken = default);
    Task EnsureRouteCanBeCreatedAsync(Guid fromSectorId, Guid toSectorId, CancellationToken cancellationToken = default);
    Task<GraphValidationReport> ValidateGraphAsync(CancellationToken cancellationToken = default);
}
