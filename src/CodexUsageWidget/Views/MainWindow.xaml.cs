using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;
using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Infrastructure.Windows;
using CodexUsageWidget.Views.ViewModels;

namespace CodexUsageWidget.Views;

public partial class MainWindow : Window
{
    private const double CompactBaseHeight = 236d;
    private const double CompactLimitRowHeight = 56d;
    private const double CompactMaximumHeight = 292d;
    private const double DetailedHeight = 620d;

    private readonly UsageMonitor _usageMonitor;
    private readonly CodexActivityMonitor _activityMonitor;
    private readonly ActivityHookSetupWindowController _activityHookSetupWindows;
    private readonly DisplayModeStore _displayModeStore;
    private readonly WidgetDensityStore _densityStore;
    private readonly DisplayedLimitPreferenceStore _displayedLimitPreferenceStore;
    private readonly StartupRegistrationService _startupRegistration;
    private readonly TrayIconService _trayIcon;
    private readonly TaskbarLabelWindow _taskbarLabel = new();
    private readonly WidgetVisibilityController _widgetVisibility;
    private readonly MainWindowCloseState _closeState = new();
    private WidgetDisplayMode _displayMode;
    private WidgetDensity _density;
    private DisplayedLimitPreference _displayedLimitPreference;
    private UsageSnapshot? _latestSnapshot;
    private UsageWidgetViewModel _viewModel = UsageWidgetViewModel.Loading();
    private bool _isRealActivityActive;
    private bool _isActivityPreviewEnabled;
    private bool _shutdownStarted;

    public MainWindow(
        UsageMonitor usageMonitor,
        CodexActivityMonitor activityMonitor,
        IActivityHookSetupService activityHookSetupService,
        ICodexLauncher codexLauncher,
        DisplayModeStore displayModeStore,
        WidgetDensityStore densityStore,
        DisplayedLimitPreferenceStore displayedLimitPreferenceStore,
        StartupRegistrationService startupRegistration,
        TrayIconService trayIcon)
    {
        _usageMonitor = usageMonitor;
        _activityMonitor = activityMonitor;
        _activityHookSetupWindows = new ActivityHookSetupWindowController(
            this,
            activityHookSetupService,
            codexLauncher);
        _displayModeStore = displayModeStore;
        _densityStore = densityStore;
        _displayedLimitPreferenceStore = displayedLimitPreferenceStore;
        _startupRegistration = startupRegistration;
        _trayIcon = trayIcon;
        _displayMode = displayModeStore.Load();
        _density = densityStore.Load();
        _displayedLimitPreference = displayedLimitPreferenceStore.Load();
        _widgetVisibility = new WidgetVisibilityController(() => IsVisible, ShowWidget, Hide);

        InitializeComponent();
        DataContext = _viewModel;
        ApplyDensity(repositionBottomEdge: false);
        WireEvents();
    }

    public bool StartsInTaskbarIndicatorMode => _displayMode == WidgetDisplayMode.TaskbarIndicator;

