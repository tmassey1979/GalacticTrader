namespace GalacticTrader.Desktop.Api;

public sealed class CombatSummaryApiDto
{
    public Guid CombatId { get; init; }
    public int State { get; init; }
    public Guid AttackerShipId { get; init; }
    public Guid DefenderShipId { get; init; }
    public Guid? WinnerShipId { get; init; }
    public int TickCount { get; init; }
    public int MaxTicks { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime? EndedAtUtc { get; init; }
    public int AttackerHull { get; init; }
    public int DefenderHull { get; init; }
    public IReadOnlyList<SubsystemHealthApiDto> AttackerSubsystems { get; init; } = [];
    public IReadOnlyList<SubsystemHealthApiDto> DefenderSubsystems { get; init; } = [];
}
