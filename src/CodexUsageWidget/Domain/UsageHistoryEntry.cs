namespace CodexUsageWidget.Domain;

public sealed record UsageHistoryEntry(
    string LimitId,
    string WindowLabel,
    long? WindowDurationMinutes,
    DateTimeOffset CapturedAt,
    double UsedPercent,
    DateTimeOffset? ResetsAt);
