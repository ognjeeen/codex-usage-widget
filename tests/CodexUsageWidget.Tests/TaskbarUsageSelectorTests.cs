using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Tests;

public sealed class TaskbarUsageSelectorTests
{
    [Fact]
    public void FiveHourPreferenceSelectsFiveHourWindowWhenWeeklyIsMoreConstrained()
    {
        var fiveHour = new UsageWindow("5h limit", 20, 300, DateTimeOffset.Now.AddHours(2));
        var weekly = new UsageWindow("Weekly limit", 85, 10_080, DateTimeOffset.Now.AddDays(2));
        var snapshot = CreateSnapshot(fiveHour, weekly);

        var selected = TaskbarUsageSelector.Select(snapshot, TaskbarLimitPreference.FiveHour);

        Assert.Same(fiveHour, selected);
    }

    [Fact]
    public void FiveHourPreferenceFallsBackToWeeklyWhenFiveHourWindowIsUnavailable()
    {
        var weekly = new UsageWindow("Weekly limit", 40, 10_080, DateTimeOffset.Now.AddDays(2));
        var snapshot = CreateSnapshot(weekly);

        var selected = TaskbarUsageSelector.Select(snapshot, TaskbarLimitPreference.FiveHour);

        Assert.Same(weekly, selected);
    }

    [Fact]
    public void WeeklyPreferenceSelectsWeeklyWindow()
    {
        var fiveHour = new UsageWindow("5h limit", 85, 300, DateTimeOffset.Now.AddHours(2));
        var weekly = new UsageWindow("Weekly limit", 20, 10_080, DateTimeOffset.Now.AddDays(2));
        var snapshot = CreateSnapshot(fiveHour, weekly);

        var selected = TaskbarUsageSelector.Select(snapshot, TaskbarLimitPreference.Weekly);

        Assert.Same(weekly, selected);
    }

    [Fact]
    public void MostConstrainedPreferenceSelectsWindowWithLowestRemainingPercentage()
    {
        var fiveHour = new UsageWindow("5h limit", 20, 300, DateTimeOffset.Now.AddHours(2));
        var weekly = new UsageWindow("Weekly limit", 85, 10_080, DateTimeOffset.Now.AddDays(2));
        var snapshot = CreateSnapshot(fiveHour, weekly);

        var selected = TaskbarUsageSelector.Select(
            snapshot,
            TaskbarLimitPreference.MostConstrained);

        Assert.Same(weekly, selected);
    }

    [Fact]
    public void WeeklyPreferenceFallsBackToFiveHourWhenWeeklyWindowIsUnavailable()
    {
        var fiveHour = new UsageWindow("5h limit", 20, 300, DateTimeOffset.Now.AddHours(2));
        var snapshot = CreateSnapshot(fiveHour);

        var selected = TaskbarUsageSelector.Select(snapshot, TaskbarLimitPreference.Weekly);

        Assert.Same(fiveHour, selected);
    }

    [Fact]
    public void DurationPreferenceFallsBackToMostConstrainedAvailableGeneralWindow()
    {
        var daily = new UsageWindow("1d limit", 75, 1_440, DateTimeOffset.Now.AddHours(4));
        var snapshot = CreateSnapshot(daily);

        var selected = TaskbarUsageSelector.Select(snapshot, TaskbarLimitPreference.FiveHour);

        Assert.Same(daily, selected);
    }

    [Fact]
    public void FiveHourPreferenceIsUnavailableWhenSnapshotHasNoFiveHourWindow()
    {
        var weekly = new UsageWindow("Weekly limit", 40, 10_080, DateTimeOffset.Now.AddDays(2));
        var snapshot = CreateSnapshot(weekly);

        var available = TaskbarUsageSelector.IsAvailable(
            snapshot,
            TaskbarLimitPreference.FiveHour);

        Assert.False(available);
    }

    [Fact]
    public void FiveHourPreferenceResolvesToWeeklyWhenFiveHourWindowIsUnavailable()
    {
        var weekly = new UsageWindow("Weekly limit", 40, 10_080, DateTimeOffset.Now.AddDays(2));
        var snapshot = CreateSnapshot(weekly);

        var resolved = TaskbarUsageSelector.ResolvePreference(
            snapshot,
            TaskbarLimitPreference.FiveHour);

        Assert.Equal(TaskbarLimitPreference.Weekly, resolved);
    }

    private static UsageSnapshot CreateSnapshot(params UsageWindow[] windows) => new(
        new UsageRateLimits(
            [
                new UsageLimitBucket(
                    "codex",
                    "Codex",
                    IsGeneral: true,
                    windows,
                    Credits: null,
                    IndividualLimit: null,
                    ReachedState: null,
                    SpendControlReached: null)
            ],
            "pro",
            ResetCredits: null),
        TokenActivity: null,
        DateTimeOffset.Now);
}