    private void WireEvents()
    {
        Loaded += MainWindowOnLoaded;
        Deactivated += MainWindowOnDeactivated;
        Closing += MainWindowOnClosing;

        _usageMonitor.RefreshStarted += UsageMonitorOnRefreshStarted;
        _usageMonitor.SnapshotUpdated += UsageMonitorOnSnapshotUpdated;
        _usageMonitor.RefreshFailed += UsageMonitorOnRefreshFailed;
        _activityMonitor.ActivityChanged += ActivityMonitorOnActivityChanged;
        _activityHookSetupWindows.Closed += (_, _) =>
        {
            if (_displayMode == WidgetDisplayMode.TaskbarIndicator)
            {
                Hide();
            }
        };

        _taskbarLabel.OpenRequested += (_, _) =>
            Dispatcher.BeginInvoke(_widgetVisibility.Show, DispatcherPriority.ApplicationIdle);
        _taskbarLabel.ToggleRequested += (_, _) => _widgetVisibility.Toggle();
        _taskbarLabel.RefreshRequested += (_, _) =>
            Dispatcher.BeginInvoke(() => _ = _usageMonitor.RefreshAsync());
        _taskbarLabel.ActivityDotsSetupRequested += (_, _) =>
            Dispatcher.BeginInvoke(ShowActivityHookSetup);
        _taskbarLabel.ActivityPreviewChanged += (_, _) =>
            Dispatcher.BeginInvoke(() =>
            {
                _isActivityPreviewEnabled = _taskbarLabel.IsActivityPreviewEnabled;
                ApplyActivityIndicatorState();
            });
        _taskbarLabel.DisplayedLimitPreferenceChanged += preference =>
            Dispatcher.BeginInvoke(() => SetDisplayedLimitPreference(preference));
        _taskbarLabel.DesktopModeRequested += (_, _) =>
            Dispatcher.BeginInvoke(() => SetDisplayMode(WidgetDisplayMode.DesktopWidget));
        _taskbarLabel.StartupToggleRequested += (_, _) =>
            Dispatcher.BeginInvoke(ToggleStartupRegistration);
        _taskbarLabel.UpdateCheckRequested += (_, _) =>
            Dispatcher.BeginInvoke(CheckForUpdates);
        _taskbarLabel.ExitRequested += (_, _) => Dispatcher.BeginInvoke(ExitApplication);

        _trayIcon.OpenRequested += (_, _) => Dispatcher.BeginInvoke(_widgetVisibility.Show);
        _trayIcon.RefreshRequested += (_, _) =>
            Dispatcher.BeginInvoke(() => _ = _usageMonitor.RefreshAsync());
        _trayIcon.ActivityDotsSetupRequested += (_, _) =>
            Dispatcher.BeginInvoke(ShowActivityHookSetup);
        _trayIcon.DesktopModeRequested += (_, _) =>
            Dispatcher.BeginInvoke(() => SetDisplayMode(WidgetDisplayMode.DesktopWidget));
        _trayIcon.TaskbarModeRequested += (_, _) =>
            Dispatcher.BeginInvoke(() => SetDisplayMode(WidgetDisplayMode.TaskbarIndicator));
        _trayIcon.DisplayedLimitPreferenceChanged += preference =>
            Dispatcher.BeginInvoke(() => SetDisplayedLimitPreference(preference));
        _trayIcon.StartupToggleRequested += (_, _) =>
            Dispatcher.BeginInvoke(ToggleStartupRegistration);
        _trayIcon.UpdateCheckRequested += (_, _) =>
            Dispatcher.BeginInvoke(CheckForUpdates);
        _trayIcon.ExitRequested += (_, _) => Dispatcher.BeginInvoke(ExitApplication);
    }

    private async void MainWindowOnLoaded(object sender, RoutedEventArgs e)
    {
        PositionNearWorkAreaEdge();
        _trayIcon.SetDisplayMode(_displayMode);
        SetDisplayedLimitPreferenceState(_displayedLimitPreference);
        SetStartupRegistrationState(_startupRegistration.IsEnabled);
        if (_displayMode == WidgetDisplayMode.TaskbarIndicator)
        {
            _taskbarLabel.ShowLabel();
        }

        await _usageMonitor.StartAsync();
    }

    private void UsageMonitorOnRefreshStarted() =>
        Dispatcher.BeginInvoke(() => SetViewModel(_viewModel.Syncing()));

    private void UsageMonitorOnSnapshotUpdated(UsageSnapshot snapshot) =>
        Dispatcher.BeginInvoke(() => RenderSnapshot(snapshot));

    private void UsageMonitorOnRefreshFailed(string message) =>
        Dispatcher.BeginInvoke(() => RenderError(message));

    private void ActivityMonitorOnActivityChanged(bool isActive) =>
        Dispatcher.BeginInvoke(() =>
        {
            _isRealActivityActive = isActive;
            ApplyActivityIndicatorState();
        });

    private void ApplyActivityIndicatorState()
    {
        var isActive = _isRealActivityActive || _isActivityPreviewEnabled;
        WidgetActivityDots.IsActive = isActive;
        _taskbarLabel.SetActivityState(isActive);
    }

