using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Starmap;
using System.Net.Http;
using System.Windows;

namespace GalacticTrader.Desktop;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var splash = new SplashWindow();
        splash.Show();

        await splash.PlayAsync();
        splash.Close();

        try
        {
            var apiOptions = DesktopApiOptions.FromEnvironment();
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiOptions.BaseUrl)
            };

            var authApiClient = new AuthApiClient(httpClient);
            var loginWindow = new LoginWindow(authApiClient, apiOptions.BaseUrl);
            var loginAccepted = loginWindow.ShowDialog();
            if (loginAccepted != true || loginWindow.Session is null)
            {
                Shutdown();
                return;
            }

            var navigationApiClient = new NavigationApiClient(httpClient);
            navigationApiClient.SetBearerToken(loginWindow.Session.AccessToken);

            var starmapLoader = new DatabaseStarmapSceneLoader(navigationApiClient);
            var scene = await starmapLoader.LoadAsync();

            var mainWindow = new MainWindow(scene, loginWindow.Session.Username);
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"Startup failed: {exception.Message}",
                "Galactic Trader",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }
}
