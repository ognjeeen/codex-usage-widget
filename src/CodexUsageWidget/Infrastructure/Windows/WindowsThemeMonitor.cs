using Microsoft.Win32;

namespace CodexUsageWidget.Infrastructure.Windows;

public sealed class WindowsThemeMonitor : IDisposable
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";
    private readonly string _personalizeKey = PersonalizeKey;
    private bool _disposed;

    public WindowsThemeMonitor()
    {
        SystemEvents.UserPreferenceChanged += SystemEventsOnUserPreferenceChanged;
    }

    public event EventHandler? ThemeChanged;

    public bool UsesLightTheme
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(_personalizeKey);
            return key?.GetValue(AppsUseLightThemeValue) is not int value || value != 0;
        }
    }

    private void SystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) =>
        ThemeChanged?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SystemEvents.UserPreferenceChanged -= SystemEventsOnUserPreferenceChanged;
    }
}
