namespace GalacticTrader.API.Contracts;

public sealed record DeclareCorporateWarApiRequest(Guid AttackerFactionId, Guid DefenderFactionId, string CasusBelli, int Intensity);
