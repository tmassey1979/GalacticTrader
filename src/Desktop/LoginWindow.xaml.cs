using GalacticTrader.Desktop.Api;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GalacticTrader.Desktop;

public partial class LoginWindow : Window
{
    private readonly AuthApiClient _authApiClient;

    public LoginWindow(AuthApiClient authApiClient, string apiBaseUrl)
    {
        _authApiClient = authApiClient;
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

    private async void OnRegisterClick(object sender, RoutedEventArgs e)
    {
        var username = Normalize(RegisterUsernameText.Text);
        var email = Normalize(RegisterEmailText.Text);
        var password = RegisterPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Username, email, and password are required to create a user.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            await _authApiClient.RegisterAsync(username, email, password);
            LoginUsernameText.Text = username;
            LoginPasswordBox.Password = password;
            AuthTabs.SelectedIndex = 0;
            SetStatus("User created. Sign in with the new account.", isError: false);
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
        RegisterButton.IsEnabled = !isBusy;
        CancelButton.IsEnabled = !isBusy;
        Mouse.OverrideCursor = isBusy ? System.Windows.Input.Cursors.Wait : null;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(255, 147, 147))
            : new SolidColorBrush(Color.FromRgb(157, 183, 226));
    }

    private static string Normalize(string value) => value.Trim();
}
