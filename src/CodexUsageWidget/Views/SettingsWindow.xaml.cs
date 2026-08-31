using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Infrastructure.Windows;
using CodexUsageWidget.Views.Controls;

namespace CodexUsageWidget.Views;

public partial class SettingsWindow : Window
{
    private readonly IWindowWorkAreaProvider _workAreaProvider;
    private bool _suppressChangeEvents;

    public SettingsWindow(
        ThemePreference themePreference,
        WidgetDensity widgetDensity,
        DisplayedLimitPreference displayedLimitPreference,
        bool fiveHourLimitAvailable,
        bool startWithWindowsEnabled,
        IActivityHookSetupService activityHookSetupService,
        ICodexLauncher codexLauncher,
        AccentPalette accentPalette = AccentPalette.Blue,
        LanguagePreference languagePreference = LanguagePreference.System,
        TimeFormatPreference timeFormatPreference = TimeFormatPreference.Automatic,
        IWindowWorkAreaProvider? workAreaProvider = null)
    {
        _workAreaProvider = workAreaProvider ?? new WindowWorkAreaProvider();
        InitializeComponent();
        ActivityDotsHost.Content = new ActivityHookSetupControl(
            activityHookSetupService,
            codexLauncher);
        _suppressChangeEvents = true;
        SetSelectedTheme(themePreference);
        SetSelectedAccentPalette(accentPalette);
        SetWidgetDensity(widgetDensity);
        SetDisplayedLimitPreference(displayedLimitPreference);
        SetFiveHourLimitAvailability(fiveHourLimitAvailable);
        SetStartWithWindowsEnabled(startWithWindowsEnabled);
        SetLanguagePreference(languagePreference);
        SetTimeFormatPreference(timeFormatPreference);
        _suppressChangeEvents = false;
    }

    public event Action<ThemePreference>? ThemePreferenceChanged;

    public event Action<AccentPalette>? AccentPaletteChanged;

    public event Action<WidgetDensity>? WidgetDensityChanged;

    public event Action<DisplayedLimitPreference>? DisplayedLimitPreferenceChanged;

    public event Action<bool>? StartWithWindowsChanged;

    public event Action<LanguagePreference>? LanguagePreferenceChanged;

    public event Action<TimeFormatPreference>? TimeFormatPreferenceChanged;

    public ThemePreference SelectedTheme =>
        LightThemeOption.IsChecked == true
            ? ThemePreference.Light
            : DarkThemeOption.IsChecked == true
                ? ThemePreference.Dark
                : ThemePreference.System;

    public AccentPalette SelectedAccentPalette =>
        VioletAccentOption.IsChecked == true
            ? AccentPalette.Violet
            : TealAccentOption.IsChecked == true
                ? AccentPalette.Teal
                : EmeraldAccentOption.IsChecked == true
                    ? AccentPalette.Emerald
                    : PinkAccentOption.IsChecked == true
                        ? AccentPalette.Pink
                        : AccentPalette.Blue;

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

    public LanguagePreference SelectedLanguage =>
        EnglishLanguageOption.IsChecked == true
            ? LanguagePreference.English
            : SimplifiedChineseLanguageOption.IsChecked == true
                ? LanguagePreference.SimplifiedChinese
                : LanguagePreference.System;

    public TimeFormatPreference SelectedTimeFormat =>
        TwentyFourHourTimeOption.IsChecked == true
            ? TimeFormatPreference.TwentyFourHour
            : TwelveHourTimeOption.IsChecked == true
                ? TimeFormatPreference.TwelveHour
                : TimeFormatPreference.Automatic;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var availableHeight = _workAreaProvider.GetAvailableHeightInDips(Owner ?? this);
        if (!double.IsFinite(availableHeight) || availableHeight <= 0d)
        {
            return;
        }

        MaxHeight = Math.Min(MaxHeight, availableHeight);
        Height = Math.Min(Height, MaxHeight);
    }

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

    public void SetLanguagePreference(LanguagePreference preference)
    {
        var previousSuppression = _suppressChangeEvents;
        _suppressChangeEvents = true;
        SystemLanguageOption.IsChecked = preference == LanguagePreference.System;
        EnglishLanguageOption.IsChecked = preference == LanguagePreference.English;
        SimplifiedChineseLanguageOption.IsChecked =
            preference == LanguagePreference.SimplifiedChinese;
        _suppressChangeEvents = previousSuppression;
    }

    public void SetTimeFormatPreference(TimeFormatPreference preference)
    {
        var previousSuppression = _suppressChangeEvents;
        _suppressChangeEvents = true;
        AutomaticTimeOption.IsChecked = preference == TimeFormatPreference.Automatic;
        TwentyFourHourTimeOption.IsChecked = preference == TimeFormatPreference.TwentyFourHour;
        TwelveHourTimeOption.IsChecked = preference == TimeFormatPreference.TwelveHour;
        _suppressChangeEvents = previousSuppression;
    }

    private void SetSelectedTheme(ThemePreference preference)
    {
        SystemThemeOption.IsChecked = preference == ThemePreference.System;
        LightThemeOption.IsChecked = preference == ThemePreference.Light;
        DarkThemeOption.IsChecked = preference == ThemePreference.Dark;
    }

    private void SetSelectedAccentPalette(AccentPalette palette)
    {
        BlueAccentOption.IsChecked = palette == AccentPalette.Blue;
        VioletAccentOption.IsChecked = palette == AccentPalette.Violet;
        TealAccentOption.IsChecked = palette == AccentPalette.Teal;
        EmeraldAccentOption.IsChecked = palette == AccentPalette.Emerald;
        PinkAccentOption.IsChecked = palette == AccentPalette.Pink;
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

    private void AccentPaletteOption_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_suppressChangeEvents)
        {
            AccentPaletteChanged?.Invoke(SelectedAccentPalette);
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

    private void LanguageOption_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_suppressChangeEvents)
        {
            LanguagePreferenceChanged?.Invoke(SelectedLanguage);
        }
    }

    private void TimeFormatOption_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!_suppressChangeEvents)
        {
            TimeFormatPreferenceChanged?.Invoke(SelectedTimeFormat);
        }
    }
}
