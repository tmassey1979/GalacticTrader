namespace GalacticTrader.Desktop.Api;

public sealed class CombatTickResultApiDto
{
    public Guid CombatId { get; init; }
    public int TickNumber { get; init; }
    public int State { get; init; }
    public int AttackerHull { get; init; }
    public int DefenderHull { get; init; }
    public int AttackerShields { get; init; }
    public int DefenderShields { get; init; }
    public IReadOnlyList<SubsystemHitApiDto> Hits { get; init; } = [];
}
