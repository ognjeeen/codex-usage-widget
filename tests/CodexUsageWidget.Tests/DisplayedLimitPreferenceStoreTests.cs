using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class DisplayedLimitPreferenceStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void LoadUsesFiveHourWhenPreferenceDoesNotExist()
    {
        var store = new DisplayedLimitPreferenceStore(
            Path.Combine(_directory, "displayed-limit.txt"));

        Assert.Equal(DisplayedLimitPreference.FiveHour, store.Load());
    }

    [Theory]
    [InlineData(DisplayedLimitPreference.FiveHour)]
    [InlineData(DisplayedLimitPreference.Weekly)]
    [InlineData(DisplayedLimitPreference.MostConstrained)]
    public void SavePersistsPreference(DisplayedLimitPreference preference)
    {
        var store = new DisplayedLimitPreferenceStore(
            Path.Combine(_directory, "displayed-limit.txt"));

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
