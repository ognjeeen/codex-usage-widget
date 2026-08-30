using System.Windows;
using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure;
using CodexUsageWidget.Infrastructure.Codex;
using CodexUsageWidget.Infrastructure.Codex.Hooks;
using CodexUsageWidget.Infrastructure.Logging;
using CodexUsageWidget.Infrastructure.Preview;
using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Infrastructure.Windows;
using CodexUsageWidget.Localization;
using CodexUsageWidget.Views;

namespace CodexUsageWidget;

public partial class App : System.Windows.Application, IDisposable
{
    private const string SingleInstanceMutexName = @"Local\CodexUsageWidget.SingleInstance";
    private SingleInstanceGuard? _singleInstanceGuard;
    private FileLogger? _logger;
    private GlobalExceptionHandler? _exceptionHandler;
    private AppThemeController? _themeController;
    private readonly AppLanguageController _languageController;
    private bool _disposed;

    public App()
        : this(new AppLanguageController(new LanguagePreferenceStore()))
    {
    }

    public App(AppLanguageController languageController)
    {
        ArgumentNullException.ThrowIfNull(languageController);
        _languageController = languageController;
    }

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
            var codexUsageProvider = new CodexUsageProvider(appServerSession);
            IUsageProvider usageProvider = codexUsageProvider;
            var usagePreviewEnabled = false;
#if DEBUG || USAGE_PREVIEW
            if (e.Args.Contains("--preview-usage", StringComparer.OrdinalIgnoreCase))
            {
                usagePreviewEnabled = true;
                usageProvider = new PreviewUsageProvider(codexUsageProvider);
                _logger.Info("Usage preview mode is active.");
            }
#endif
            var usageMonitor = new UsageMonitor(usageProvider);
            usageMonitor.DiagnosticMessage += (_, message) => _logger.Info(message);

            activityMonitor = new CodexActivityMonitor(new CodexActivityPipeSignalSource());
            activityMonitor.DiagnosticMessage += (_, message) => _logger.Info(message);
            var processPath = Environment.ProcessPath ??
                throw new InvalidOperationException("Cannot determine the widget executable path.");
            var startupRegistrationService = new StartupRegistrationService(
                processPath,
                usagePreviewEnabled ? new PreviewStartupRegistrationStore() : null);
            if (!startupRegistrationService.TryRefreshExecutablePathIfEnabled())
            {
                _logger.LogError("The Windows startup registration could not be refreshed.");
            }

            var activityHookSetupService = new CodexActivityHookSetupService(
                new CodexHookConfigurationManager(),
                appServerSession);
            _themeController = new AppThemeController(
                this,
                new ThemePreferenceMonitor(
                    new ThemePreferenceStore(),
                    new WindowsThemeMonitor()),
                new AccentPaletteStore());

            var window = new MainWindow(
                usageMonitor,
                activityMonitor,
                activityHookSetupService,
                new CodexCliLauncher(),
                new DisplayModeStore(),
                new WidgetDensityStore(),
                new DisplayedLimitPreferenceStore(),
                startupRegistrationService,
                new TrayIconService(),
                _themeController,
                _languageController);
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
                Strings.Format("App_StartupFailure", AppPaths.LogDirectory),
                Strings.Get("App_Name"),
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
        _themeController?.Dispose();
        _themeController = null;
        _singleInstanceGuard?.Dispose();
        _singleInstanceGuard = null;
        GC.SuppressFinalize(this);
    }
}
