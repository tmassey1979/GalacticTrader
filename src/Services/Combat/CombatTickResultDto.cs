namespace GalacticTrader.Services.Combat;

public sealed class CombatTickResultDto
{
    public Guid CombatId { get; init; }
    public int TickNumber { get; init; }
    public CombatState State { get; init; }
    public int AttackerHull { get; init; }
    public int DefenderHull { get; init; }
    public int AttackerShields { get; init; }
    public int DefenderShields { get; init; }
    public List<SubsystemHitDto> Hits { get; init; } = [];
}
