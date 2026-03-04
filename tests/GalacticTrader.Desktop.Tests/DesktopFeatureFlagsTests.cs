namespace GalacticTrader.Desktop.Tests;

public sealed class DesktopFeatureFlagsTests
{
    private const string FeatureFlagVariable = "GT_DESKTOP_ENABLE_3D_STARMAP";

    [Fact]
    public void FromEnvironment_Defaults3DToDisabled()
    {
        var previous = Environment.GetEnvironmentVariable(FeatureFlagVariable);
        try
        {
            Environment.SetEnvironmentVariable(FeatureFlagVariable, null);
            var flags = DesktopFeatureFlags.FromEnvironment();
            Assert.False(flags.EnableStarmap3D);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FeatureFlagVariable, previous);
        }
    }

    [Fact]
    public void FromEnvironment_HonorsExplicitEnableFlag()
    {
        var previous = Environment.GetEnvironmentVariable(FeatureFlagVariable);
        try
        {
            Environment.SetEnvironmentVariable(FeatureFlagVariable, "true");
            var flags = DesktopFeatureFlags.FromEnvironment();
            Assert.True(flags.EnableStarmap3D);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FeatureFlagVariable, previous);
        }
    }
}
