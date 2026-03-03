using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Dashboard;

public static class TerritoryConflictEventProjector
{
    private const float HighMomentumThreshold = 65f;
    private const float ContestedDominanceMin = 40f;
    private const float ContestedDominanceMax = 60f;

    public static IReadOnlyList<EventFeedEntry> Build(
        IReadOnlyList<TerritoryDominanceApiDto> dominanceRows,
        DateTime fallbackAtUtc)
    {
        return dominanceRows
            .Where(IsConflictSignal)
            .Select(row => new EventFeedEntry
            {
                OccurredAtUtc = row.UpdatedAt == default
                    ? fallbackAtUtc
                    : row.UpdatedAt.ToUniversalTime(),
                Category = "Territory",
                Title = $"Conflict watch: {row.FactionName}",
                Detail = $"Sectors {row.ControlledSectorCount} | Dominance {row.DominanceScore:N1}% | Momentum {row.WarMomentumScore:N1}%"
            })
            .ToArray();
    }

    private static bool IsConflictSignal(TerritoryDominanceApiDto row)
    {
        if (row.WarMomentumScore >= HighMomentumThreshold)
        {
            return true;
        }

        return row.DominanceScore > ContestedDominanceMin && row.DominanceScore < ContestedDominanceMax;
    }
}
