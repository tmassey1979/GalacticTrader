using GalacticTrader.Desktop.Api;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GalacticTrader.Desktop;

public partial class LoginWindow : Window
{
    private readonly AuthApiClient _authApiClient;
    private readonly string _apiBaseUrl;

    public LoginWindow(AuthApiClient authApiClient, string apiBaseUrl)
    {
        _authApiClient = authApiClient;
        _apiBaseUrl = apiBaseUrl;
        InitializeComponent();
        ApiBaseUrlText.Text = $"API endpoint: {apiBaseUrl}";
    }

    public DesktopSession? Session { get; private set; }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        var username = Normalize(LoginUsernameText.Text);
        var password = LoginPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Username and password are required.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            Session = await _authApiClient.LoginAsync(username, password);
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
        Mouse.OverrideCursor = isBusy ? Cursors.Wait : null;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 145, 145))
            : new SolidColorBrush(Color.FromRgb(156, 231, 186));
    }

    private static string Normalize(string value) => value.Trim();
}
