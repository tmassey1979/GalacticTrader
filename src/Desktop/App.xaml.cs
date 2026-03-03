using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Realtime;
using GalacticTrader.Desktop.Starmap;
using Serilog;
using Serilog.Events;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;

namespace GalacticTrader.Desktop;

public partial class App : Application
{
    private HttpClient? _httpClient;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ConfigureLogging();
        RegisterGlobalExceptionHandlers();
        Log.Information("Desktop startup initiated.");

        try
        {
            var splash = new SplashWindow();
            splash.Show();

            await splash.PlayAsync();
            splash.Close();

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
            var leaderboardApiClient = new LeaderboardApiClient(_httpClient);
            leaderboardApiClient.SetBearerToken(loginWindow.Session.AccessToken);
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
            var communicationApiClient = new CommunicationApiClient(_httpClient);
            communicationApiClient.SetBearerToken(loginWindow.Session.AccessToken);
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
            var starmapLoad = await StarmapSceneResolver.ResolveAsync(
                starmapLoader.LoadAsync,
                StarmapSceneBuilder.Build);
            if (starmapLoad.UsedFallback)
            {
                MessageBox.Show(
                    starmapLoad.Warning,
                    "Galactic Trader",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            var scene = starmapLoad.Scene;

            var mainWindow = new MainWindow(
                scene,
                loginWindow.Session,
                navigationApiClient,
                economyApiClient,
                marketApiClient,
                fleetApiClient,
                reputationApiClient,
                leaderboardApiClient,
                strategicApiClient,
                telemetryApiClient,
                marketIntelligenceApiClient,
                npcApiClient,
                combatApiClient,
                communicationApiClient,
                strategicRealtimeClient,
                communicationRealtimeClient);
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
            Log.Information("Desktop startup completed for user {Username}.", loginWindow.Session.Username);
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Desktop startup failed.");
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
        Log.Information("Desktop app exiting.");
        _httpClient?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ConfigureLogging()
    {
        var logServerUrl = Environment.GetEnvironmentVariable("GT_LOG_SERVER_URL");
        var logServerApiKey = Environment.GetEnvironmentVariable("GT_LOG_SERVER_API_KEY");

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "logs", "desktop-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true);

        if (!string.IsNullOrWhiteSpace(logServerUrl))
        {
            loggerConfiguration = loggerConfiguration.WriteTo.Seq(
                serverUrl: logServerUrl.Trim(),
                apiKey: string.IsNullOrWhiteSpace(logServerApiKey) ? null : logServerApiKey.Trim());
        }

        Log.Logger = loggerConfiguration.CreateLogger();
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI thread exception.");
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Log.Fatal(exception, "Unhandled AppDomain exception. IsTerminating: {IsTerminating}", e.IsTerminating);
            return;
        }

        Log.Fatal("Unhandled AppDomain exception object of type {Type}. IsTerminating: {IsTerminating}", e.ExceptionObject?.GetType().FullName ?? "unknown", e.IsTerminating);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception.");
        e.SetObserved();
    }
}
