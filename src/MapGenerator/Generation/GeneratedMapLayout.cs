namespace GalacticTrader.MapGenerator.Generation;

public sealed record GeneratedMapLayout(IReadOnlyList<GeneratedSector> Sectors, IReadOnlyList<GeneratedRoute> Routes);
