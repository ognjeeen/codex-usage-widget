using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CodexUsageWidget.Infrastructure.Windows;

namespace CodexUsageWidget.Views;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "WPF owns the window lifecycle; the Closed handler releases the native hooks.")]
public partial class TaskbarLabelWindow : Window
{
    private readonly System.Windows.Threading.DispatcherTimer _positionTimer;
    private readonly WindowChangeWatcher _windowChangeWatcher;
    private IntPtr _windowHandle;
    private bool _labelRequested;
    private bool _isTaskActive;
    private bool _isClosed;
    private int _visibilityUpdateQueued;

    public TaskbarLabelWindow()
    {
        InitializeComponent();

#if DEBUG || ACTIVITY_PREVIEW
        ActivityPreviewMenuItem.Visibility = Visibility.Visible;
#endif

        SourceInitialized += (_, _) =>
        {
            _windowHandle = new WindowInteropHelper(this).Handle;
            TaskbarWindowInterop.ConfigureAsTaskbarOverlay(_windowHandle);
            Reposition();
        };

        _positionTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _positionTimer.Tick += (_, _) => UpdateVisibilityAndPosition();

        _windowChangeWatcher = new WindowChangeWatcher(QueueVisibilityUpdate);
        Closed += (_, _) =>
        {
            _isClosed = true;
            _labelRequested = false;
            _positionTimer.Stop();
            _windowChangeWatcher.Dispose();
        };
    }

    public event EventHandler? OpenRequested;

    public event EventHandler? ToggleRequested;

    public event EventHandler? RefreshRequested;

    public event EventHandler? ActivityDotsSetupRequested;

    public event EventHandler? ActivityPreviewChanged;

    public event EventHandler? DesktopModeRequested;

    public event EventHandler? StartupToggleRequested;

    public event EventHandler? ExitRequested;

    public bool IsPointerOver => IsMouseOver;

    public bool IsActivityPreviewEnabled => ActivityPreviewMenuItem.IsChecked;

    public void ResetActivityPreview() => ActivityPreviewMenuItem.IsChecked = false;

    public void ShowLabel()
    {
        _labelRequested = true;
        new WindowInteropHelper(this).EnsureHandle();
        _positionTimer.Start();
        UpdateVisibilityAndPosition();
    }

    public void HideLabel()
    {
        _labelRequested = false;
        _positionTimer.Stop();
        if (!_isClosed)
        {
            Hide();
        }
    }

    public void CloseLabel()
    {
        if (!_isClosed)
        {
            Close();
        }
    }

    public void SetActivityState(bool isActive)
    {
        if (_isTaskActive == isActive)
        {
            return;
        }

        _isTaskActive = isActive;
        ActivityDots.IsActive = isActive;
    }

    public void SetStartupEnabled(bool enabled) => StartWithWindowsMenuItem.IsChecked = enabled;

    public void UpdateUsage(double? remainingPercent, DateTimeOffset? resetsAt)
    {
        if (remainingPercent is null)
        {
            UsageText.Text = "--%";
            LabelSurface.ToolTip = "Codex usage is currently unavailable.";
            return;
        }

        var value = Math.Round(Math.Clamp(remainingPercent.Value, 0d, 100d));
        UsageText.Text = $"{value:0}%";
        LabelSurface.ToolTip = resetsAt is null
            ? $"Codex: {value:0}% remaining"
            : $"Codex: {value:0}% remaining · resets {resetsAt.Value:ddd HH:mm}";
    }

    private void Reposition()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            TaskbarWindowInterop.PositionNextToNotificationArea(_windowHandle, Width, Height);
        }
    }

    private void UpdateVisibilityAndPosition()
    {
        if (_isClosed || !_labelRequested || _windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (FullscreenWindowDetector.IsForegroundWindowFullscreenOnMonitor(_windowHandle))
        {
            if (IsVisible)
            {
                Hide();
            }

            return;
        }

        if (!IsVisible)
        {
            Reposition();
            Show();
            return;
        }

        Reposition();
    }

    private void QueueVisibilityUpdate()
    {
        if (Interlocked.Exchange(ref _visibilityUpdateQueued, 1) != 0)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            () =>
            {
                Interlocked.Exchange(ref _visibilityUpdateQueued, 0);
                UpdateVisibilityAndPosition();
            },
            System.Windows.Threading.DispatcherPriority.Send);
    }

    private void LabelSurface_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        ToggleRequested?.Invoke(this, EventArgs.Empty);

    private void OpenMenuItem_OnClick(object sender, RoutedEventArgs e) =>
        OpenRequested?.Invoke(this, EventArgs.Empty);

    private void RefreshMenuItem_OnClick(object sender, RoutedEventArgs e) =>
        RefreshRequested?.Invoke(this, EventArgs.Empty);

    private void ActivityDotsMenuItem_OnClick(object sender, RoutedEventArgs e) =>
        ActivityDotsSetupRequested?.Invoke(this, EventArgs.Empty);

    private void ActivityPreviewMenuItem_OnClick(object sender, RoutedEventArgs e)
        => ActivityPreviewChanged?.Invoke(this, EventArgs.Empty);

    private void DesktopModeMenuItem_OnClick(object sender, RoutedEventArgs e) =>
        DesktopModeRequested?.Invoke(this, EventArgs.Empty);

    private void StartWithWindowsMenuItem_OnClick(object sender, RoutedEventArgs e) =>
        StartupToggleRequested?.Invoke(this, EventArgs.Empty);

    private void ExitMenuItem_OnClick(object sender, RoutedEventArgs e) =>
        ExitRequested?.Invoke(this, EventArgs.Empty);
}
