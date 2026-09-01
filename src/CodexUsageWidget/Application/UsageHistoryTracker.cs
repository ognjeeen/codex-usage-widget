using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Application;

public sealed class UsageHistoryTracker
{
    private static readonly TimeSpan MinimumShortWindowObservation = TimeSpan.FromHours(1);
    private static readonly TimeSpan MinimumLongWindowObservation = TimeSpan.FromDays(1);
    private static readonly TimeSpan HistoryRetention = TimeSpan.FromDays(7);
    private const int MinimumPacePointCount = 3;
    private const double ResetDropThresholdPercent = 1d;
    private readonly IUsageHistoryStore _store;

    public UsageHistoryTracker(IUsageHistoryStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public UsagePaceSummary Record(UsageSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var retentionStart = snapshot.FetchedAt - HistoryRetention;
        var entries = _store.Load()
            .Where(entry =>
                entry.CapturedAt >= retentionStart &&
                entry.CapturedAt <= snapshot.FetchedAt)
            .ToList();
        foreach (var limit in snapshot.GeneralLimits)
        {
            entries.AddRange(limit.Windows.Select(window => new UsageHistoryEntry(
                limit.LimitId,
                window.Label,
                window.WindowDurationMinutes,
                snapshot.FetchedAt,
                window.UsedPercent,
                window.ResetsAt)));
        }

        _store.Save(entries);

        var limits = snapshot.GeneralLimits
            .SelectMany(limit => limit.Windows.Select(window => CalculatePace(
                entries,
                limit.LimitId,
                window,
                snapshot.FetchedAt)))
            .ToArray();
        return new UsagePaceSummary(limits);
    }

    private static UsageLimitPace CalculatePace(
        IReadOnlyList<UsageHistoryEntry> entries,
        string limitId,
        UsageWindow window,
        DateTimeOffset now)
    {
        var matchingHistory = entries
            .Where(entry =>
                entry.LimitId == limitId &&
                entry.WindowDurationMinutes == window.WindowDurationMinutes &&
                (window.WindowDurationMinutes is not null ||
                 entry.WindowLabel == window.Label))
            .OrderBy(entry => entry.CapturedAt)
            .ToArray();
        var segmentStart = 0;
        for (var index = 1; index < matchingHistory.Length; index++)
        {
            var previous = matchingHistory[index - 1];
            var current = matchingHistory[index];
            var resetTimeAdvanced = current.WindowDurationMinutes is { } durationMinutes &&
                durationMinutes > 0 &&
                previous.ResetsAt is { } previousReset &&
                current.ResetsAt is { } currentReset &&
                currentReset - previousReset >= TimeSpan.FromMinutes(durationMinutes / 2d);
            if (previous.UsedPercent - current.UsedPercent >= ResetDropThresholdPercent ||
                resetTimeAdvanced)
            {
                segmentStart = index;
            }
        }

        var history = matchingHistory[segmentStart..];
        var points = history
            .Select(entry => new UsagePacePoint(
                entry.CapturedAt,
                Math.Clamp(100d - entry.UsedPercent, 0d, 100d)))
            .ToArray();
        var minimumObservation = window.WindowDurationMinutes < 1_440
            ? MinimumShortWindowObservation
            : MinimumLongWindowObservation;
        if (history.Length < MinimumPacePointCount ||
            history[^1].CapturedAt - history[0].CapturedAt < minimumObservation)
        {
            return new UsageLimitPace(
                limitId,
                window.Label,
                window.WindowDurationMinutes,
                UsedPercentPerDay: null,
                ProjectedRemainingAtReset: null,
                ProjectedExhaustionAt: null,
                WillExhaustBeforeReset: false,
                points);
        }

        var elapsedDays = (history[^1].CapturedAt - history[0].CapturedAt).TotalDays;
        var usedPercentPerDay = Math.Max(
            0d,
            (history[^1].UsedPercent - history[0].UsedPercent) / elapsedDays);
        var currentRemaining = Math.Clamp(100d - window.UsedPercent, 0d, 100d);
        double? projectedRemaining = null;
        DateTimeOffset? projectedExhaustion = null;
        var willExhaustBeforeReset = false;
        if (window.ResetsAt is { } resetsAt && resetsAt > now)
        {
            projectedRemaining = Math.Clamp(
                currentRemaining - usedPercentPerDay * (resetsAt - now).TotalDays,
                0d,
                100d);
            if (usedPercentPerDay > 0)
            {
                var exhaustion = now.AddDays(currentRemaining / usedPercentPerDay);
                if (exhaustion < resetsAt)
                {
                    projectedExhaustion = exhaustion;
                    willExhaustBeforeReset = true;
                }
            }
        }

        return new UsageLimitPace(
            limitId,
            window.Label,
            window.WindowDurationMinutes,
            usedPercentPerDay,
            projectedRemaining,
            projectedExhaustion,
            willExhaustBeforeReset,
            points);
    }
}
