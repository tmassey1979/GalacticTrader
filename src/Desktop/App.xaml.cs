using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Realtime;
using GalacticTrader.Desktop.Starmap;
using System.Net.Http;
using System.Windows;

namespace GalacticTrader.Desktop;

public partial class App : Application
{
    private HttpClient? _httpClient;

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
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiOptions.BaseUrl)
            };

            var authApiClient = new AuthApiClient(_httpClient);
            var loginWindow = new LoginWindow(authApiClient, apiOptions.BaseUrl);
            var loginAccepted = loginWindow.ShowDialog();
            if (loginAccepted != true || loginWindow.Session is null)
            {
                Shutdown();
                return;
            }

            var navigationApiClient = new NavigationApiClient(_httpClient);
            navigationApiClient.SetBearerToken(loginWindow.Session.AccessToken);
            var economyApiClient = new EconomyApiClient(_httpClient);
            economyApiClient.SetBearerToken(loginWindow.Session.AccessToken);
            var marketApiClient = new MarketApiClient(_httpClient);
            marketApiClient.SetBearerToken(loginWindow.Session.AccessToken);
            var fleetApiClient = new FleetApiClient(_httpClient);
            fleetApiClient.SetBearerToken(loginWindow.Session.AccessToken);
            var reputationApiClient = new ReputationApiClient(_httpClient);
            reputationApiClient.SetBearerToken(loginWindow.Session.AccessToken);
            var strategicApiClient = new StrategicApiClient(_httpClient);
            strategicApiClient.SetBearerToken(loginWindow.Session.AccessToken);
            var telemetryApiClient = new TelemetryApiClient(_httpClient);
            telemetryApiClient.SetBearerToken(loginWindow.Session.AccessToken);
            var marketIntelligenceApiClient = new MarketIntelligenceApiClient(_httpClient);
            marketIntelligenceApiClient.SetBearerToken(loginWindow.Session.AccessToken);
            var npcApiClient = new NpcApiClient(_httpClient);
            npcApiClient.SetBearerToken(loginWindow.Session.AccessToken);
            var combatApiClient = new CombatApiClient(_httpClient);
            combatApiClient.SetBearerToken(loginWindow.Session.AccessToken);
            var reconnectPolicy = new RealtimeReconnectPolicy();
            var strategicRealtimeClient = new StrategicRealtimeStreamClient(
                apiOptions.BaseUrl,
                loginWindow.Session.AccessToken,
                reconnectPolicy);
            var communicationRealtimeClient = new CommunicationRealtimeStreamClient(
                apiOptions.BaseUrl,
                loginWindow.Session.AccessToken,
                reconnectPolicy);

            var starmapLoader = new DatabaseStarmapSceneLoader(navigationApiClient);
            var scene = await starmapLoader.LoadAsync();

            var mainWindow = new MainWindow(
                scene,
                loginWindow.Session,
                navigationApiClient,
                economyApiClient,
                marketApiClient,
                fleetApiClient,
                reputationApiClient,
                strategicApiClient,
                telemetryApiClient,
                marketIntelligenceApiClient,
                npcApiClient,
                combatApiClient,
                strategicRealtimeClient,
                communicationRealtimeClient);
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

    protected override void OnExit(ExitEventArgs e)
    {
        _httpClient?.Dispose();
        base.OnExit(e);
    }
}
