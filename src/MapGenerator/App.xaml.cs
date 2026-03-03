using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace GalacticTrader.MapGenerator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ConfigureLogging();
        RegisterGlobalExceptionHandlers();
        Log.Information("Map Generator startup initiated.");
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Map Generator exiting.");
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
                Path.Combine(AppContext.BaseDirectory, "logs", "map-generator-.log"),
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
        Log.Error(e.Exception, "Unhandled UI thread exception in Map Generator.");
        BreakIfDebugging();
        e.Handled = true;
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Log.Fatal(exception, "Unhandled AppDomain exception in Map Generator. IsTerminating: {IsTerminating}", e.IsTerminating);
            BreakIfDebugging();
            return;
        }

        Log.Fatal("Unhandled AppDomain exception object in Map Generator of type {Type}. IsTerminating: {IsTerminating}", e.ExceptionObject?.GetType().FullName ?? "unknown", e.IsTerminating);
        BreakIfDebugging();
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception in Map Generator.");
        BreakIfDebugging();
        e.SetObserved();
    }

    private static void BreakIfDebugging()
    {
        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
    }
}
