using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Infrastructure.Preview;

public sealed class PreviewUsageHistoryStore : IUsageHistoryStore
{
    private IReadOnlyList<UsageHistoryEntry> _entries;

    public PreviewUsageHistoryStore(TimeProvider? timeProvider = null)
    {
        var now = (timeProvider ?? TimeProvider.System).GetLocalNow();
        _entries =
        [
            new UsageHistoryEntry(
                "codex",
                "5h limit",
                300,
                now.AddHours(-1),
                UsedPercent: 10,
                now.AddHours(2)),
            new UsageHistoryEntry(
                "codex",
                "5h limit",
                300,
                now.AddMinutes(-30),
                UsedPercent: 15,
                now.AddHours(2)),
            new UsageHistoryEntry(
                "codex",
                "Weekly limit",
                10_080,
                now.AddDays(-2),
                UsedPercent: 73,
                now.AddDays(2)),
            new UsageHistoryEntry(
                "codex",
                "Weekly limit",
                10_080,
                now.AddDays(-1),
                UsedPercent: 79,
                now.AddDays(2))
        ];
    }

    public IReadOnlyList<UsageHistoryEntry> Load() => _entries;

    public void Save(IReadOnlyList<UsageHistoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _entries = entries.ToArray();
    }
}
