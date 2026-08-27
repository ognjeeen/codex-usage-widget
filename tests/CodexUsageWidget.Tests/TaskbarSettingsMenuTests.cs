using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using System.Windows.Media;
using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Infrastructure.Windows;
using CodexUsageWidget.Views;
using CodexUsageWidget.Views.Controls;

namespace CodexUsageWidget.Tests;

public sealed class TaskbarSettingsMenuTests
{
    [Fact]
    public void SettingsUiOffersExpectedActionsAndThemeChoices()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var application = new App();
                application.InitializeComponent();
                var themeTestDirectory = Path.Combine(
                    Path.GetTempPath(),
                    "CodexUsageWidget.Tests",
                    Guid.NewGuid().ToString("N"));
                var themeStore = new ThemePreferenceStore(
                    Path.Combine(themeTestDirectory, "theme.txt"));
                themeStore.Save(ThemePreference.Dark);
                var accentStore = new AccentPaletteStore(
                    Path.Combine(themeTestDirectory, "accent.txt"));
                accentStore.Save(AccentPalette.Violet);
                using var themeController = new AppThemeController(
                    application,
                    new ThemePreferenceMonitor(themeStore, new WindowsThemeMonitor()),
                    accentStore);
                try
                {
                var window = new TaskbarLabelWindow();
                var menu = Assert.IsType<ContextMenu>(window.FindName("TaskbarMenu"));

                window.SetSystemTheme(EffectiveTheme.Light);
                var activityDots = Assert.IsType<ActivityDotsIndicator>(
                    window.FindName("ActivityDots"));
                var activityDotBrush = Assert.IsType<SolidColorBrush>(activityDots.DotBrush);
                Assert.Equal(Color.FromRgb(32, 33, 36), activityDotBrush.Color);

                Assert.Contains(
                    menu.Items.OfType<MenuItem>(),
                    item => string.Equals(
                        item.Header as string,
                        "Check for updates...",
                        StringComparison.Ordinal));
                Assert.Contains(
                    menu.Items.OfType<MenuItem>(),
                    item => string.Equals(
                        item.Header as string,
                        "Settings...",
                        StringComparison.Ordinal));
                Assert.DoesNotContain(
                    menu.Items.OfType<MenuItem>(),
                    item => string.Equals(
                        item.Header as string,
                        "Activity dots...",
                        StringComparison.Ordinal));
                Assert.DoesNotContain(
                    menu.Items.OfType<MenuItem>(),
                    item => string.Equals(
                        item.Header as string,
                        "Displayed limit",
                        StringComparison.Ordinal));
                Assert.DoesNotContain(
                    menu.Items.OfType<MenuItem>(),
                    item => string.Equals(
                        item.Header as string,
                        "Start with Windows",
                        StringComparison.Ordinal));

                var cases = new[]
                {
                    (ThemePreference.System, "SystemThemeOption"),
                    (ThemePreference.Light, "LightThemeOption"),
                    (ThemePreference.Dark, "DarkThemeOption")
                };
                foreach (var (preference, expectedOptionName) in cases)
                {
                    var settings = new SettingsWindow(
                        themePreference: preference,
                        widgetDensity: WidgetDensity.Compact,
                        displayedLimitPreference: DisplayedLimitPreference.FiveHour,
                        fiveHourLimitAvailable: true,
                        startWithWindowsEnabled: false,
                        activityHookSetupService: new StubActivityHookSetupService(),
                        codexLauncher: new StubCodexLauncher());
                    var option = Assert.IsType<RadioButton>(
                        settings.FindName(expectedOptionName));
                    Assert.True(option.IsChecked);
                    settings.Close();
                }

                var liveSettings = new SettingsWindow(
                    themePreference: ThemePreference.System,
                    widgetDensity: WidgetDensity.Compact,
                    displayedLimitPreference: DisplayedLimitPreference.FiveHour,
                    fiveHourLimitAvailable: true,
                    startWithWindowsEnabled: false,
                    activityHookSetupService: new StubActivityHookSetupService(),
                    codexLauncher: new StubCodexLauncher());
                ThemePreference? changedPreference = null;
                liveSettings.ThemePreferenceChanged += preference =>
                    changedPreference = preference;

                var lightOption = Assert.IsType<RadioButton>(
                    liveSettings.FindName("LightThemeOption"));
                lightOption.IsChecked = true;

                Assert.Equal(ThemePreference.Light, changedPreference);
                liveSettings.Close();

                var usageSettings = new SettingsWindow(
                    themePreference: ThemePreference.System,
                    widgetDensity: WidgetDensity.Compact,
                    displayedLimitPreference: DisplayedLimitPreference.Weekly,
                    fiveHourLimitAvailable: false,
                    startWithWindowsEnabled: true,
                    activityHookSetupService: new StubActivityHookSetupService(),
                    codexLauncher: new StubCodexLauncher(),
                    accentPalette: AccentPalette.Violet);
                Assert.NotNull(usageSettings.FindName("ActivityDotsSection"));
                var activityDotsHost = Assert.IsType<ContentControl>(
                    usageSettings.FindName("ActivityDotsHost"));
                Assert.IsType<ActivityHookSetupControl>(activityDotsHost.Content);
                Assert.NotNull(usageSettings.FindName("BlueAccentOption"));
                var violetAccentOption = Assert.IsType<RadioButton>(
                    usageSettings.FindName("VioletAccentOption"));
                Assert.True(violetAccentOption.IsChecked);
                Assert.NotNull(usageSettings.FindName("TealAccentOption"));
                Assert.NotNull(usageSettings.FindName("EmeraldAccentOption"));
                Assert.NotNull(usageSettings.FindName("PinkAccentOption"));
                AccentPalette? changedAccentPalette = null;
                usageSettings.AccentPaletteChanged += palette =>
                    changedAccentPalette = palette;
                var emeraldAccentOption = Assert.IsType<RadioButton>(
                    usageSettings.FindName("EmeraldAccentOption"));
                emeraldAccentOption.IsChecked = true;

                Assert.Equal(AccentPalette.Emerald, changedAccentPalette);
                var weeklyOption = Assert.IsType<RadioButton>(
                    usageSettings.FindName("WeeklyLimitOption"));
                var fiveHourOption = Assert.IsType<RadioButton>(
                    usageSettings.FindName("FiveHourLimitOption"));
                Assert.True(weeklyOption.IsChecked);
                Assert.False(fiveHourOption.IsEnabled);

                DisplayedLimitPreference? changedLimit = null;
                usageSettings.DisplayedLimitPreferenceChanged += preference =>
                    changedLimit = preference;
                var mostConstrainedOption = Assert.IsType<RadioButton>(
                    usageSettings.FindName("MostConstrainedLimitOption"));
                mostConstrainedOption.IsChecked = true;

                Assert.Equal(DisplayedLimitPreference.MostConstrained, changedLimit);

                var startupOption = Assert.IsType<CheckBox>(
                    usageSettings.FindName("StartWithWindowsOption"));
                Assert.True(startupOption.IsChecked);
                bool? changedStartupState = null;
                usageSettings.StartWithWindowsChanged += enabled =>
                    changedStartupState = enabled;
                startupOption.IsChecked = false;

                Assert.False(changedStartupState);

                var compactLayoutOption = Assert.IsType<RadioButton>(
                    usageSettings.FindName("CompactLayoutOption"));
                Assert.True(compactLayoutOption.IsChecked);
                WidgetDensity? changedDensity = null;
                usageSettings.WidgetDensityChanged += density =>
                    changedDensity = density;
                var detailedLayoutOption = Assert.IsType<RadioButton>(
                    usageSettings.FindName("DetailedLayoutOption"));
                detailedLayoutOption.IsChecked = true;

                Assert.Equal(WidgetDensity.Detailed, changedDensity);
                usageSettings.Close();

                var constrainedSettings = new SettingsWindow(
                    themePreference: ThemePreference.System,
                    widgetDensity: WidgetDensity.Compact,
                    displayedLimitPreference: DisplayedLimitPreference.FiveHour,
                    fiveHourLimitAvailable: true,
                    startWithWindowsEnabled: false,
                    activityHookSetupService: new StubActivityHookSetupService(),
                    codexLauncher: new StubCodexLauncher(),
                    workAreaProvider: new StubWindowWorkAreaProvider(600d));
                constrainedSettings.Show();
                try
                {
                    Assert.Equal(600d, constrainedSettings.Height);
                }
                finally
                {
                    constrainedSettings.Close();
                }

                    var accentButton = new AccentButton
                    {
                        Content = "Accent action"
                    };
                    accentButton.Style = Assert.IsType<System.Windows.Style>(
                        application.FindResource("PrimaryDialogButton"));
                    accentButton.ApplyTemplate();
                    Assert.Equal(
                        Color.FromRgb(124, 58, 237),
                        Assert.IsType<SolidColorBrush>(accentButton.PrimaryBrush).Color);

                    themeController.SetAccentPalette(AccentPalette.Emerald);
                    accentButton.RaiseEvent(
                        new System.Windows.RoutedEventArgs(
                            System.Windows.FrameworkElement.LoadedEvent));
                    try
                    {
                        Assert.Equal(
                            Color.FromRgb(29, 145, 72),
                            Assert.IsType<SolidColorBrush>(accentButton.PrimaryBrush).Color);
                        Assert.Equal(
                            Color.FromRgb(29, 145, 72),
                            GetButtonSurfaceColor(accentButton));
                    }
                    finally
                    {
                        accentButton.RaiseEvent(
                            new System.Windows.RoutedEventArgs(
                                System.Windows.FrameworkElement.UnloadedEvent));
                    }

                window.Close();
                application.Shutdown();
                }
                finally
                {
                    if (Directory.Exists(themeTestDirectory))
                    {
                        Directory.Delete(themeTestDirectory, recursive: true);
                    }
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);

        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static Color GetButtonSurfaceColor(Button button)
    {
        var surface = Assert.IsType<Border>(button.Template.FindName("ButtonSurface", button));
        return Assert.IsType<SolidColorBrush>(surface.Background).Color;
    }

    private sealed class StubActivityHookSetupService : IActivityHookSetupService
    {
        public Task<ActivityHookSetupStatus> GetStatusAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ActivityHookSetupStatus(ActivityHookSetupState.Active));

        public ActivityHookChangePreview PrepareChange(ActivityHookChangeKind kind) =>
            new(kind, HasChanges: false, ProposedContent: string.Empty);

        public void ApplyChange(ActivityHookChangePreview preview)
        {
        }
    }

    private sealed class StubCodexLauncher : ICodexLauncher
    {
        public void OpenInteractive()
        {
        }
    }

    private sealed class StubWindowWorkAreaProvider(double availableHeight) : IWindowWorkAreaProvider
    {
        public double GetAvailableHeightInDips(System.Windows.Window referenceWindow) =>
            availableHeight;
    }
}
