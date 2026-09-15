using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;
using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Infrastructure.Windows;
using CodexUsageWidget.Localization;
using CodexUsageWidget.Views.ViewModels;

namespace CodexUsageWidget.Views;

public partial class MainWindow : Window
{
    private const double CompactWidth = 420d;
    private const double CompactMinimumWidth = 390d;
    private const double DetailedWidth = 504d;
    private const double DetailedMinimumWidth = 468d;
    private const double CompactBaseHeight = 198d;
    private const double CompactWarningHeight = 36d;
    private const double CompactLimitRowHeight = 0d;
    private const double CompactMaximumHeight = 234d;
    private const double DetailedHeight = 585d;

    private readonly UsageMonitor _usageMonitor;
    private readonly RateLimitResetUseCase _resetUseCase;
    private readonly CodexActivityMonitor _activityMonitor;
    private readonly IActivityHookSetupService _activityHookSetupService;
    private readonly ICodexLauncher _codexLauncher;
    private readonly DisplayModeStore _displayModeStore;
    private readonly WidgetDensityStore _densityStore;
    private readonly DisplayedLimitPreferenceStore _displayedLimitPreferenceStore;
    private readonly TimeFormatPreferenceStore _timeFormatPreferenceStore;
    private readonly StartupRegistrationService _startupRegistration;
    private readonly TrayIconService _trayIcon;
    private readonly AppThemeController _themeController;
    private readonly AppLanguageController _languageController;
    private readonly TaskbarLabelWindow _taskbarLabel = new();
    private readonly WidgetVisibilityController _widgetVisibility;
    private readonly DispatcherTimer _taskbarHoverHideTimer;
    private readonly MainWindowCloseState _closeState = new();
    private SettingsWindow? _settingsWindow;
    private WidgetDisplayMode _displayMode;
    private WidgetDensity _density;
    private DisplayedLimitPreference _displayedLimitPreference;
    private TimeFormatPreference _timeFormatPreference;
    private UsageSnapshot? _latestSnapshot;
    private UsageWidgetViewModel _viewModel = UsageWidgetViewModel.Loading();
    private bool _isRealActivityActive;
    private bool _isActivityPreviewEnabled;
    private bool _isSettingsOpen;
    private bool _isResetDialogOpen;
    private bool _isTaskbarHoverPreview;
    private bool _resetUsePending;
    private bool _shutdownStarted;

