namespace GalacticTrader.Desktop.Fleet;

public static class FleetCrewUtilizationProjector
{
    public static FleetCrewUtilization Build(int crewCount, int crewSlots)
    {
        var normalizedSlots = Math.Max(1, crewSlots);
        var clampedCrew = Math.Max(0, crewCount);
        var ratio = (float)clampedCrew / normalizedSlots;

        var status = ratio switch
        {
            < 0.6f => "Understaffed",
            <= 1.0f => "Nominal",
            _ => "Overstaffed"
        };

        return new FleetCrewUtilization
        {
            CrewCount = clampedCrew,
            CrewSlots = crewSlots,
            Ratio = ratio,
            Status = status
        };
    }
}
