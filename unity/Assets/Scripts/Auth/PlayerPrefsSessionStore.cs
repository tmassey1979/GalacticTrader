using GalacticTrader.Desktop.Api;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using UnityEngine;

namespace GalacticTrader.Unity.Auth;

public sealed class PlayerPrefsSessionStore : IClientSessionStore
{
    private const string SessionKey = "gt.session.v1";

    public Task<DesktopSession?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var payload = PlayerPrefs.GetString(SessionKey, string.Empty);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return Task.FromResult<DesktopSession?>(null);
        }

        try
        {
            var session = JsonSerializer.Deserialize<DesktopSession>(payload);
            return Task.FromResult(session);
        }
        catch (JsonException)
        {
            PlayerPrefs.DeleteKey(SessionKey);
            return Task.FromResult<DesktopSession?>(null);
        }
    }

    public Task SaveAsync(DesktopSession session, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(session);
        PlayerPrefs.SetString(SessionKey, payload);
        PlayerPrefs.Save();
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        PlayerPrefs.DeleteKey(SessionKey);
        PlayerPrefs.Save();
        return Task.CompletedTask;
    }
}
