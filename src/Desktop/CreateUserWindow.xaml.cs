using GalacticTrader.Desktop.Api;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GalacticTrader.Desktop;

public partial class CreateUserWindow : Window
{
    private readonly AuthApiClient _authApiClient;

    public CreateUserWindow(AuthApiClient authApiClient, string apiBaseUrl)
    {
        _authApiClient = authApiClient;
        InitializeComponent();
        StatusText.Text = $"API: {apiBaseUrl}";
    }

    public string? CreatedUsername { get; private set; }

    public string? CreatedPassword { get; private set; }

    private async void OnCreateClick(object sender, RoutedEventArgs e)
    {
        var username = Normalize(UsernameText.Text);
        var email = Normalize(EmailText.Text);
        var password = PasswordBox.Password;
        var firstName = NormalizeOptional(FirstNameText.Text);
        var lastName = NormalizeOptional(LastNameText.Text);
        var middleName = NormalizeOptional(MiddleNameText.Text);
        var nickname = NormalizeOptional(NicknameText.Text);
        var phoneNumber = NormalizeOptional(PhoneNumberText.Text);
        var locale = NormalizeOptional(LocaleText.Text);
        var timeZone = NormalizeOptional(TimeZoneText.Text);
        var website = NormalizeOptional(WebsiteText.Text);
        var birthdateInput = NormalizeOptional(BirthdateText.Text);
        var birthdate = TryParseBirthdate(birthdateInput);
        var gender = ResolveDropdownValue(GenderCombo);
        var pronouns = ResolveDropdownValue(PronounsCombo);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Username, email, and password are required.", isError: true);
            return;
        }

        if (birthdate is null)
        {
            SetStatus("Birthdate is required in YYYY-MM-DD format.", isError: true);
            return;
        }

        if (birthdate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            SetStatus("Birthdate cannot be in the future.", isError: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(gender))
        {
            SetStatus("Select a gender value.", isError: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(pronouns))
        {
            SetStatus("Select a pronouns value.", isError: true);
            return;
        }

        SetBusy(true);
        try
        {
            await _authApiClient.RegisterAsync(new RegisterPlayerRequestDto(
                Username: username,
                Email: email,
                Password: password,
                FirstName: firstName,
                LastName: lastName,
                MiddleName: middleName,
                Nickname: nickname,
                Birthdate: birthdate,
                Gender: gender,
                Pronouns: pronouns,
                PhoneNumber: phoneNumber,
                Locale: locale,
                TimeZone: timeZone,
                Website: website));

            CreatedUsername = username;
            CreatedPassword = password;
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
        CreateButton.IsEnabled = !isBusy;
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

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? ResolveDropdownValue(ComboBox comboBox)
    {
        var selected = (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (string.IsNullOrWhiteSpace(selected))
        {
            return null;
        }

        return selected;
    }

    private static DateOnly? TryParseBirthdate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoDate))
        {
            return isoDate;
        }

        if (DateOnly.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var localDate))
        {
            return localDate;
        }

        return null;
    }
}
