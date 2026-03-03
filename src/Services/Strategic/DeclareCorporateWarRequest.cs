namespace GalacticTrader.Services.Strategic;

public sealed class DeclareCorporateWarRequest
{
    public Guid AttackerFactionId { get; init; }
    public Guid DefenderFactionId { get; init; }
    public string CasusBelli { get; init; } = string.Empty;
    public int Intensity { get; init; }
}
