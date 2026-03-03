namespace GalacticTrader.API.Contracts;

public sealed record CreateIntelligenceNetworkApiRequest(Guid OwnerPlayerId, string Name, int AssetCount, float CoverageScore);
