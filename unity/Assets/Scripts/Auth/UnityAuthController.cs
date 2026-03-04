using GalacticTrader.Desktop.Api;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GalacticTrader.Unity.Auth;

public sealed class UnityAuthController : MonoBehaviour
{
    [SerializeField] private string apiBaseUrl = "http://localhost:8080";

    private AuthSessionManager? _sessionManager;
    private HttpClient? _httpClient;

    public event Action<AuthOperationResult>? AuthResultChanged;

    public DesktopSession? CurrentSession => _sessionManager?.CurrentSession;

    public bool IsInitialized => _sessionManager is not null;

    private void Awake()
    {
        var baseUri = apiBaseUrl.TrimEnd('/');
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUri)
        };

        var authClient = new AuthApiClient(_httpClient);
        var store = new PlayerPrefsSessionStore();
        _sessionManager = new AuthSessionManager(authClient, store);
    }

    private void OnDestroy()
    {
        _httpClient?.Dispose();
        _httpClient = null;
    }

    public async Task RestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_sessionManager is null)
        {
            Publish(AuthOperationResult.Failure(AuthFailureState.Unknown, "Auth controller is not initialized."));
            return;
        }

        var result = await _sessionManager.RestoreSessionAsync(cancellationToken);
        Publish(result);
    }

    public async Task LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (_sessionManager is null)
        {
            Publish(AuthOperationResult.Failure(AuthFailureState.Unknown, "Auth controller is not initialized."));
            return;
        }

        var result = await _sessionManager.LoginAsync(username, password, cancellationToken);
        Publish(result);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (_sessionManager is null)
        {
            Publish(AuthOperationResult.Failure(AuthFailureState.Unknown, "Auth controller is not initialized."));
            return;
        }

        var result = await _sessionManager.LogoutAsync(cancellationToken);
        Publish(result);
    }

    private void Publish(AuthOperationResult result)
    {
        AuthResultChanged?.Invoke(result);
    }
}
