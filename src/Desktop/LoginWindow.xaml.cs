using GalacticTrader.Desktop.Api;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GalacticTrader.Desktop;

public partial class LoginWindow : Window
{
    private static readonly TimeSpan LoginTimeout = TimeSpan.FromSeconds(15);

    private readonly AuthApiClient _authApiClient;
    private readonly string _apiBaseUrl;
    private bool _isAuthenticating;

    public LoginWindow(AuthApiClient authApiClient, string apiBaseUrl)
    {
        _authApiClient = authApiClient;
        _apiBaseUrl = apiBaseUrl;
        InitializeComponent();
        ApiBaseUrlText.Text = $"API endpoint: {apiBaseUrl}";
        SetStatus("Awaiting credentials.", isError: false);
    }

    public DesktopSession? Session { get; private set; }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        await TryLoginAsync();
    }

    private async void OnCredentialsKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter)
        {
            return;
        }

        e.Handled = true;
        await TryLoginAsync();
    }

    private async Task TryLoginAsync()
    {
        if (_isAuthenticating)
        {
            return;
        }

        var username = Normalize(LoginUsernameText.Text);
        var password = LoginPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Username and password are required.", isError: true);
            return;
        }

        _isAuthenticating = true;
        SetBusy(true);
        using var loginTimeoutCts = new CancellationTokenSource(LoginTimeout);
        try
        {
            SetStatus("Signing in to command services...", isError: false);
            Session = await _authApiClient.LoginAsync(username, password, loginTimeoutCts.Token);
            SetStatus("Login successful. Opening command interface...", isError: false);
            await Task.Delay(250);
            DialogResult = true;
            Close();
        }
        catch (OperationCanceledException) when (loginTimeoutCts.IsCancellationRequested)
        {
            SetStatus("Login timed out after 15 seconds. Verify API availability and try again.", isError: true);
        }
        catch (Exception exception)
        {
            SetStatus(BuildErrorMessage(exception), isError: true);
        }
        finally
        {
            SetBusy(false);
            _isAuthenticating = false;
        }
    }

    private void OnOpenCreateUserClick(object sender, RoutedEventArgs e)
    {
        var createWindow = new CreateUserWindow(_authApiClient, _apiBaseUrl)
        {
            Owner = this
        };

        var created = createWindow.ShowDialog();
        if (created != true)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(createWindow.CreatedUsername))
        {
            LoginUsernameText.Text = createWindow.CreatedUsername;
        }

        if (!string.IsNullOrWhiteSpace(createWindow.CreatedPassword))
        {
            LoginPasswordBox.Password = createWindow.CreatedPassword;
        }

        SetStatus("User created. Sign in with the new account.", isError: false);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SetBusy(bool isBusy)
    {
        LoginButton.IsEnabled = !isBusy;
        OpenCreateUserButton.IsEnabled = !isBusy;
        CancelButton.IsEnabled = !isBusy;
        LoginProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        LoginButton.Content = isBusy ? "Signing In..." : "Sign In";
        Mouse.OverrideCursor = isBusy ? Cursors.Wait : null;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 145, 145))
            : new SolidColorBrush(Color.FromRgb(156, 231, 186));
    }

    private static string BuildErrorMessage(Exception exception)
    {
        var message = exception.Message?.Trim();
        if (!string.IsNullOrWhiteSpace(message))
        {
            return $"Login failed: {message}";
        }

        return "Login failed due to an unexpected error. Check API and identity services, then try again.";
    }

    private static string Normalize(string value) => value.Trim();
}
