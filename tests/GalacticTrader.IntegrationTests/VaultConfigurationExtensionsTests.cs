using GalacticTrader.API.Secrets;
using Microsoft.Extensions.Configuration;

namespace GalacticTrader.IntegrationTests;

public sealed class VaultConfigurationExtensionsTests
{
    [Fact]
    public void AddVaultSecretsIfConfigured_DoesNothing_WhenDisabled()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Vault:Enabled"] = "false",
            ["ConnectionStrings:Default"] = "Host=localhost;Database=gt"
        });

        configuration.AddVaultSecretsIfConfigured();

        Assert.Equal("Host=localhost;Database=gt", configuration["ConnectionStrings:Default"]);
    }

    [Fact]
    public void AddVaultSecretsIfConfigured_Throws_WhenEnabledAndAddressMissing()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Vault:Enabled"] = "true",
            ["Vault:Token"] = "token-value"
        });

        var error = Assert.Throws<InvalidOperationException>(() => configuration.AddVaultSecretsIfConfigured());

        Assert.Contains("Vault__Address", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddVaultSecretsIfConfigured_Throws_WhenEnabledAndTokenMissing()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Vault:Enabled"] = "true",
            ["Vault:Address"] = "http://127.0.0.1:8200"
        });

        var error = Assert.Throws<InvalidOperationException>(() => configuration.AddVaultSecretsIfConfigured());

        Assert.Contains("Vault__Token", error.Message, StringComparison.Ordinal);
    }
}
