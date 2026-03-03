namespace GalacticTrader.Services.Fleet;

public sealed class CrewMemberDto
{
    public Guid Id { get; init; }
    public Guid PlayerId { get; init; }
    public Guid? ShipId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public int CombatSkill { get; init; }
    public int EngineeringSkill { get; init; }
    public int NavigationSkill { get; init; }
    public int Morale { get; init; }
    public int Loyalty { get; init; }
    public int ExperienceLevel { get; init; }
    public long ExperiencePoints { get; init; }
}
