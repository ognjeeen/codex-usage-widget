using System.Globalization;
using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Tests;

[Collection("Localization")]
public sealed class AppLanguageControllerTests : IDisposable
{
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

    public void Dispose()
    {
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("en-US"));
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}

[CollectionDefinition("Localization", DisableParallelization = true)]
public sealed class LocalizationCollectionDefinition;
