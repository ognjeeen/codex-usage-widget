using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Views;

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
                var window = new TaskbarLabelWindow();
                var menu = Assert.IsType<ContextMenu>(window.FindName("TaskbarMenu"));

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
                        startWithWindowsEnabled: false);
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
                    startWithWindowsEnabled: false);
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
                    startWithWindowsEnabled: true);
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

                window.Close();
                application.Shutdown();
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
}
