using GalacticTrader.Desktop.Splash;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GalacticTrader.Desktop;

public partial class SplashWindow : Window
{
    private readonly TaskCompletionSource _playCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly DispatcherTimer _terminalTimer;
    private int _bootLineIndex;

    public SplashWindow()
    {
        InitializeComponent();
        BuildSplashScene();

        _terminalTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(220)
        };
        _terminalTimer.Tick += OnTerminalTick;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public Task PlayAsync() => _playCompletion.Task;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _terminalTimer.Start();

        if (FindResource("SplashStoryboard") is not Storyboard storyboardTemplate)
        {
            _playCompletion.TrySetResult();
            return;
        }

        var storyboard = storyboardTemplate.Clone();
        storyboard.Completed += (_, _) =>
        {
            LoadingProgress.Value = 100;
            _terminalTimer.Stop();
            _playCompletion.TrySetResult();
        };

        storyboard.Begin(this);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _terminalTimer.Stop();
    }

    private void OnTerminalTick(object? sender, EventArgs e)
    {
        if (_bootLineIndex >= SplashBootScript.Lines.Count)
        {
            _terminalTimer.Stop();
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        TerminalLog.Text += $"{timestamp}  {SplashBootScript.Lines[_bootLineIndex]}{Environment.NewLine}";
        _bootLineIndex++;
    }

    private void BuildSplashScene()
    {
        StarfieldVisual.Content = SplashStarfieldFactory.CreateStarfield();
        ShipModelGroup.Children.Clear();

        foreach (var model in SplashShipFactory.CreateShipModels())
        {
            ShipModelGroup.Children.Add(model);
        }
    }
}
