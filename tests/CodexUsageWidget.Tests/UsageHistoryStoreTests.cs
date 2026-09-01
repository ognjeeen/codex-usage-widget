using CodexUsageWidget.Domain;
using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class UsageHistoryStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void SavePersistsHistoryForTheNextStoreInstance()
    {
        var path = Path.Combine(_directory, "usage-history.json");
        var entry = new UsageHistoryEntry(
            "codex",
            "Weekly limit",
            10_080,
            new DateTimeOffset(2030, 1, 2, 12, 0, 0, TimeSpan.Zero),
            UsedPercent: 16,
            new DateTimeOffset(2030, 1, 5, 12, 0, 0, TimeSpan.Zero));

        new UsageHistoryStore(path).Save([entry]);
        var loaded = new UsageHistoryStore(path).Load();

        Assert.Equal(entry, Assert.Single(loaded));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
