namespace GalacticTrader.Desktop.Modules;

public static class TerritoryEconomicPolicyInputParser
{
    public static bool TryParsePercent(string raw, decimal minimum, decimal maximum, out decimal parsed)
    {
        parsed = 0m;
        if (!decimal.TryParse(raw, out var value))
        {
            return false;
        }

        if (value < minimum || value > maximum)
        {
            return false;
        }

        parsed = decimal.Round(value, 2);
        return true;
    }
}
