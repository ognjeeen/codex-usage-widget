using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class TaskbarLimitPreferenceStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void LoadUsesFiveHourWhenPreferenceDoesNotExist()
    {
        var store = new TaskbarLimitPreferenceStore(
            Path.Combine(_directory, "taskbar-limit.txt"));

        Assert.Equal(TaskbarLimitPreference.FiveHour, store.Load());
    }

    [Theory]
    [InlineData(TaskbarLimitPreference.FiveHour)]
    [InlineData(TaskbarLimitPreference.Weekly)]
    [InlineData(TaskbarLimitPreference.MostConstrained)]
    public void SavePersistsPreference(TaskbarLimitPreference preference)
    {
        var store = new TaskbarLimitPreferenceStore(
            Path.Combine(_directory, "taskbar-limit.txt"));

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
