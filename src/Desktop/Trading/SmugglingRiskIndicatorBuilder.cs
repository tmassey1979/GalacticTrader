namespace GalacticTrader.Desktop.Trading;

public static class SmugglingRiskIndicatorBuilder
{
    public static SmugglingRiskIndicator Build(
        float riskPremium,
        float pirateActivityModifier,
        float monopolyModifier,
        float demandMultiplier)
    {
        var score =
            (riskPremium * 55f) +
            (pirateActivityModifier * 25f) +
            (monopolyModifier * 15f) +
            (MathF.Max(0f, demandMultiplier - 1f) * 10f);
        var clamped = Math.Clamp(score, 0f, 100f);

        var band = clamped switch
        {
            < 25f => "Low",
            < 50f => "Moderate",
            < 75f => "High",
            _ => "Critical"
        };

        return new SmugglingRiskIndicator
        {
            Score = clamped,
            Band = band
        };
    }
}
