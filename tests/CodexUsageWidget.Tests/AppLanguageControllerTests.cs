using System.Globalization;
using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;
using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Localization;
using CodexUsageWidget.Views.ViewModels;

namespace CodexUsageWidget.Tests;

[Collection("Localization")]
public sealed class AppLanguageControllerTests : IDisposable
{
    private readonly CultureInfo _originalWindowsRegionalCulture =
        Strings.Current.WindowsRegionalCulture;
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void ManualPreferenceUpdatesCultureAndPersistsOverride()
    {
        var store = new LanguagePreferenceStore(Path.Combine(_directory, "language.txt"));
        var controller = new AppLanguageController(
            store,
            CultureInfo.GetCultureInfo("zh-CN"));
        var languageChanged = false;
        Strings.Current.PropertyChanged += OnLanguageChanged;
        try
        {
            Assert.Equal(LanguagePreference.System, controller.Preference);
            Assert.Equal("zh-CN", Strings.Current.Culture.Name);

            controller.SetPreference(LanguagePreference.English);

            Assert.True(languageChanged);
            Assert.Equal("en-US", Strings.Current.Culture.Name);
            Assert.Equal(LanguagePreference.English, store.Load());
        }
        finally
        {
            Strings.Current.PropertyChanged -= OnLanguageChanged;
        }

        void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            languageChanged |= e.PropertyName == "Item[]";
        }
    }

    [Fact]
    public void ManualPreferenceFormatsVisibleDatesUsingSelectedLanguage()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        var controller = new AppLanguageController(
            LanguagePreference.SimplifiedChinese,
            CultureInfo.GetCultureInfo("en-US"));
        var date = new DateOnly(2026, 8, 11);
        var activity = new TokenActivitySummary(
            LifetimeTokens: null,
            PeakDailyTokens: null,
            LongestRunningTurnSeconds: null,
            CurrentStreakDays: null,
            LongestStreakDays: null,
            DailyUsage: [new DailyTokenUsage(date, 50_000)]);

        var viewModel = new TokenActivityViewModel(activity);

        Assert.Equal("星期二, 八月 11", viewModel.DailyBars[0].DateText);
    }

    [Fact]
    public void AutomaticTimeFormatUsesWindowsRegionalCultureAfterLanguageOverride()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _ = new AppLanguageController(
            LanguagePreference.English,
            CultureInfo.GetCultureInfo("en-US"));
        var value = new DateTimeOffset(2030, 8, 31, 14, 5, 0, TimeSpan.Zero);

        var result = TimeTextFormatter.FormatTime(
            value,
            TimeFormatPreference.Automatic);

        Assert.Equal("14:05", result);
    }

    public void Dispose()
    {
        Strings.Current.SetCulture(
            CultureInfo.GetCultureInfo("en-US"),
            _originalWindowsRegionalCulture);
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}

[CollectionDefinition("Localization", DisableParallelization = true)]
public sealed class LocalizationCollectionDefinition;
