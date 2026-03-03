namespace GalacticTrader.Services.Combat;

public sealed class SubsystemHealthDto
{
    public SubsystemType Type { get; init; }
    public int CurrentHp { get; init; }
    public int MaxHp { get; init; }
    public bool IsOperational { get; init; }
}
