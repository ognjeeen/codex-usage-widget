using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class TimeFormatPreferenceStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void LoadUsesAutomaticWhenPreferenceDoesNotExist()
    {
        var store = new TimeFormatPreferenceStore(
            Path.Combine(_directory, "time-format.txt"));

        Assert.Equal(TimeFormatPreference.Automatic, store.Load());
    }

    [Theory]
    [InlineData(TimeFormatPreference.Automatic)]
    [InlineData(TimeFormatPreference.TwentyFourHour)]
    [InlineData(TimeFormatPreference.TwelveHour)]
    public void SavePersistsPreference(TimeFormatPreference preference)
    {
        var store = new TimeFormatPreferenceStore(
            Path.Combine(_directory, "time-format.txt"));

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