    public MainWindow(
        UsageMonitor usageMonitor,
        RateLimitResetUseCase resetUseCase,
        CodexActivityMonitor activityMonitor,
        IActivityHookSetupService activityHookSetupService,
        ICodexLauncher codexLauncher,
        DisplayModeStore displayModeStore,
        WidgetDensityStore densityStore,
        DisplayedLimitPreferenceStore displayedLimitPreferenceStore,
        StartupRegistrationService startupRegistration,
        TrayIconService trayIcon,
        AppThemeController themeController,
        AppLanguageController languageController,
        TimeFormatPreferenceStore timeFormatPreferenceStore)
    {
        _usageMonitor = usageMonitor;
        _resetUseCase = resetUseCase;
        _activityMonitor = activityMonitor;
        _activityHookSetupService = activityHookSetupService;
        _codexLauncher = codexLauncher;
        _displayModeStore = displayModeStore;
        _densityStore = densityStore;
        _displayedLimitPreferenceStore = displayedLimitPreferenceStore;
        _startupRegistration = startupRegistration;
        _trayIcon = trayIcon;
        _themeController = themeController;
        _languageController = languageController;
        _timeFormatPreferenceStore = timeFormatPreferenceStore;
        _displayMode = displayModeStore.Load();
        _density = densityStore.Load();
        _displayedLimitPreference = displayedLimitPreferenceStore.Load();
        _timeFormatPreference = timeFormatPreferenceStore.Load();
        _widgetVisibility = new WidgetVisibilityController(
            () => IsVisible,
            () => ShowWidget(),
            Hide);

        InitializeComponent();
        _taskbarHoverHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(220)
        };
        _taskbarHoverHideTimer.Tick += (_, _) => HideTaskbarHoverPreviewIfPointerLeft();
        _taskbarLabel.SetTimeFormatPreference(_timeFormatPreference);
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
        DetailedView.ResetUseRequested += DetailedViewOnResetUseRequested;
        _activityMonitor.ActivityChanged += ActivityMonitorOnActivityChanged;
        _taskbarLabel.OpenRequested += (_, _) =>
            Dispatcher.BeginInvoke(_widgetVisibility.Show, DispatcherPriority.ApplicationIdle);
        _taskbarLabel.HoverEntered += (_, _) =>
            Dispatcher.BeginInvoke(ShowTaskbarHoverPreview, DispatcherPriority.Input);
        _taskbarLabel.HoverExited += (_, _) =>
            Dispatcher.BeginInvoke(ScheduleHideTaskbarHoverPreview, DispatcherPriority.Input);
        _taskbarLabel.ToggleRequested += (_, _) =>
        {
            CancelTaskbarHoverPreview();
            _widgetVisibility.Toggle();
        };
        _taskbarLabel.RefreshRequested += (_, _) =>
            Dispatcher.BeginInvoke(() => _ = _usageMonitor.RefreshAsync());
        _taskbarLabel.SettingsRequested += (_, _) => Dispatcher.BeginInvoke(ShowSettings);
        _taskbarLabel.ActivityPreviewChanged += (_, _) =>
            Dispatcher.BeginInvoke(() =>
            {
                _isActivityPreviewEnabled = _taskbarLabel.IsActivityPreviewEnabled;
                ApplyActivityIndicatorState();
            });
        _taskbarLabel.DesktopModeRequested += (_, _) =>
            Dispatcher.BeginInvoke(() => SetDisplayMode(WidgetDisplayMode.DesktopWidget));
        _taskbarLabel.UpdateCheckRequested += (_, _) =>
            Dispatcher.BeginInvoke(CheckForUpdates);
        _taskbarLabel.ExitRequested += (_, _) => Dispatcher.BeginInvoke(ExitApplication);

