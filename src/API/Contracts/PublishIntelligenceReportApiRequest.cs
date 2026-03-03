namespace GalacticTrader.API.Contracts;

public sealed record PublishIntelligenceReportApiRequest(Guid NetworkId, Guid SectorId, string SignalType, float ConfidenceScore, string Payload, int TtlMinutes);
