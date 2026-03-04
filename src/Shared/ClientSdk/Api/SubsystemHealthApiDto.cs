namespace GalacticTrader.Desktop.Api;

public sealed class SubsystemHealthApiDto
{
    public int Type { get; init; }
    public int CurrentHp { get; init; }
    public int MaxHp { get; init; }
    public bool IsOperational { get; init; }
}
