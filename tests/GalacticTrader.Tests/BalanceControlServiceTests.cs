namespace GalacticTrader.Tests;

using GalacticTrader.Services.Admin;

public sealed class BalanceControlServiceTests
{
    [Fact]
    public void SetTaxRate_ClampsToValidRange()
    {
        var service = new BalanceControlService();
        var snapshot = service.SetTaxRate(120m);

        Assert.Equal(100m, snapshot.TaxRatePercent);
    }

    [Fact]
    public void TriggerSectorInstability_AddsSectorToSnapshot()
    {
        var service = new BalanceControlService();
        var sectorId = Guid.NewGuid();

        var snapshot = service.TriggerSectorInstability(sectorId, "test");

        Assert.Contains(sectorId, snapshot.UnstableSectors);
    }

    [Fact]
    public void TriggerEconomicCorrection_ClampsToValidRange()
    {
        var service = new BalanceControlService();

        var snapshot = service.TriggerEconomicCorrection(-250m, "stress-test");

        Assert.Equal(-100m, snapshot.EconomicCorrectionPercent);
    }
}
