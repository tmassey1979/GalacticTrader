namespace GalacticTrader.Services.Admin;

public sealed class BalanceControlService : IBalanceControlService
{
    private readonly Lock _sync = new();

    private decimal _taxRatePercent = 5m;
    private int _pirateIntensityPercent = 40;
    private decimal _liquidityAdjustmentPercent;
    private decimal _economicCorrectionPercent;
    private DateTime _lastUpdatedUtc = DateTime.UtcNow;
    private string _lastAction = "initialized";
    private readonly HashSet<Guid> _unstableSectors = [];

    public BalanceControlSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return Snapshot("read");
        }
    }

    public BalanceControlSnapshot SetTaxRate(decimal taxRatePercent)
    {
        lock (_sync)
        {
            _taxRatePercent = Math.Clamp(decimal.Round(taxRatePercent, 2), 0m, 100m);
            _lastUpdatedUtc = DateTime.UtcNow;
            _lastAction = "tax-rate-updated";
            return Snapshot(_lastAction);
        }
    }

    public BalanceControlSnapshot SetPirateIntensity(int intensityPercent)
    {
        lock (_sync)
        {
            _pirateIntensityPercent = Math.Clamp(intensityPercent, 0, 100);
            _lastUpdatedUtc = DateTime.UtcNow;
            _lastAction = "pirate-intensity-updated";
            return Snapshot(_lastAction);
        }
    }

    public BalanceControlSnapshot ApplyLiquidityAdjustment(decimal deltaPercent, string reason)
    {
        lock (_sync)
        {
            _liquidityAdjustmentPercent = Math.Clamp(_liquidityAdjustmentPercent + deltaPercent, -100m, 100m);
            _lastUpdatedUtc = DateTime.UtcNow;
            _lastAction = string.IsNullOrWhiteSpace(reason) ? "liquidity-adjusted" : $"liquidity-adjusted:{reason.Trim()}";
            return Snapshot(_lastAction);
        }
    }

    public BalanceControlSnapshot TriggerSectorInstability(Guid sectorId, string reason)
    {
        lock (_sync)
        {
            if (sectorId != Guid.Empty)
            {
                _unstableSectors.Add(sectorId);
            }

            _lastUpdatedUtc = DateTime.UtcNow;
            _lastAction = string.IsNullOrWhiteSpace(reason) ? "sector-instability-triggered" : $"sector-instability-triggered:{reason.Trim()}";
            return Snapshot(_lastAction);
        }
    }

    public BalanceControlSnapshot TriggerEconomicCorrection(decimal adjustmentPercent, string reason)
    {
        lock (_sync)
        {
            _economicCorrectionPercent = Math.Clamp(decimal.Round(adjustmentPercent, 2), -100m, 100m);
            _lastUpdatedUtc = DateTime.UtcNow;
            _lastAction = string.IsNullOrWhiteSpace(reason)
                ? "economic-correction-triggered"
                : $"economic-correction-triggered:{reason.Trim()}";
            return Snapshot(_lastAction);
        }
    }

    private BalanceControlSnapshot Snapshot(string action)
    {
        return new BalanceControlSnapshot
        {
            TaxRatePercent = _taxRatePercent,
            PirateIntensityPercent = _pirateIntensityPercent,
            LiquidityAdjustmentPercent = _liquidityAdjustmentPercent,
            EconomicCorrectionPercent = _economicCorrectionPercent,
            LastUpdatedUtc = _lastUpdatedUtc,
            LastAction = action,
            UnstableSectors = _unstableSectors.ToList()
        };
    }
}
