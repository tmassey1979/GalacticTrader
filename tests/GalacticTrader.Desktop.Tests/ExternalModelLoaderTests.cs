using GalacticTrader.Desktop.Assets;

namespace GalacticTrader.Desktop.Tests;

public sealed class ExternalModelLoaderTests
{
    [Fact]
    public void Catalog_HasExpectedOnlineModels()
    {
        Assert.NotEmpty(ExternalModelCatalog.All);

        foreach (var asset in ExternalModelCatalog.All)
        {
            Assert.StartsWith("Assets/Models/", asset.RelativePath);
            Assert.StartsWith("https://", asset.SourceUrl);
            Assert.NotEmpty(asset.Attribution);
        }
    }

    [Fact]
    public void Loader_LoadsConfiguredAssets()
    {
        var loader = new ExternalModelLoader();

        foreach (var asset in ExternalModelCatalog.All)
        {
            var model = loader.TryLoad(asset);

            Assert.NotNull(model);
            Assert.NotEmpty(model.Children);
        }
    }

    [Fact]
    public void Loader_ReturnsNull_WhenAssetPathIsMissing()
    {
        var loader = new ExternalModelLoader();
        var missingAsset = new ExternalModelAsset
        {
            RelativePath = "Assets/Models/does-not-exist.glb",
            SourceUrl = "https://example.invalid/does-not-exist.glb",
            Attribution = "test"
        };

        var model = loader.TryLoad(missingAsset);

        Assert.Null(model);
    }
}
