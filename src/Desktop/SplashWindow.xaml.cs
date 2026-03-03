using GalacticTrader.Desktop.Splash;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GalacticTrader.Desktop;

public partial class SplashWindow : Window
{
    private static readonly TimeSpan MinimumSplashDuration = TimeSpan.FromSeconds(20);
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

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _terminalTimer.Interval = CalculateTerminalInterval();
        _terminalTimer.Start();

        var storyboardCompletion = Task.CompletedTask;
        if (FindResource("SplashStoryboard") is Storyboard storyboardTemplate)
        {
            var storyboardTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var storyboard = storyboardTemplate.Clone();
            storyboard.Completed += (_, _) => storyboardTaskSource.TrySetResult();
            storyboard.Begin(this);
            storyboardCompletion = storyboardTaskSource.Task;
        }

        await Task.WhenAll(
            storyboardCompletion,
            Task.Delay(MinimumSplashDuration));

        LoadingProgress.Value = 100;
        _terminalTimer.Stop();
        _playCompletion.TrySetResult();
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

    private static TimeSpan CalculateTerminalInterval()
    {
        var lineCount = Math.Max(1, SplashBootScript.Lines.Count);
        var desiredIntervalMs = MinimumSplashDuration.TotalMilliseconds / lineCount;
        return TimeSpan.FromMilliseconds(Math.Max(220, desiredIntervalMs));
    }

    private void BuildSplashScene()
    {
        StarfieldVisual.Content = SplashStarfieldFactory.CreateStarfield();
        BackdropVisual.Content = SplashSceneFactory.CreateBackdropModels();
        ShipModelGroup.Children.Clear();

        foreach (var model in SplashShipFactory.CreateShipModels())
        {
            ShipModelGroup.Children.Add(model);
        }
    }
}
