namespace GalacticTrader.API.Secrets;

using System.Globalization;
using System.Text.Json;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

public static class VaultConfigurationExtensions
{
    public static void AddVaultSecretsIfConfigured(this ConfigurationManager configuration)
    {
        var options = VaultBootstrapOptions.FromConfiguration(configuration);
        if (!options.Enabled)
        {
            return;
        }

        try
        {
            var secretMap = VaultSecretLoader.LoadAsync(options).GetAwaiter().GetResult();
            configuration.AddInMemoryCollection(secretMap);

            Console.WriteLine(
                $"[Vault] Loaded {secretMap.Count} secret entries from '{options.Mount}/{options.Path}'.");
        }
        catch (Exception exception)
        {
            if (options.FailFast)
            {
                throw new InvalidOperationException(
                    "Vault secret bootstrap failed and Vault__FailFast is enabled.",
                    exception);
            }

            Console.WriteLine($"[Vault] Secret bootstrap failed. Continuing with existing configuration. {exception.Message}");
        }
    }
}

internal sealed class VaultBootstrapOptions
{
    public required bool Enabled { get; init; }
    public required bool FailFast { get; init; }
    public required string Address { get; init; }
    public required string Token { get; init; }
    public required string Mount { get; init; }
    public required string Path { get; init; }
    public required int KvVersion { get; init; }

    public static VaultBootstrapOptions FromConfiguration(IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool?>("Vault:Enabled")
            ?? configuration.GetValue<bool?>("Vault__Enabled")
            ?? false;

        var failFast = configuration.GetValue<bool?>("Vault:FailFast")
            ?? configuration.GetValue<bool?>("Vault__FailFast")
            ?? true;

        var address = configuration["Vault:Address"]
            ?? configuration["Vault__Address"]
            ?? "";

        var token = configuration["Vault:Token"]
            ?? configuration["Vault__Token"]
            ?? "";

        var mount = configuration["Vault:Mount"]
            ?? configuration["Vault__Mount"]
            ?? "secret";

        var path = configuration["Vault:Path"]
            ?? configuration["Vault__Path"]
            ?? "galactictrader/api";

        var kvVersion = configuration.GetValue<int?>("Vault:KvVersion")
            ?? configuration.GetValue<int?>("Vault__KvVersion")
            ?? 2;

        if (enabled)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new InvalidOperationException("Vault is enabled but Vault__Address is not configured.");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Vault is enabled but Vault__Token is not configured.");
            }
        }

        return new VaultBootstrapOptions
        {
            Enabled = enabled,
            FailFast = failFast,
            Address = address,
            Token = token,
            Mount = mount,
            Path = path,
            KvVersion = kvVersion is 1 or 2 ? kvVersion : 2
        };
    }
}

internal static class VaultSecretLoader
{
    public static async Task<Dictionary<string, string?>> LoadAsync(VaultBootstrapOptions options)
    {
        var clientSettings = new VaultClientSettings(options.Address, new TokenAuthMethodInfo(options.Token));
        var client = new VaultClient(clientSettings);

        IDictionary<string, object?> secretData;
        if (options.KvVersion == 1)
        {
            var secret = await client.V1.Secrets.KeyValue.V1.ReadSecretAsync(path: options.Path, mountPoint: options.Mount);
            secretData = secret.Data.ToDictionary(
                entry => entry.Key,
                entry => (object?)entry.Value,
                StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            var secret = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: options.Path, mountPoint: options.Mount);
            secretData = secret.Data.Data.ToDictionary(
                entry => entry.Key,
                entry => (object?)entry.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in secretData)
        {
            if (string.IsNullOrWhiteSpace(key) || value is null)
            {
                continue;
            }

            var normalizedKey = key.Replace("__", ":", StringComparison.Ordinal);
            normalized[normalizedKey] = ConvertToString(value);
        }

        return normalized;
    }

    private static string ConvertToString(object value)
    {
        return value switch
        {
            string text => text,
            JsonElement jsonElement => jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                JsonValueKind.Number => jsonElement.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => jsonElement.GetRawText()
            },
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
