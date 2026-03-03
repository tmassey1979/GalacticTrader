using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Dashboard;

public static class DashboardAssetAllocationProjector
{
    public static IReadOnlyList<DashboardAssetAllocationSlice> Build(
        decimal liquidCredits,
        IReadOnlyList<ShipApiDto> ships,
        int maxSlices = 5)
    {
        var valuedSlices = new List<(string Label, decimal Value)>();

        if (liquidCredits > 0m)
        {
            valuedSlices.Add(("Liquid Credits", liquidCredits));
        }

        valuedSlices.AddRange(
            ships
                .Where(static ship => ship.CurrentValue > 0m)
                .GroupBy(static ship => NormalizeShipClass(ship.ShipClass))
                .Select(group => ($"{group.Key} Hulls", group.Sum(static ship => ship.CurrentValue))));

        if (valuedSlices.Count == 0)
        {
            return
            [
                new DashboardAssetAllocationSlice
                {
                    Label = "No assets",
                    Value = 0m,
                    Percent = 0m,
                    PieGlyph = "○"
                }
            ];
        }

        var cappedMaxSlices = Math.Max(2, maxSlices);
        var ordered = valuedSlices
            .OrderByDescending(static slice => slice.Value)
            .ToList();
        if (ordered.Count > cappedMaxSlices)
        {
            var keepCount = cappedMaxSlices - 1;
            var retained = ordered.Take(keepCount).ToList();
            var otherValue = ordered.Skip(keepCount).Sum(static slice => slice.Value);
            retained.Add(("Other Holdings", otherValue));
            ordered = retained;
        }

        var totalValue = ordered.Sum(static slice => slice.Value);
        return ordered
            .Select(slice =>
            {
                var percent = totalValue <= 0m
                    ? 0m
                    : decimal.Round((slice.Value / totalValue) * 100m, 1, MidpointRounding.AwayFromZero);
                return new DashboardAssetAllocationSlice
                {
                    Label = slice.Label,
                    Value = decimal.Round(slice.Value, 2, MidpointRounding.AwayFromZero),
                    Percent = percent,
                    PieGlyph = ResolvePieGlyph(percent)
                };
            })
            .ToArray();
    }

    private static string NormalizeShipClass(string shipClass)
    {
        return string.IsNullOrWhiteSpace(shipClass)
            ? "Unknown"
            : shipClass.Trim();
    }

    private static string ResolvePieGlyph(decimal percent)
    {
        return percent switch
        {
            >= 75m => "◕",
            >= 45m => "◑",
            >= 20m => "◔",
            > 0m => "◌",
            _ => "○"
        };
    }
}
