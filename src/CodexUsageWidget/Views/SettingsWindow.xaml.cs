using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Views;

public partial class SettingsWindow : Window
{
    private bool _suppressChangeEvents;

    public SettingsWindow(
        ThemePreference themePreference,
        WidgetDensity widgetDensity,
        DisplayedLimitPreference displayedLimitPreference,
        bool fiveHourLimitAvailable,
        bool startWithWindowsEnabled)
    {
        InitializeComponent();
        _suppressChangeEvents = true;
        SetSelectedTheme(themePreference);
        SetWidgetDensity(widgetDensity);
        SetDisplayedLimitPreference(displayedLimitPreference);
        SetFiveHourLimitAvailability(fiveHourLimitAvailable);
        SetStartWithWindowsEnabled(startWithWindowsEnabled);
        _suppressChangeEvents = false;
    }

    public event Action<ThemePreference>? ThemePreferenceChanged;

    public event Action<WidgetDensity>? WidgetDensityChanged;

    public event Action<DisplayedLimitPreference>? DisplayedLimitPreferenceChanged;

    public event Action<bool>? StartWithWindowsChanged;

    public ThemePreference SelectedTheme =>
        LightThemeOption.IsChecked == true
            ? ThemePreference.Light
            : DarkThemeOption.IsChecked == true
                ? ThemePreference.Dark
                : ThemePreference.System;

    public DisplayedLimitPreference SelectedDisplayedLimit =>
        WeeklyLimitOption.IsChecked == true
            ? DisplayedLimitPreference.Weekly
            : MostConstrainedLimitOption.IsChecked == true
                ? DisplayedLimitPreference.MostConstrained
                : DisplayedLimitPreference.FiveHour;

    public WidgetDensity SelectedWidgetDensity =>
        DetailedLayoutOption.IsChecked == true
            ? WidgetDensity.Detailed
            : WidgetDensity.Compact;

    public bool StartWithWindowsEnabled => StartWithWindowsOption.IsChecked == true;

    public void SetDisplayedLimitPreference(DisplayedLimitPreference preference)
    {
        var previousSuppression = _suppressChangeEvents;
        _suppressChangeEvents = true;
        FiveHourLimitOption.IsChecked = preference == DisplayedLimitPreference.FiveHour;
        WeeklyLimitOption.IsChecked = preference == DisplayedLimitPreference.Weekly;
        MostConstrainedLimitOption.IsChecked =
            preference == DisplayedLimitPreference.MostConstrained;
        _suppressChangeEvents = previousSuppression;
    }

    public void SetFiveHourLimitAvailability(bool available)
    {
        FiveHourLimitOption.IsEnabled = available;
        ToolTipService.SetIsEnabled(FiveHourLimitOption, !available);
    }

    public void SetWidgetDensity(WidgetDensity density)
    {
        var previousSuppression = _suppressChangeEvents;
        _suppressChangeEvents = true;
        CompactLayoutOption.IsChecked = density == WidgetDensity.Compact;
        DetailedLayoutOption.IsChecked = density == WidgetDensity.Detailed;
        _suppressChangeEvents = previousSuppression;
    }

    public void SetStartWithWindowsEnabled(bool enabled)
    {
        var previousSuppression = _suppressChangeEvents;
        _suppressChangeEvents = true;
        StartWithWindowsOption.IsChecked = enabled;
        _suppressChangeEvents = previousSuppression;
    }

    private void SetSelectedTheme(ThemePreference preference)
    {
        SystemThemeOption.IsChecked = preference == ThemePreference.System;
        LightThemeOption.IsChecked = preference == ThemePreference.Light;
        DarkThemeOption.IsChecked = preference == ThemePreference.Dark;
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void ThemeOption_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_suppressChangeEvents)
        {
            ThemePreferenceChanged?.Invoke(SelectedTheme);
        }
    }

    private void DisplayedLimitOption_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_suppressChangeEvents)
        {
            DisplayedLimitPreferenceChanged?.Invoke(SelectedDisplayedLimit);
        }
    }

    private void WidgetLayoutOption_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_suppressChangeEvents)
        {
            WidgetDensityChanged?.Invoke(SelectedWidgetDensity);
        }
    }

    private void StartWithWindowsOption_OnChanged(object sender, RoutedEventArgs e)
    {
        if (!_suppressChangeEvents)
        {
            StartWithWindowsChanged?.Invoke(StartWithWindowsEnabled);
        }
    }
}
