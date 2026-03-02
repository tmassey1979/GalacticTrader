namespace GalacticTrader.MapGenerator.Api;

public sealed class MapGeneratorApiOptions
{
    public required string BaseUrl { get; init; }

    public static MapGeneratorApiOptions FromEnvironment()
    {
        var baseUrl = Environment.GetEnvironmentVariable("GT_API_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "http://localhost:8080";
        }

        return new MapGeneratorApiOptions
        {
            BaseUrl = baseUrl.TrimEnd('/')
        };
    }
}
