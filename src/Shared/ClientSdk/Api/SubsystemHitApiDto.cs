namespace GalacticTrader.Desktop.Api;

public sealed class SubsystemHitApiDto
{
    public Guid AttackerShipId { get; init; }
    public Guid TargetShipId { get; init; }
    public int TargetSubsystem { get; init; }
    public int Damage { get; init; }
    public int RemainingSubsystemHp { get; init; }
    public bool SubsystemDisabled { get; init; }
}
