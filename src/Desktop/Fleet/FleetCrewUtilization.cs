namespace GalacticTrader.Desktop.Fleet;

public sealed class FleetCrewUtilization
{
    public int CrewCount { get; init; }
    public int CrewSlots { get; init; }
    public float Ratio { get; init; }
    public required string Status { get; init; }
}
