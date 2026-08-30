using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class LanguagePreferenceStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void LoadUsesSystemWhenPreferenceDoesNotExist()
    {
        var store = new LanguagePreferenceStore(Path.Combine(_directory, "language.txt"));

        Assert.Equal(LanguagePreference.System, store.Load());
    }

    [Theory]
    [InlineData("system", LanguagePreference.System)]
    [InlineData("english", LanguagePreference.English)]
    [InlineData("simplified-chinese", LanguagePreference.SimplifiedChinese)]
    [InlineData("SIMPLIFIED-CHINESE", LanguagePreference.SimplifiedChinese)]
    [InlineData("unknown", LanguagePreference.System)]
    public void LoadParsesStableValueOrFallsBackToSystem(
        string value,
        LanguagePreference expected)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "language.txt"), value);
        var store = new LanguagePreferenceStore(Path.Combine(_directory, "language.txt"));

        Assert.Equal(expected, store.Load());
    }

    [Theory]
    [InlineData(LanguagePreference.System, "system")]
    [InlineData(LanguagePreference.English, "english")]
    [InlineData(LanguagePreference.SimplifiedChinese, "simplified-chinese")]
    public void SavePersistsStablePreferenceValue(
        LanguagePreference preference,
        string expectedValue)
    {
        var path = Path.Combine(_directory, "language.txt");
        var store = new LanguagePreferenceStore(path);

        store.Save(preference);

        Assert.Equal(expectedValue, File.ReadAllText(path));
        Assert.Equal(preference, store.Load());
    }

    [Fact]
    public void LoadUsesSystemWhenPreferenceCannotBeRead()
    {
        Directory.CreateDirectory(_directory);
        var store = new LanguagePreferenceStore(_directory);

        Assert.Equal(LanguagePreference.System, store.Load());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
