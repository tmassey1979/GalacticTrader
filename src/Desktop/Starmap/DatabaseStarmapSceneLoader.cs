using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Starmap;

public sealed class DatabaseStarmapSceneLoader
{
    private readonly NavigationApiClient _navigationApiClient;

    public DatabaseStarmapSceneLoader(NavigationApiClient navigationApiClient)
    {
        _navigationApiClient = navigationApiClient;
    }

    public async Task<StarmapScene> LoadAsync(CancellationToken cancellationToken = default)
    {
        var sectors = await _navigationApiClient.GetSectorsAsync(cancellationToken);
        var routes = await _navigationApiClient.GetRoutesAsync(cancellationToken);

        var stars = DatabaseStarmapProjection.ToStars(sectors);
        var routeSegments = DatabaseStarmapProjection.ToRoutes(routes, sectors);
        var renderBudget = StarmapRenderBudget.FromEnvironment();
        return StarmapSceneBuilder.Build(stars, routeSegments, renderBudget);
    }
}
