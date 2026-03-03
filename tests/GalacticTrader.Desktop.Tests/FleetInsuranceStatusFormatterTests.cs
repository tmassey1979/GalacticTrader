using GalacticTrader.Desktop.Fleet;

namespace GalacticTrader.Desktop.Tests;

public sealed class FleetInsuranceStatusFormatterTests
{
    [Fact]
    public void Format_ReturnsUninsured_WhenCoverageDisabled()
    {
        var value = FleetInsuranceStatusFormatter.Format(hasInsurance: false, insuranceRate: 0.02m);

        Assert.Equal("Uninsured", value);
    }

    [Fact]
    public void Format_ReturnsRate_WhenCoverageEnabled()
    {
        var value = FleetInsuranceStatusFormatter.Format(hasInsurance: true, insuranceRate: 0.015m);

        Assert.StartsWith("Insured (1.5", value, StringComparison.Ordinal);
    }
}
