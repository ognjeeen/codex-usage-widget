using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Tests;

public sealed class UsageHistoryTrackerTests
{
    [Fact]
    public void RecordCalculatesDailyPaceAndProjectedRemainingAtReset()
    {
        var store = new MemoryUsageHistoryStore();
        var tracker = new UsageHistoryTracker(store);
        var reset = new DateTimeOffset(2030, 1, 5, 12, 0, 0, TimeSpan.Zero);

        tracker.Record(CreateSnapshot(
            fetchedAt: new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero),
            usedPercent: 10,
            reset));
        tracker.Record(CreateSnapshot(
            fetchedAt: new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero),
            usedPercent: 13,
            reset));
        var summary = tracker.Record(CreateSnapshot(
            fetchedAt: new DateTimeOffset(2030, 1, 2, 12, 0, 0, TimeSpan.Zero),
            usedPercent: 16,
            reset));

        var pace = Assert.Single(summary.Limits);
        Assert.Equal(6, pace.UsedPercentPerDay);
        Assert.Equal(66, pace.ProjectedRemainingAtReset);
        Assert.Null(pace.ProjectedExhaustionAt);
        Assert.False(pace.WillExhaustBeforeReset);
        Assert.Equal(3, pace.History.Count);
    }

    [Fact]
    public void RecordStartsANewPaceSegmentWhenUsageResets()
    {
        var tracker = new UsageHistoryTracker(new MemoryUsageHistoryStore());
        var reset = new DateTimeOffset(2030, 1, 3, 0, 0, 0, TimeSpan.Zero);

        tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
            usedPercent: 70,
            reset,
            windowLabel: "5h limit",
            windowDurationMinutes: 300));
        tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 1, 1, 0, 0, TimeSpan.Zero),
            usedPercent: 80,
            reset,
            windowLabel: "5h limit",
            windowDurationMinutes: 300));
        tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 1, 2, 0, 0, TimeSpan.Zero),
            usedPercent: 5,
            reset,
            windowLabel: "5h limit",
            windowDurationMinutes: 300));
        tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 1, 2, 30, 0, TimeSpan.Zero),
            usedPercent: 7.5,
            reset,
            windowLabel: "5h limit",
            windowDurationMinutes: 300));
        var summary = tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 1, 3, 0, 0, TimeSpan.Zero),
            usedPercent: 10,
            reset,
            windowLabel: "5h limit",
            windowDurationMinutes: 300));

        var pace = Assert.Single(summary.Limits);
        Assert.Equal(120, pace.UsedPercentPerDay);
    }

    [Fact]
    public void RecordDisplaysOnlyTheCurrentResetCycle()
    {
        var tracker = new UsageHistoryTracker(new MemoryUsageHistoryStore());
        var firstReset = new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var secondReset = firstReset.AddDays(7);

        tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
            usedPercent: 70,
            firstReset));
        tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 1, 1, 0, 0, TimeSpan.Zero),
            usedPercent: 80,
            firstReset));
        tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero),
            usedPercent: 5,
            secondReset));
        var summary = tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 2, 1, 0, 0, TimeSpan.Zero),
            usedPercent: 10,
            secondReset));

        Assert.Equal(2, Assert.Single(summary.Limits).History.Count);
    }

    [Fact]
    public void RecordStartsANewCycleWhenTheResetTimeAdvancesWithoutAUsageDrop()
    {
        var tracker = new UsageHistoryTracker(new MemoryUsageHistoryStore());
        var firstReset = new DateTimeOffset(2030, 1, 1, 5, 0, 0, TimeSpan.Zero);
        var secondReset = firstReset.AddHours(5);

        tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
            usedPercent: 20,
            firstReset,
            windowLabel: "5h limit",
            windowDurationMinutes: 300));
        tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 1, 5, 0, 0, TimeSpan.Zero),
            usedPercent: 25,
            secondReset,
            windowLabel: "5h limit",
            windowDurationMinutes: 300));
        tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 1, 5, 30, 0, TimeSpan.Zero),
            usedPercent: 30,
            secondReset,
            windowLabel: "5h limit",
            windowDurationMinutes: 300));
        var summary = tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 1, 6, 0, 0, TimeSpan.Zero),
            usedPercent: 35,
            secondReset,
            windowLabel: "5h limit",
            windowDurationMinutes: 300));

        var pace = Assert.Single(summary.Limits);
        Assert.Equal(3, pace.History.Count);
        Assert.Equal(240, pace.UsedPercentPerDay);
    }

    [Theory]
    [InlineData("5h limit", 300, 60)]
    [InlineData("Weekly limit", 10_080, 1_440)]
    public void RecordWaitsForARepresentativeObservationBeforeCalculatingPace(
        string windowLabel,
        long windowDurationMinutes,
        int requiredMinutes)
    {
        var tracker = new UsageHistoryTracker(new MemoryUsageHistoryStore());
        var start = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var reset = start.AddMinutes(windowDurationMinutes);

        tracker.Record(CreateSnapshot(
            start,
            usedPercent: 10,
            reset,
            windowLabel,
            windowDurationMinutes));
        var premature = tracker.Record(CreateSnapshot(
            start.AddMinutes(requiredMinutes / 2d),
            usedPercent: 15,
            reset,
            windowLabel,
            windowDurationMinutes));
        var ready = tracker.Record(CreateSnapshot(
            start.AddMinutes(requiredMinutes),
            usedPercent: 20,
            reset,
            windowLabel,
            windowDurationMinutes));

        Assert.Null(Assert.Single(premature.Limits).UsedPercentPerDay);
        Assert.NotNull(Assert.Single(ready.Limits).UsedPercentPerDay);
    }

    [Fact]
    public void RecordKeepsOnlyTheLastSevenDaysOfHistory()
    {
        var reset = new DateTimeOffset(2030, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var store = new MemoryUsageHistoryStore(
        [
            new UsageHistoryEntry(
                "codex",
                "Weekly limit",
                10_080,
                new DateTimeOffset(2030, 1, 1, 23, 59, 0, TimeSpan.Zero),
                UsedPercent: 4,
                reset),
            new UsageHistoryEntry(
                "codex",
                "Weekly limit",
                10_080,
                new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero),
                UsedPercent: 5,
                reset)
        ]);
        var tracker = new UsageHistoryTracker(store);

        tracker.Record(CreateSnapshot(
            new DateTimeOffset(2030, 1, 9, 0, 0, 0, TimeSpan.Zero),
            usedPercent: 12,
            reset));

        Assert.Collection(
            store.Entries,
            entry => Assert.Equal(
                new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero),
                entry.CapturedAt),
            entry => Assert.Equal(
                new DateTimeOffset(2030, 1, 9, 0, 0, 0, TimeSpan.Zero),
                entry.CapturedAt));
    }

    private static UsageSnapshot CreateSnapshot(
        DateTimeOffset fetchedAt,
        double usedPercent,
        DateTimeOffset reset,
        string windowLabel = "Weekly limit",
        long windowDurationMinutes = 10_080) => new(
        new UsageRateLimits(
            [
                new UsageLimitBucket(
                    "codex",
                    "Codex",
                    IsGeneral: true,
                    [new UsageWindow(
                        windowLabel,
                        usedPercent,
                        windowDurationMinutes,
                        reset)],
                    Credits: null,
                    IndividualLimit: null,
                    ReachedState: null,
                    SpendControlReached: null)
            ],
            "pro",
            ResetCredits: null),
        TokenActivity: null,
        fetchedAt);

    private sealed class MemoryUsageHistoryStore : IUsageHistoryStore
    {
        public MemoryUsageHistoryStore(IReadOnlyList<UsageHistoryEntry>? entries = null)
        {
            Entries = entries ?? [];
        }

        public IReadOnlyList<UsageHistoryEntry> Entries { get; private set; }

        public IReadOnlyList<UsageHistoryEntry> Load() => Entries;

        public void Save(IReadOnlyList<UsageHistoryEntry> entries) =>
            Entries = entries.ToArray();
    }
}