        _trayIcon.OpenRequested += (_, _) => Dispatcher.BeginInvoke(_widgetVisibility.Show);
        _trayIcon.RefreshRequested += (_, _) =>
            Dispatcher.BeginInvoke(() => _ = _usageMonitor.RefreshAsync());
        _trayIcon.SettingsRequested += (_, _) => Dispatcher.BeginInvoke(ShowSettings);
        _trayIcon.DesktopModeRequested += (_, _) =>
            Dispatcher.BeginInvoke(() => SetDisplayMode(WidgetDisplayMode.DesktopWidget));
        _trayIcon.TaskbarModeRequested += (_, _) =>
            Dispatcher.BeginInvoke(() => SetDisplayMode(WidgetDisplayMode.TaskbarIndicator));
        _trayIcon.UpdateCheckRequested += (_, _) =>
            Dispatcher.BeginInvoke(CheckForUpdates);
        _trayIcon.ExitRequested += (_, _) => Dispatcher.BeginInvoke(ExitApplication);
        _themeController.EffectiveThemeChanged += theme =>
            Dispatcher.BeginInvoke(() => _trayIcon.SetTheme(theme));
        _themeController.SystemThemeChanged += theme =>
            Dispatcher.BeginInvoke(() => _taskbarLabel.SetSystemTheme(theme));
    }

    private async void MainWindowOnLoaded(object sender, RoutedEventArgs e)
    {
        PositionNearWorkAreaEdge();
        _trayIcon.SetDisplayMode(_displayMode);
        _trayIcon.SetTheme(_themeController.EffectiveTheme);
        _taskbarLabel.SetSystemTheme(_themeController.SystemTheme);
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
        var nextViewModel = UsageWidgetViewModel.FromSnapshot(
            snapshot,
            displayedWindow,
            _timeFormatPreference);
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
            nextViewModel.HasWeeklyLimit
                ? nextViewModel.WeeklyRemainingPercent
                : null,
            displayedWindow?.ResetsAt);
    }

    private void RenderError(string message)
    {
        _latestSnapshot = null;
        SetViewModel(UsageWidgetViewModel.Error(message));
        if (_density == WidgetDensity.Compact)
        {
            ApplyDensity(repositionBottomEdge: true);
        }
        _trayIcon.UpdateUsage(null);
        _taskbarLabel.UpdateUsage(null, null, null, null);
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
        _settingsWindow?.SetDisplayedLimitPreference(preference);
    }

    private UsageWindow? ResolveDisplayedWindow(UsageSnapshot snapshot)
    {
        var fiveHourAvailable = DisplayedUsageSelector.IsAvailable(
            snapshot,
            DisplayedLimitPreference.FiveHour);
        _settingsWindow?.SetFiveHourLimitAvailability(fiveHourAvailable);

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
        var previousRight = IsLoaded ? Left + ActualWidth : 0d;
        var workArea = SystemParameters.WorkArea;
        var isDetailed = _density == WidgetDensity.Detailed;
        var desiredWidth = isDetailed ? DetailedWidth : CompactWidth;
        var desiredHeight = isDetailed
            ? Math.Min(DetailedHeight, Math.Max(CompactMaximumHeight, workArea.Height - 40d))
            : CalculateCompactHeight();

        MinWidth = isDetailed ? DetailedMinimumWidth : CompactMinimumWidth;
        Width = desiredWidth;
        HeaderRow.Height = new GridLength(isDetailed ? 43d : 36d);
        FooterRow.Height = new GridLength(isDetailed ? 36d : 27d);
        HeaderLayout.LayoutTransform = isDetailed
            ? new ScaleTransform(0.9d, 0.9d)
            : new ScaleTransform(0.75d, 0.75d);
        FooterLayout.LayoutTransform = isDetailed
            ? new ScaleTransform(0.9d, 0.9d)
            : new ScaleTransform(0.75d, 0.75d);
        WidgetSurface.Margin = isDetailed
            ? new Thickness(9d)
            : new Thickness(7.5d);
        WidgetSurface.CornerRadius = new CornerRadius(isDetailed ? 20d : 16.5d);
        WidgetShadow.BlurRadius = isDetailed ? 20d : 16.5d;
        WidgetShadow.ShadowDepth = isDetailed ? 3.5d : 3d;
        MinHeight = desiredHeight;
        Height = desiredHeight;
        ContentRow.Height = isDetailed
            ? new GridLength(1, GridUnitType.Star)
            : GridLength.Auto;
        CompactView.Visibility = !isDetailed
            ? Visibility.Visible
            : Visibility.Collapsed;
        DetailedView.Visibility = isDetailed
            ? Visibility.Visible
            : Visibility.Collapsed;
        DensityGlyphRotation.Angle = isDetailed ? 180d : 0d;
        DensityButton.ToolTip = isDetailed
            ? Strings.Get("Main_ShowCompact")
            : Strings.Get("Main_ShowDetails");

        if (_density == WidgetDensity.Detailed)
        {
            DetailedView.ScrollToTop();
        }

        if (repositionBottomEdge && IsLoaded)
        {
            Left = Math.Clamp(previousRight - desiredWidth, workArea.Left, workArea.Right - desiredWidth);
            Top = Math.Clamp(previousBottom - desiredHeight, workArea.Top, workArea.Bottom - desiredHeight);
        }
    }

    private double CalculateCompactHeight()
    {
        var additionalRows = Math.Max(0, _viewModel.GeneralLimits.Count - 1);
        var warningHeight = _viewModel.HasWarning ? CompactWarningHeight : 0d;
        return Math.Min(
            CompactMaximumHeight,
            CompactBaseHeight + warningHeight + additionalRows * CompactLimitRowHeight);
    }

    private void SetDensity(WidgetDensity density)
    {
        if (_density == density)
        {
            return;
        }

        _density = density;
        _densityStore.Save(_density);
        ApplyDensity(repositionBottomEdge: true);
    }

    private void PositionNearWorkAreaEdge()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20 + WidgetSurface.Margin.Right;
        Top = workArea.Bottom - Height - 20 + WidgetSurface.Margin.Bottom;
    }

    private void ShowWidget(bool activate = true)
    {
        if (!IsVisible)
        {
            ShowActivated = activate;
            Show();
        }

        WindowState = WindowState.Normal;
        if (activate)
        {
            Activate();
        }

        if (_density == WidgetDensity.Detailed)
        {
            DetailedView.ScrollToTop();
        }
    }

    private void ShowTaskbarHoverPreview()
    {
        if (_displayMode != WidgetDisplayMode.TaskbarIndicator ||
            _isSettingsOpen ||
            _isResetDialogOpen ||
            _shutdownStarted)
        {
            return;
        }

        _isTaskbarHoverPreview = true;
        _taskbarHoverHideTimer.Stop();
        ShowWidget(activate: false);
    }

    private void ScheduleHideTaskbarHoverPreview()
    {
        if (!_isTaskbarHoverPreview)
        {
            return;
        }

        _taskbarHoverHideTimer.Stop();
        _taskbarHoverHideTimer.Start();
    }

    private void HideTaskbarHoverPreviewIfPointerLeft()
    {
        _taskbarHoverHideTimer.Stop();
        if (!_isTaskbarHoverPreview ||
            _taskbarLabel.IsPointerOver ||
            IsMouseOver ||
            _isSettingsOpen ||
            _isResetDialogOpen)
        {
            return;
        }

        _isTaskbarHoverPreview = false;
        if (_displayMode == WidgetDisplayMode.TaskbarIndicator && IsVisible)
        {
            Hide();
        }
    }

    private void CancelTaskbarHoverPreview()
    {
        _taskbarHoverHideTimer.Stop();
        _isTaskbarHoverPreview = false;
    }

    private void ShowSettings()
    {
        if (_settingsWindow is { } existingWindow)
        {
            if (existingWindow.IsVisible)
            {
                existingWindow.Activate();
            }

            return;
        }

        var shouldReturnToTaskbar = _displayMode == WidgetDisplayMode.TaskbarIndicator;
        if (!IsVisible)
        {
            ShowWidget();
        }

        _isSettingsOpen = true;
        try
        {
            var fiveHourLimitAvailable = _latestSnapshot is { } snapshot &&
                DisplayedUsageSelector.IsAvailable(
                    snapshot,
                    DisplayedLimitPreference.FiveHour);
            var window = new SettingsWindow(
                _themeController.Preference,
                _density,
                _displayedLimitPreference,
                fiveHourLimitAvailable,
                _startupRegistration.IsEnabled,
                _activityHookSetupService,
                _codexLauncher,
                _languageController.Preference,
                _timeFormatPreference)
            {
                Owner = this
            };
            _settingsWindow = window;
            window.ThemePreferenceChanged += _themeController.SetPreference;
            window.WidgetDensityChanged += SetDensity;
            window.DisplayedLimitPreferenceChanged += SetDisplayedLimitPreference;
            window.StartWithWindowsChanged += SetStartupRegistration;
            window.LanguagePreferenceChanged += SetLanguagePreference;
            window.TimeFormatPreferenceChanged += SetTimeFormatPreference;
            window.ShowDialog();
        }
        finally
        {
            _settingsWindow = null;
            _isSettingsOpen = false;
            if (shouldReturnToTaskbar)
            {
                Hide();
            }
        }
    }

    private void SetDisplayMode(WidgetDisplayMode mode)
    {
        CancelTaskbarHoverPreview();
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

    private void SetStartupRegistration(bool enabled)
    {
        if (_startupRegistration.IsEnabled == enabled)
        {
            return;
        }

        if (_startupRegistration.TrySetEnabled(enabled))
        {
            SetStartupRegistrationState(enabled);
            return;
        }

        SetStartupRegistrationState(_startupRegistration.IsEnabled);
        System.Windows.MessageBox.Show(
            Strings.Get("Main_StartupPreferenceError"),
            Strings.Get("App_Name"),
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void SetStartupRegistrationState(bool enabled)
    {
        _settingsWindow?.SetStartWithWindowsEnabled(enabled);
    }

    private static void CheckForUpdates()
    {
        if (GitHubReleaseLauncher.TryOpenLatestRelease())
        {
            return;
        }

        System.Windows.MessageBox.Show(
            Strings.Get("Main_UpdateOpenError"),
            Strings.Get("App_Name"),
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void SetLanguagePreference(LanguagePreference preference)
    {
        _languageController.SetPreference(preference);
        ApplyDensity(repositionBottomEdge: false);
        if (_latestSnapshot is { } snapshot)
        {
            RenderSnapshot(snapshot);
            return;
        }

        SetViewModel(UsageWidgetViewModel.Loading());
        _ = _usageMonitor.RefreshAsync();
    }

    private void SetTimeFormatPreference(TimeFormatPreference preference)
    {
        _timeFormatPreference = preference;
        _timeFormatPreferenceStore.Save(preference);
        _taskbarLabel.SetTimeFormatPreference(preference);
        if (_latestSnapshot is { } snapshot)
        {
            RenderSnapshot(snapshot);
        }
    }

    private async void DetailedViewOnResetUseRequested(
        object? sender,
        Controls.RateLimitResetRequestedEventArgs e)
    {
        if (_resetUsePending)
        {
            return;
        }

        if (!ShowResetConfirmation(e.Credit))
        {
            return;
        }

        _resetUsePending = true;
        DetailedView.SetResetUsePending(pending: true);
        try
        {
            var result = await _resetUseCase.UseAsync(e.Credit.CreditId);
            if (result.Status is RateLimitResetUseStatus.NothingToReset)
            {
                ShowResetMessage(
                    Strings.Get("Usage_ResetNothingToReset"),
                    MessageBoxImage.Information);
            }
            else if (result.Status is RateLimitResetUseStatus.NoCredit)
            {
                ShowResetMessage(
                    Strings.Get("Usage_ResetNoCredit"),
                    MessageBoxImage.Warning);
            }
            else if (result.Status is RateLimitResetUseStatus.TimedOut)
            {
                ShowResetMessage(
                    Strings.Get("Error_ResponseTimeout"),
                    MessageBoxImage.Warning);
            }
            else if (result.Status is RateLimitResetUseStatus.Failed)
            {
                ShowResetMessage(
                    Strings.Format(
                        "Usage_ResetFailure",
                        result.ErrorMessage ?? string.Empty),
                    MessageBoxImage.Warning);
            }
        }
        finally
        {
            _resetUsePending = false;
            DetailedView.SetResetUsePending(pending: false);
        }
    }

    private bool ShowResetConfirmation(RateLimitResetCreditViewModel credit)
    {
        _isResetDialogOpen = true;
        try
        {
            var confirmation = new RateLimitResetConfirmationWindow(
                credit,
                _timeFormatPreference)
            {
                Owner = this
            };
            return confirmation.ShowDialog() == true;
        }
        finally
        {
            _isResetDialogOpen = false;
        }
    }

    private void ShowResetMessage(string message, MessageBoxImage image)
    {
        _isResetDialogOpen = true;
        try
        {
            System.Windows.MessageBox.Show(
                this,
                message,
                Strings.Get("Usage_RateLimitResets"),
                MessageBoxButton.OK,
                image);
        }
        finally
        {
            _isResetDialogOpen = false;
        }
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

    private void MainWindow_OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e) =>
        _taskbarHoverHideTimer.Stop();

    private void MainWindow_OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e) =>
        ScheduleHideTaskbarHoverPreview();

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

    private void DensityButton_OnClick(object sender, RoutedEventArgs e) =>
        SetDensity(
            _density == WidgetDensity.Compact
                ? WidgetDensity.Detailed
                : WidgetDensity.Compact);

    private void SettingsButton_OnClick(object sender, RoutedEventArgs e) => ShowSettings();

    private void HideButton_OnClick(object sender, RoutedEventArgs e) =>
        SetDisplayMode(WidgetDisplayMode.TaskbarIndicator);

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => ExitApplication();

    private void MainWindowOnDeactivated(object? sender, EventArgs e)
    {
        if (_displayMode == WidgetDisplayMode.TaskbarIndicator)
        {
            _widgetVisibility.HideOnDeactivated(
                _taskbarLabel.IsPointerOver || _isTaskbarHoverPreview || IsMouseOver,
                ownedDialogOpen: _isSettingsOpen || _isResetDialogOpen);
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
        CancelTaskbarHoverPreview();
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
