using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Localization;
using Forms = System.Windows.Forms;

namespace CodexUsageWidget.Infrastructure.Windows;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly Forms.ToolStripMenuItem _openItem;
    private readonly Forms.ToolStripMenuItem _refreshItem;
    private readonly Forms.ToolStripMenuItem _settingsItem;
    private readonly Forms.ToolStripMenuItem _displayModeItem;
    private readonly Forms.ToolStripMenuItem _desktopWidgetModeItem;
    private readonly Forms.ToolStripMenuItem _taskbarIndicatorModeItem;
    private readonly Forms.ToolStripMenuItem _updateItem;
    private readonly Forms.ToolStripMenuItem _exitItem;
    private System.Drawing.Icon _currentIcon;
    private double? _remainingPercent;
    private bool _disposed;

    public TrayIconService()
    {
        var menu = new Forms.ContextMenuStrip
        {
            ShowItemToolTips = true
        };
        _openItem = new Forms.ToolStripMenuItem(
            null,
            null,
            (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty));
        _refreshItem = new Forms.ToolStripMenuItem(
            null,
            null,
            (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty));
        _settingsItem = new Forms.ToolStripMenuItem(
            null,
            null,
            (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(_openItem);
        menu.Items.Add(_refreshItem);
        menu.Items.Add(_settingsItem);
        menu.Items.Add(new Forms.ToolStripSeparator());

        _displayModeItem = new Forms.ToolStripMenuItem();
        _desktopWidgetModeItem = new Forms.ToolStripMenuItem(
            null,
            null,
            (_, _) => DesktopModeRequested?.Invoke(this, EventArgs.Empty));
        _taskbarIndicatorModeItem = new Forms.ToolStripMenuItem(
            null,
            null,
            (_, _) => TaskbarModeRequested?.Invoke(this, EventArgs.Empty));
        _displayModeItem.DropDownItems.Add(_desktopWidgetModeItem);
        _displayModeItem.DropDownItems.Add(_taskbarIndicatorModeItem);
        menu.Items.Add(_displayModeItem);

        _updateItem = new Forms.ToolStripMenuItem(
            null,
            null,
            (_, _) => UpdateCheckRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(_updateItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        _exitItem = new Forms.ToolStripMenuItem(
            null,
            null,
            (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(_exitItem);

        _currentIcon = UsageIconFactory.Create(null);
        _menu = menu;
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _currentIcon,
            Text = "Codex Usage Widget",
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.MouseClick += NotifyIconOnMouseClick;
        Strings.Current.PropertyChanged += StringsOnPropertyChanged;
        RefreshLocalizedText();
        SetTheme(EffectiveTheme.Dark);
    }

    public event EventHandler? OpenRequested;

    public event EventHandler? RefreshRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler? DesktopModeRequested;

    public event EventHandler? TaskbarModeRequested;

    public event EventHandler? UpdateCheckRequested;

    public event EventHandler? ExitRequested;

    public void SetDisplayMode(WidgetDisplayMode mode)
    {
        _desktopWidgetModeItem.Checked = mode == WidgetDisplayMode.DesktopWidget;
        _taskbarIndicatorModeItem.Checked = mode == WidgetDisplayMode.TaskbarIndicator;
    }

    public void SetTheme(EffectiveTheme theme)
    {
        var light = theme == EffectiveTheme.Light;
        var background = light
            ? System.Drawing.Color.FromArgb(250, 250, 251)
            : System.Drawing.Color.FromArgb(36, 36, 36);
        var foreground = light
            ? System.Drawing.Color.FromArgb(35, 35, 37)
            : System.Drawing.Color.FromArgb(232, 232, 232);

        _menu.BackColor = background;
        _menu.ForeColor = foreground;
        ApplyMenuColors(_menu.Items, background, foreground);
        _menu.Renderer = new Forms.ToolStripProfessionalRenderer(
            new TrayMenuColorTable(light));
    }

    private static void ApplyMenuColors(
        Forms.ToolStripItemCollection items,
        System.Drawing.Color background,
        System.Drawing.Color foreground)
    {
        foreach (Forms.ToolStripItem item in items)
        {
            item.BackColor = background;
            item.ForeColor = foreground;
            if (item is Forms.ToolStripMenuItem menuItem)
            {
                ApplyMenuColors(menuItem.DropDownItems, background, foreground);
            }
        }
    }

    public void UpdateUsage(double? remainingPercent)
    {
        _remainingPercent = remainingPercent;
        _notifyIcon.Text = remainingPercent is null
            ? Strings.Get("Tray_Unavailable")
            : Strings.Format("Tray_Remaining", Math.Round(remainingPercent.Value));

        var nextIcon = UsageIconFactory.Create(remainingPercent);
        var previousIcon = _currentIcon;
        _currentIcon = nextIcon;
        _notifyIcon.Icon = nextIcon;
        previousIcon.Dispose();
    }

    private void StringsOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item[]")
        {
            RefreshLocalizedText();
        }
    }

    private void RefreshLocalizedText()
    {
        _openItem.Text = Strings.Get("Tray_Open");
        _refreshItem.Text = Strings.Get("Common_Refresh");
        _settingsItem.Text = Strings.Get("Tray_Settings");
        _displayModeItem.Text = Strings.Get("Tray_DisplayMode");
        _desktopWidgetModeItem.Text = Strings.Get("Tray_DesktopWidget");
        _taskbarIndicatorModeItem.Text = Strings.Get("Tray_TaskbarLabel");
        _updateItem.Text = Strings.Get("Tray_CheckUpdates");
        _exitItem.Text = Strings.Get("Common_Exit");
        UpdateUsage(_remainingPercent);
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
        Strings.Current.PropertyChanged -= StringsOnPropertyChanged;
        _notifyIcon.MouseClick -= NotifyIconOnMouseClick;
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _currentIcon.Dispose();
    }

    private sealed class TrayMenuColorTable(bool light) : Forms.ProfessionalColorTable
    {
        private readonly System.Drawing.Color _background = light
            ? System.Drawing.Color.FromArgb(250, 250, 251)
            : System.Drawing.Color.FromArgb(36, 36, 36);
        private readonly System.Drawing.Color _border = light
            ? System.Drawing.Color.FromArgb(215, 217, 222)
            : System.Drawing.Color.FromArgb(69, 69, 69);
        private readonly System.Drawing.Color _hover = light
            ? System.Drawing.Color.FromArgb(236, 238, 241)
            : System.Drawing.Color.FromArgb(54, 54, 54);

        public override System.Drawing.Color ToolStripDropDownBackground => _background;

        public override System.Drawing.Color ImageMarginGradientBegin => _background;

        public override System.Drawing.Color ImageMarginGradientMiddle => _background;

        public override System.Drawing.Color ImageMarginGradientEnd => _background;

        public override System.Drawing.Color MenuBorder => _border;

        public override System.Drawing.Color MenuItemBorder => _hover;

        public override System.Drawing.Color MenuItemSelected => _hover;

        public override System.Drawing.Color MenuItemSelectedGradientBegin => _hover;

        public override System.Drawing.Color MenuItemSelectedGradientEnd => _hover;

        public override System.Drawing.Color MenuItemPressedGradientBegin => _hover;

        public override System.Drawing.Color MenuItemPressedGradientMiddle => _hover;

        public override System.Drawing.Color MenuItemPressedGradientEnd => _hover;

        public override System.Drawing.Color SeparatorDark => _border;

        public override System.Drawing.Color SeparatorLight => _background;
    }
}
