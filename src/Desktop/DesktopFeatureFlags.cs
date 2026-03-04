namespace GalacticTrader.Desktop;

public sealed class DesktopFeatureFlags
{
    public bool EnableStarmap3D { get; init; }

    public static DesktopFeatureFlags FromEnvironment()
    {
        return new DesktopFeatureFlags
        {
            EnableStarmap3D = ResolveBoolean("GT_DESKTOP_ENABLE_3D_STARMAP", defaultValue: false)
        };
    }

    private static bool ResolveBoolean(string variableName, bool defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        return bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }
}
