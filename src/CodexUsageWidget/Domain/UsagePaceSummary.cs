namespace CodexUsageWidget.Domain;

public sealed record UsagePaceSummary(IReadOnlyList<UsageLimitPace> Limits)
{
    public static UsagePaceSummary Empty { get; } = new([]);
}

public sealed record UsageLimitPace(
    string LimitId,
    string WindowLabel,
    long? WindowDurationMinutes,
    double? UsedPercentPerDay,
    double? ProjectedRemainingAtReset,
    DateTimeOffset? ProjectedExhaustionAt,
    bool WillExhaustBeforeReset,
    IReadOnlyList<UsagePacePoint> History);

public sealed record UsagePacePoint(
    DateTimeOffset CapturedAt,
    double RemainingPercent);
