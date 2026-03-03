namespace GalacticTrader.Services.Npc;

public sealed class CreateNpcRequest
{
    public string Name { get; init; } = string.Empty;
    public NpcArchetype Archetype { get; init; }
    public Guid? FactionId { get; init; }
    public Guid? StartingSectorId { get; init; }
}
