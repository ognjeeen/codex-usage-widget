using CodexUsageWidget.Infrastructure.Windows;

namespace CodexUsageWidget.Infrastructure.Settings;

public sealed class ThemePreferenceMonitor : IDisposable
{
    private readonly ThemePreferenceStore _store;
    private readonly WindowsThemeMonitor _windowsThemeMonitor;
    private bool _disposed;

    public ThemePreferenceMonitor(
        ThemePreferenceStore store,
        WindowsThemeMonitor windowsThemeMonitor)
    {
        _store = store;
        _windowsThemeMonitor = windowsThemeMonitor;
        Preference = store.Load();
        EffectiveTheme = ResolveEffectiveTheme();
        _windowsThemeMonitor.ThemeChanged += WindowsThemeMonitorOnThemeChanged;
    }

    public event Action<EffectiveTheme>? EffectiveThemeChanged;

    public event Action<EffectiveTheme>? SystemThemeChanged;

    public ThemePreference Preference { get; private set; }

    public EffectiveTheme EffectiveTheme { get; private set; }

    public EffectiveTheme SystemTheme => _windowsThemeMonitor.UsesLightSystemTheme
        ? EffectiveTheme.Light
        : EffectiveTheme.Dark;

    public void SetPreference(ThemePreference preference)
    {
        Preference = preference;
        _store.Save(preference);
        SetEffectiveTheme(ResolveEffectiveTheme());
    }

    private void WindowsThemeMonitorOnThemeChanged(object? sender, EventArgs e)
    {
        SystemThemeChanged?.Invoke(SystemTheme);
        if (Preference == ThemePreference.System)
        {
            SetEffectiveTheme(ResolveEffectiveTheme());
        }
    }

    private EffectiveTheme ResolveEffectiveTheme() => ThemePreferenceResolver.Resolve(
        Preference,
        _windowsThemeMonitor.UsesLightAppTheme);

    private void SetEffectiveTheme(EffectiveTheme theme)
    {
        EffectiveTheme = theme;
        EffectiveThemeChanged?.Invoke(theme);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _windowsThemeMonitor.ThemeChanged -= WindowsThemeMonitorOnThemeChanged;
        _windowsThemeMonitor.Dispose();
    }
}
