namespace GalacticTrader.Data.Models;

public sealed class CorporateWar
{
    public Guid Id { get; set; }
    public Guid AttackerFactionId { get; set; }
    public Guid DefenderFactionId { get; set; }
    public string CasusBelli { get; set; } = string.Empty;
    public int Intensity { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }

    public Faction? AttackerFaction { get; set; }
    public Faction? DefenderFaction { get; set; }
}