    private void RenderSnapshot(UsageSnapshot snapshot)
    {
        _latestSnapshot = snapshot;
        var displayedWindow = ResolveDisplayedWindow(snapshot);
        var nextViewModel = UsageWidgetViewModel.FromSnapshot(snapshot, displayedWindow);
        SetViewModel(nextViewModel);
        if (_density == WidgetDensity.Detailed)
        {
            DetailedView.ScrollToTop();
        }
        else
        {
            ApplyDensity(repositionBottomEdge: true);
        }

        _trayIcon.UpdateUsage(displayedWindow?.RemainingPercent);
        _taskbarLabel.UpdateUsage(
            displayedWindow?.Label,
            displayedWindow?.RemainingPercent,
            displayedWindow?.ResetsAt);
    }

    private void RenderError(string message)
    {
        _latestSnapshot = null;
        SetViewModel(UsageWidgetViewModel.Error(message));
        _trayIcon.UpdateUsage(null);
        _taskbarLabel.UpdateUsage(null, null, null);
    }

    private void SetDisplayedLimitPreference(DisplayedLimitPreference preference)
    {
        _displayedLimitPreference = preference;
        _displayedLimitPreferenceStore.Save(preference);
        SetDisplayedLimitPreferenceState(preference);

        if (_latestSnapshot is { } snapshot)
        {
            RenderSnapshot(snapshot);
        }
    }

    private void SetDisplayedLimitPreferenceState(DisplayedLimitPreference preference)
    {
        _taskbarLabel.SetDisplayedLimitPreference(preference);
        _trayIcon.SetDisplayedLimitPreference(preference);
    }

    private UsageWindow? ResolveDisplayedWindow(UsageSnapshot snapshot)
    {
        var fiveHourAvailable = DisplayedUsageSelector.IsAvailable(
            snapshot,
            DisplayedLimitPreference.FiveHour);
        _taskbarLabel.SetFiveHourLimitAvailability(fiveHourAvailable);
        _trayIcon.SetFiveHourLimitAvailability(fiveHourAvailable);

        var resolvedPreference = DisplayedUsageSelector.ResolvePreference(
            snapshot,
            _displayedLimitPreference);
        if (resolvedPreference != _displayedLimitPreference)
        {
            _displayedLimitPreference = resolvedPreference;
            _displayedLimitPreferenceStore.Save(resolvedPreference);
            SetDisplayedLimitPreferenceState(resolvedPreference);
        }

        return DisplayedUsageSelector.Select(snapshot, _displayedLimitPreference);
    }

    private void SetViewModel(UsageWidgetViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void ApplyDensity(bool repositionBottomEdge)
    {
        var previousBottom = IsLoaded ? Top + ActualHeight : 0d;
        var workArea = SystemParameters.WorkArea;
        var desiredHeight = _density == WidgetDensity.Detailed
            ? Math.Min(DetailedHeight, Math.Max(CompactMaximumHeight, workArea.Height - 40d))
            : CalculateCompactHeight();

        MinHeight = desiredHeight;
        Height = desiredHeight;
        CompactView.Visibility = _density == WidgetDensity.Compact
            ? Visibility.Visible
            : Visibility.Collapsed;
        DetailedView.Visibility = _density == WidgetDensity.Detailed
            ? Visibility.Visible
            : Visibility.Collapsed;
        DensityGlyphRotation.Angle = _density == WidgetDensity.Detailed ? 180d : 0d;
        DensityButton.ToolTip = _density == WidgetDensity.Detailed
            ? "Show compact view"
            : "Show details";

        if (_density == WidgetDensity.Detailed)
        {
            DetailedView.ScrollToTop();
        }

        if (repositionBottomEdge && IsLoaded)
        {
            Top = Math.Clamp(previousBottom - desiredHeight, workArea.Top, workArea.Bottom - desiredHeight);
        }
    }

    private double CalculateCompactHeight()
    {
        var additionalRows = Math.Max(0, _viewModel.GeneralLimits.Count - 1);
        return Math.Min(
            CompactMaximumHeight,
            CompactBaseHeight + additionalRows * CompactLimitRowHeight);
    }

    private void ToggleDensity()
    {
        _density = _density == WidgetDensity.Compact
            ? WidgetDensity.Detailed
            : WidgetDensity.Compact;
        _densityStore.Save(_density);
        ApplyDensity(repositionBottomEdge: true);
    }

    private void PositionNearWorkAreaEdge()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20 + WidgetSurface.Margin.Right;
        Top = workArea.Bottom - Height - 20 + WidgetSurface.Margin.Bottom;
    }

