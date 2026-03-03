namespace GalacticTrader.Services.Combat;

public sealed class SubsystemHitDto
{
    public Guid AttackerShipId { get; init; }
    public Guid TargetShipId { get; init; }
    public SubsystemType TargetSubsystem { get; init; }
    public int Damage { get; init; }
    public int RemainingSubsystemHp { get; init; }
    public bool SubsystemDisabled { get; init; }
}
