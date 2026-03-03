using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Trading;

public static class TradeSupplyDemandCurveBuilder
{
    public static TradeSupplyDemandCurveSnapshot Build(
        IReadOnlyList<TradeExecutionResultApiDto> transactions,
        int maxPoints = 14,
        double maxHeight = 64d)
    {
        var normalizedMaxPoints = Math.Max(3, maxPoints);
        var recent = transactions
            .Take(normalizedMaxPoints)
            .ToArray();
        var demandUnits = recent
            .Where(static transaction => transaction.ActionType == 0)
            .Sum(static transaction => transaction.Quantity);
        var supplyUnits = recent
            .Where(static transaction => transaction.ActionType != 0)
            .Sum(static transaction => transaction.Quantity);
        var totalUnits = Math.Max(1L, demandUnits + supplyUnits);
        if (recent.Length == 0)
        {
            return new TradeSupplyDemandCurveSnapshot
            {
                DemandUnits = demandUnits,
                SupplyUnits = supplyUnits,
                DemandRatio = 0m,
                SupplyRatio = 0m,
                Points = []
            };
        }

        var maxMagnitude = Math.Max(1d, recent.Max(static transaction => Math.Abs((double)transaction.Quantity)));
        var points = recent
            .Reverse()
            .Select((transaction, index) =>
            {
                var height = Math.Max(2d, Math.Abs(transaction.Quantity) / maxMagnitude * maxHeight);
                var isDemand = transaction.ActionType == 0;
                return new TradeSupplyDemandCurvePoint
                {
                    Label = $"{index + 1}",
                    Height = height,
                    BrushHex = isDemand ? "#67D7A5" : "#F5A85D"
                };
            })
            .ToArray();

        return new TradeSupplyDemandCurveSnapshot
        {
            DemandUnits = demandUnits,
            SupplyUnits = supplyUnits,
            DemandRatio = Math.Round((decimal)demandUnits / totalUnits, 3),
            SupplyRatio = Math.Round((decimal)supplyUnits / totalUnits, 3),
            Points = points
        };
    }
}
