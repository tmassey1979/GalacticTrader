namespace GalacticTrader.Desktop.Fleet;

public static class FleetInsuranceStatusFormatter
{
    public static string Format(bool hasInsurance, decimal insuranceRate)
    {
        if (!hasInsurance)
        {
            return "Uninsured";
        }

        return $"Insured ({insuranceRate:P1})";
    }
}
