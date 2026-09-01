using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Application;

public interface IUsageHistoryStore
{
    IReadOnlyList<UsageHistoryEntry> Load();

    void Save(IReadOnlyList<UsageHistoryEntry> entries);
}
