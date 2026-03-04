using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Starmap;

public sealed class DatabaseStarmapSceneLoader
{
    private readonly NavigationApiClient _navigationApiClient;
    private readonly bool _enable3DModels;

    public DatabaseStarmapSceneLoader(
        NavigationApiClient navigationApiClient,
        bool enable3DModels = true)
    {
        _navigationApiClient = navigationApiClient;
        _enable3DModels = enable3DModels;
    }

    public async Task<StarmapScene> LoadAsync(CancellationToken cancellationToken = default)
    {
        var sectors = await _navigationApiClient.GetSectorsAsync(cancellationToken);
        var routes = await _navigationApiClient.GetRoutesAsync(cancellationToken);

        var stars = DatabaseStarmapProjection.ToStars(sectors);
        var routeSegments = DatabaseStarmapProjection.ToRoutes(routes, sectors);
        var renderBudget = StarmapRenderBudget.FromEnvironment();
        return StarmapSceneBuilder.Build(stars, routeSegments, renderBudget, include3DModels: _enable3DModels);
    }
}
