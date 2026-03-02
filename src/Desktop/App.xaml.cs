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

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();

        splash.Close();
    }
}
