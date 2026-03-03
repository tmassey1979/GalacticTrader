using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using GalacticTrader.MapGenerator.Api;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace GalacticTrader.MapGenerator;

public partial class MapGeneratorLoginWindow : Window
{
    private readonly string _apiBaseUrl;
    private readonly MapGeneratorIdentityOptions _identityOptions;

    public MapGeneratorLoginWindow(string apiBaseUrl)
    {
        _apiBaseUrl = apiBaseUrl;
        _identityOptions = MapGeneratorIdentityOptions.FromEnvironment(apiBaseUrl);
        InitializeComponent();
        LoginHintText.Text =
            $"API login is preferred for local dev. Keycloak fallback: {_identityOptions.KeycloakBaseUrl}/realms/{_identityOptions.Realm}.";
    }

    public string? AccessToken { get; private set; }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        var username = UsernameText.Text.Trim();
        var password = PasswordText.Password;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Username and password are required.", isError: true);
            return;
        }

        if (!Uri.TryCreate(_apiBaseUrl, UriKind.Absolute, out var apiBaseUri))
        {
            SetStatus("API base URL is invalid.", isError: true);
            return;
        }

        if (!Uri.TryCreate(_identityOptions.KeycloakBaseUrl, UriKind.Absolute, out var keycloakBaseUri))
        {
            SetStatus("Keycloak base URL is invalid.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            using var httpClient = new HttpClient();
            var apiError = await TryLoginAgainstApiAsync(httpClient, apiBaseUri, username, password);
            if (string.IsNullOrWhiteSpace(apiError))
            {
                return;
            }

            var keycloakError = await TryLoginAgainstKeycloakAsync(httpClient, keycloakBaseUri, username, password);
            if (string.IsNullOrWhiteSpace(keycloakError))
            {
                return;
            }

            SetStatus($"API login failed: {apiError} | Keycloak login failed: {keycloakError}", isError: true);
        }
        catch (Exception exception)
        {
            SetStatus(exception.Message, isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task<string?> TryLoginAgainstApiAsync(HttpClient httpClient, Uri apiBaseUri, string username, string password)
    {
        httpClient.BaseAddress = apiBaseUri;

        var response = await httpClient.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });

        if (!response.IsSuccessStatusCode)
        {
            return $"status {(int)response.StatusCode}";
        }

        var payload = await response.Content.ReadFromJsonAsync<ApiLoginResponse>();
        if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            return "token missing from API login response";
        }

        AccessToken = payload.AccessToken;
        DialogResult = true;
        Close();
        return null;
    }

    private async Task<string?> TryLoginAgainstKeycloakAsync(HttpClient httpClient, Uri keycloakBaseUri, string username, string password)
    {
        var tokenEndpoint = BuildTokenEndpoint(keycloakBaseUri, _identityOptions);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _identityOptions.ClientId,
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid profile email"
        });

        var response = await httpClient.PostAsync(tokenEndpoint, form);
        if (!response.IsSuccessStatusCode)
        {
            return $"status {(int)response.StatusCode}";
        }

        var payload = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>();
        if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            return "token missing from Keycloak response";
        }

        AccessToken = payload.AccessToken;
        DialogResult = true;
        Close();
        return null;
    }

    private static string BuildTokenEndpoint(Uri keycloakBaseUri, MapGeneratorIdentityOptions options)
    {
        var escapedRealm = Uri.EscapeDataString(options.Realm);
        return $"{keycloakBaseUri.ToString().TrimEnd('/')}/realms/{escapedRealm}/protocol/openid-connect/token";
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SetBusy(bool isBusy)
    {
        LoginButton.IsEnabled = !isBusy;
        CancelButton.IsEnabled = !isBusy;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(125, 241, 166));
    }

    private sealed class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class ApiLoginResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; init; } = string.Empty;
    }
}
