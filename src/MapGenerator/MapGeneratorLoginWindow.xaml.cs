using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Media;

namespace GalacticTrader.MapGenerator;

public partial class MapGeneratorLoginWindow : Window
{
    private readonly string _apiBaseUrl;

    public MapGeneratorLoginWindow(string apiBaseUrl)
    {
        _apiBaseUrl = apiBaseUrl;
        InitializeComponent();
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

        SetBusy(true);
        try
        {
            using var httpClient = new HttpClient { BaseAddress = apiBaseUri };
            var response = await httpClient.PostAsJsonAsync("/api/auth/login", new
            {
                username,
                password
            });

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                SetStatus($"Login failed ({(int)response.StatusCode}): {detail}", isError: true);
                return;
            }

            var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                SetStatus("Login succeeded but no bearer token was returned.", isError: true);
                return;
            }

            AccessToken = payload.AccessToken;
            DialogResult = true;
            Close();
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

    private sealed class LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
    }
}
