namespace GalacticTrader.Services.Strategic;

public sealed class CorporateWarDto
{
    public Guid Id { get; init; }
    public Guid AttackerFactionId { get; init; }
    public string AttackerFactionName { get; init; } = string.Empty;
    public Guid DefenderFactionId { get; init; }
    public string DefenderFactionName { get; init; } = string.Empty;
    public string CasusBelli { get; init; } = string.Empty;
    public int Intensity { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public bool IsActive { get; init; }
}
