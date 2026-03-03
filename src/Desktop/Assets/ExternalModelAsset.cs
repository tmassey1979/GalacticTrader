namespace GalacticTrader.Desktop.Assets;

public sealed class ExternalModelAsset
{
    public required string RelativePath { get; init; }
    public required string SourceUrl { get; init; }
    public required string Attribution { get; init; }
}
