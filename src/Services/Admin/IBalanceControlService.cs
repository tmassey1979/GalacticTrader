namespace GalacticTrader.Services.Admin;

public interface IBalanceControlService
{
    BalanceControlSnapshot GetSnapshot();
    BalanceControlSnapshot SetTaxRate(decimal taxRatePercent);
    BalanceControlSnapshot SetPirateIntensity(int intensityPercent);
    BalanceControlSnapshot ApplyLiquidityAdjustment(decimal deltaPercent, string reason);
    BalanceControlSnapshot TriggerSectorInstability(Guid sectorId, string reason);
    BalanceControlSnapshot TriggerEconomicCorrection(decimal adjustmentPercent, string reason);
}
