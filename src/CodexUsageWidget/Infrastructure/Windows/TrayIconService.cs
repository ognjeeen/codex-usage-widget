using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure.Settings;
using Forms = System.Windows.Forms;

namespace CodexUsageWidget.Infrastructure.Windows;

public sealed class TrayIconService : IDisposable
{
    private const string FiveHourUnavailableText =
        "Codex did not return a global 5h limit for this account.";

    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _desktopWidgetModeItem;
    private readonly Forms.ToolStripMenuItem _taskbarIndicatorModeItem;
    private readonly Forms.ToolStripMenuItem _fiveHourLimitItem;
    private readonly Forms.ToolStripMenuItem _weeklyLimitItem;
    private readonly Forms.ToolStripMenuItem _mostConstrainedLimitItem;
    private readonly Forms.ToolStripMenuItem _startWithWindowsItem;
    private System.Drawing.Icon _currentIcon;
    private bool _disposed;

    public TrayIconService()
    {
        var menu = new Forms.ContextMenuStrip
        {
            ShowItemToolTips = true
        };
        menu.Items.Add("Open", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Refresh", null, (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(
            "Activity dots...",
            null,
            (_, _) => ActivityDotsSetupRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new Forms.ToolStripSeparator());

        var displayModeMenu = new Forms.ToolStripMenuItem("Display mode");
        _desktopWidgetModeItem = new Forms.ToolStripMenuItem(
            "Desktop widget",
            null,
            (_, _) => DesktopModeRequested?.Invoke(this, EventArgs.Empty));
        _taskbarIndicatorModeItem = new Forms.ToolStripMenuItem(
            "Taskbar label",
            null,
            (_, _) => TaskbarModeRequested?.Invoke(this, EventArgs.Empty));
        displayModeMenu.DropDownItems.Add(_desktopWidgetModeItem);
        displayModeMenu.DropDownItems.Add(_taskbarIndicatorModeItem);
        menu.Items.Add(displayModeMenu);

        var displayedLimitMenu = new Forms.ToolStripMenuItem("Displayed limit");
        _fiveHourLimitItem = new Forms.ToolStripMenuItem("5h limit")
        {
            Enabled = false,
            ToolTipText = FiveHourUnavailableText
        };
        _fiveHourLimitItem.Click += (_, _) =>
            DisplayedLimitPreferenceChanged?.Invoke(DisplayedLimitPreference.FiveHour);
        _weeklyLimitItem = new Forms.ToolStripMenuItem("Weekly limit");
        _weeklyLimitItem.Click += (_, _) =>
            DisplayedLimitPreferenceChanged?.Invoke(DisplayedLimitPreference.Weekly);
        _mostConstrainedLimitItem = new Forms.ToolStripMenuItem("Most constrained");
        _mostConstrainedLimitItem.Click += (_, _) =>
            DisplayedLimitPreferenceChanged?.Invoke(DisplayedLimitPreference.MostConstrained);
        displayedLimitMenu.DropDownItems.Add(_fiveHourLimitItem);
        displayedLimitMenu.DropDownItems.Add(_weeklyLimitItem);
        displayedLimitMenu.DropDownItems.Add(_mostConstrainedLimitItem);
        menu.Items.Add(displayedLimitMenu);
        _startWithWindowsItem = new Forms.ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true
        };
        _startWithWindowsItem.Click += (_, _) =>
            StartupToggleRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(_startWithWindowsItem);
        menu.Items.Add(
            "Check for updates...",
            null,
            (_, _) => UpdateCheckRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _currentIcon = UsageIconFactory.Create(null);
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _currentIcon,
            Text = "Codex Usage Widget",
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.MouseClick += NotifyIconOnMouseClick;
    }

    public event EventHandler? OpenRequested;

    public event EventHandler? RefreshRequested;

    public event EventHandler? ActivityDotsSetupRequested;

    public event EventHandler? DesktopModeRequested;

    public event EventHandler? TaskbarModeRequested;

    public event Action<DisplayedLimitPreference>? DisplayedLimitPreferenceChanged;

    public event EventHandler? StartupToggleRequested;

    public event EventHandler? UpdateCheckRequested;

    public event EventHandler? ExitRequested;

    public void SetDisplayMode(WidgetDisplayMode mode)
    {
        _desktopWidgetModeItem.Checked = mode == WidgetDisplayMode.DesktopWidget;
        _taskbarIndicatorModeItem.Checked = mode == WidgetDisplayMode.TaskbarIndicator;
    }

    public void SetStartupEnabled(bool enabled) => _startWithWindowsItem.Checked = enabled;

    public void SetDisplayedLimitPreference(DisplayedLimitPreference preference)
    {
        _fiveHourLimitItem.Checked = preference == DisplayedLimitPreference.FiveHour;
        _weeklyLimitItem.Checked = preference == DisplayedLimitPreference.Weekly;
        _mostConstrainedLimitItem.Checked = preference == DisplayedLimitPreference.MostConstrained;
    }

    public void SetFiveHourLimitAvailability(bool available)
    {
        _fiveHourLimitItem.Enabled = available;
        _fiveHourLimitItem.ToolTipText = available ? string.Empty : FiveHourUnavailableText;
    }

    public void UpdateUsage(double? remainingPercent)
    {
        _notifyIcon.Text = remainingPercent is null
            ? "Codex Usage · unavailable"
            : $"Codex · {Math.Round(remainingPercent.Value):0}% remaining";

        var nextIcon = UsageIconFactory.Create(remainingPercent);
        var previousIcon = _currentIcon;
        _currentIcon = nextIcon;
        _notifyIcon.Icon = nextIcon;
        previousIcon.Dispose();
    }

    private void NotifyIconOnMouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            OpenRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.MouseClick -= NotifyIconOnMouseClick;
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _currentIcon.Dispose();
    }
}
