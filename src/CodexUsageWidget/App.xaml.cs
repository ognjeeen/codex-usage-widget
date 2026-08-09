using System.Windows;
using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure;
using CodexUsageWidget.Infrastructure.Codex;
using CodexUsageWidget.Infrastructure.Codex.Hooks;
using CodexUsageWidget.Infrastructure.Logging;
using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Infrastructure.Windows;
using CodexUsageWidget.Views;

namespace CodexUsageWidget;

public partial class App : System.Windows.Application, IDisposable
{
    private const string SingleInstanceMutexName = @"Local\CodexUsageWidget.SingleInstance";
    private SingleInstanceGuard? _singleInstanceGuard;
    private FileLogger? _logger;
    private GlobalExceptionHandler? _exceptionHandler;
    private bool _disposed;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceGuard = SingleInstanceGuard.TryAcquire(SingleInstanceMutexName);
        if (_singleInstanceGuard is null)
        {
            Shutdown();
            return;
        }

        _logger = new FileLogger(AppPaths.LogDirectory);
        _exceptionHandler = new GlobalExceptionHandler(this, _logger);

        CodexActivityMonitor? activityMonitor = null;
        try
        {
            var appServerSession = new CodexAppServerSession();
            var usageProvider = new CodexUsageProvider(appServerSession);
            var usageMonitor = new UsageMonitor(usageProvider);
            usageMonitor.DiagnosticMessage += (_, message) => _logger.Info(message);

            activityMonitor = new CodexActivityMonitor(new CodexActivityPipeSignalSource());
            var processPath = Environment.ProcessPath ??
                throw new InvalidOperationException("Cannot determine the widget executable path.");
            var startupRegistrationService = new StartupRegistrationService(processPath);
            if (!startupRegistrationService.TryRefreshExecutablePathIfEnabled())
            {
                _logger.LogError("The Windows startup registration could not be refreshed.");
            }

            var activityHookSetupService = new CodexActivityHookSetupService(
                new CodexHookConfigurationManager(),
                appServerSession,
                processPath);

            var window = new MainWindow(
                usageMonitor,
                activityMonitor,
                activityHookSetupService,
                new CodexCliLauncher(),
                new DisplayModeStore(),
                new WidgetDensityStore(),
                startupRegistrationService,
                new TrayIconService());
            MainWindow = window;
            activityMonitor.StartAsync().GetAwaiter().GetResult();
            window.Show();
            if (window.StartsInTaskbarIndicatorMode)
            {
                window.Hide();
            }

            _logger.Info("Codex Usage Widget started.");
        }
        catch (Exception ex)
        {
            activityMonitor?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _logger.LogError("Application startup failed.", ex);
            System.Windows.MessageBox.Show(
                "Codex Usage Widget could not start. See the log under " + AppPaths.LogDirectory,
                "Codex Usage Widget",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Info("Codex Usage Widget stopped.");
        Dispose();
        base.OnExit(e);
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        base.OnSessionEnding(e);
        if (!e.Cancel && MainWindow is MainWindow window)
        {
            window.NotifySessionEnding();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _exceptionHandler?.Dispose();
        _exceptionHandler = null;
        _singleInstanceGuard?.Dispose();
        _singleInstanceGuard = null;
        GC.SuppressFinalize(this);
    }
}
