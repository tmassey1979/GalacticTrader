using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Modules;

public static class TerritoryHeatmapProjector
{
    public static IReadOnlyList<TerritoryDominanceDisplayRow> Build(
        IReadOnlyList<TerritoryDominanceApiDto> records,
        IReadOnlyDictionary<Guid, string> protectionPriorities,
        IReadOnlyDictionary<Guid, TerritoryEconomicPolicyApiDto> economicPolicies)
    {
        return records
            .OrderByDescending(static record => record.DominanceScore)
            .Select(record =>
            {
                var taxRatePercent = ResolveTaxRatePercent(record.FactionId, economicPolicies);
                var incentivePercent = ResolveTradeIncentivePercent(record.FactionId, economicPolicies);
                return new TerritoryDominanceDisplayRow
                {
                    FactionId = record.FactionId,
                    FactionName = record.FactionName,
                    ControlledSectorCount = record.ControlledSectorCount,
                    InfrastructureControlScore = record.InfrastructureControlScore,
                    WarMomentumScore = record.WarMomentumScore,
                    DominanceScore = record.DominanceScore,
                    HeatHex = ResolveHeatHex(record.DominanceScore),
                    ProtectionPriority = protectionPriorities.TryGetValue(record.FactionId, out var priority) ? priority : "None",
                    EconomicOutputPerSystem = TerritoryEconomicOutputProjector.BuildPerSystem(record, taxRatePercent, incentivePercent),
                    TaxRatePercent = taxRatePercent,
                    TradeIncentivePercent = incentivePercent
                };
            })
            .ToArray();
    }

    private static decimal ResolveTaxRatePercent(
        Guid factionId,
        IReadOnlyDictionary<Guid, TerritoryEconomicPolicyApiDto> economicPolicies)
    {
        return economicPolicies.TryGetValue(factionId, out var policy)
            ? decimal.Round(policy.TaxRate * 100m, 2)
            : 0m;
    }

    private static decimal ResolveTradeIncentivePercent(
        Guid factionId,
        IReadOnlyDictionary<Guid, TerritoryEconomicPolicyApiDto> economicPolicies)
    {
        return economicPolicies.TryGetValue(factionId, out var policy)
            ? decimal.Round(policy.TradeIncentiveModifier * 100m, 2)
            : 0m;
    }

    private static string ResolveHeatHex(float dominanceScore)
    {
        return dominanceScore switch
        {
            < 20f => "#2E7D32",
            < 40f => "#558B2F",
            < 60f => "#F9A825",
            < 80f => "#EF6C00",
            _ => "#C62828"
        };
    }
}
