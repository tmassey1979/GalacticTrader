namespace GalacticTrader.Desktop.Api;

public sealed class DesktopApiOptions
{
    public required string BaseUrl { get; init; }

    public static DesktopApiOptions FromEnvironment()
    {
        var baseUrl = Environment.GetEnvironmentVariable("GT_API_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "http://localhost:8080";
        }

        return new DesktopApiOptions
        {
            BaseUrl = baseUrl.TrimEnd('/')
        };
    }
}
