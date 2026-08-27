using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class ThemePreferenceStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void LoadUsesSystemWhenPreferenceDoesNotExist()
    {
        var store = new ThemePreferenceStore(Path.Combine(_directory, "theme.txt"));

        Assert.Equal(ThemePreference.System, store.Load());
    }

    [Theory]
    [InlineData(ThemePreference.System)]
    [InlineData(ThemePreference.Light)]
    [InlineData(ThemePreference.Dark)]
    public void SavePersistsPreference(ThemePreference preference)
    {
        var store = new ThemePreferenceStore(Path.Combine(_directory, "theme.txt"));

        store.Save(preference);

        Assert.Equal(preference, store.Load());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