    private void ShowWidget()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        if (_density == WidgetDensity.Detailed)
        {
            DetailedView.ScrollToTop();
        }
    }

    private void ShowActivityHookSetup()
    {
        if (!IsVisible)
        {
            ShowWidget();
        }

        _activityHookSetupWindows.Show();
    }

    private void SetDisplayMode(WidgetDisplayMode mode)
    {
        _displayMode = mode;
        _displayModeStore.Save(mode);
        _trayIcon.SetDisplayMode(mode);

        if (mode == WidgetDisplayMode.DesktopWidget)
        {
            _isActivityPreviewEnabled = false;
            _taskbarLabel.ResetActivityPreview();
            ApplyActivityIndicatorState();
            _taskbarLabel.HideLabel();
            ShowWidget();
            return;
        }

        _taskbarLabel.ShowLabel();
        Hide();
    }

    private void ToggleStartupRegistration()
    {
        var enabled = !_startupRegistration.IsEnabled;
        if (_startupRegistration.TrySetEnabled(enabled))
        {
            SetStartupRegistrationState(enabled);
            return;
        }

        SetStartupRegistrationState(_startupRegistration.IsEnabled);
        System.Windows.MessageBox.Show(
            "The Windows startup preference could not be updated.",
            "Codex Usage Widget",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void SetStartupRegistrationState(bool enabled)
    {
        _taskbarLabel.SetStartupEnabled(enabled);
        _trayIcon.SetStartupEnabled(enabled);
    }

    private static void CheckForUpdates()
    {
        if (GitHubReleaseLauncher.TryOpenLatestRelease())
        {
            return;
        }

        System.Windows.MessageBox.Show(
            "Windows could not open the widget release page.",
            "Codex Usage Widget",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void Widget_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed ||
            e.OriginalSource is not DependencyObject source ||
            FindAncestor<System.Windows.Controls.Button>(source) is not null)
        {
            return;
        }

        DragMove();
    }

    private static T? FindAncestor<T>(DependencyObject source) where T : DependencyObject
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is T match)
            {
                return match;
            }
        }

        return null;
    }

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e) =>
        await _usageMonitor.RefreshAsync();

    private void DensityButton_OnClick(object sender, RoutedEventArgs e) => ToggleDensity();

    private void SettingsButton_OnClick(object sender, RoutedEventArgs e) =>
        _taskbarLabel.OpenMenu(SettingsButton);

    private void ActivityDotsButton_OnClick(object sender, RoutedEventArgs e) =>
        ShowActivityHookSetup();

    private void HideButton_OnClick(object sender, RoutedEventArgs e) =>
        SetDisplayMode(WidgetDisplayMode.TaskbarIndicator);

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => ExitApplication();

    private void MainWindowOnDeactivated(object? sender, EventArgs e)
    {
        if (_displayMode == WidgetDisplayMode.TaskbarIndicator &&
            !_activityHookSetupWindows.IsOpen)
        {
            _widgetVisibility.HideOnDeactivated(_taskbarLabel.IsPointerOver);
        }
    }

    private void ExitApplication()
    {
        _closeState.RequestExplicitExit();
        Close();
    }

    internal void NotifySessionEnding() => _closeState.NotifySessionEnding();

    private void MainWindowOnClosing(object? sender, CancelEventArgs e)
    {
        var closeAction = _closeState.GetCloseAction();
        if (closeAction == MainWindowCloseAction.MinimizeToTaskbar)
        {
            e.Cancel = true;
            SetDisplayMode(WidgetDisplayMode.TaskbarIndicator);
            return;
        }

        if (_shutdownStarted)
        {
            return;
        }

        _shutdownStarted = true;
        _taskbarLabel.HideLabel();
        _taskbarLabel.CloseLabel();
        _trayIcon.Dispose();
        _activityMonitor.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _usageMonitor.DisposeAsync().AsTask().GetAwaiter().GetResult();
        if (closeAction == MainWindowCloseAction.CloseAndShutdownApplication)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
