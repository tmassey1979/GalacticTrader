namespace GalacticTrader.Desktop.Assets;

public static class ExternalModelCatalog
{
    public static ExternalModelAsset SplashShip { get; } = new()
    {
        RelativePath = "Assets/Models/dart_spacecraft.stl",
        SourceUrl = "https://raw.githubusercontent.com/nasa/NASA-3D-Resources/master/3D%20Printing/Double%20Asteroid%20Redirection%20Test%20(DART)/Double%20Asteroid%20Redirection%20Test%20(DART).stl",
        Attribution = "NASA 3D Resources (public domain U.S. Government work)"
    };

    public static ExternalModelAsset StarBody { get; } = new()
    {
        RelativePath = "Assets/Models/rq36_asteroid.glb",
        SourceUrl = "https://raw.githubusercontent.com/nasa/NASA-3D-Resources/master/3D%20Models/1999%20RQ36%20asteroid/1999%20RQ36%20asteroid.glb",
        Attribution = "NASA 3D Resources (public domain U.S. Government work)"
    };

    public static IReadOnlyList<ExternalModelAsset> All { get; } =
    [
        SplashShip,
        StarBody
    ];
}
