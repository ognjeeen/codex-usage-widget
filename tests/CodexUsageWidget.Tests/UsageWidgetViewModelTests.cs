using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;
using CodexUsageWidget.Views.ViewModels;

namespace CodexUsageWidget.Tests;

public sealed class UsageWidgetViewModelTests
{
    [Fact]
    public void FromSnapshotUsesMostConstrainedGeneralWindowForHeadline()
    {
        var reset = DateTimeOffset.Now.AddHours(2);
        var snapshot = CreateSnapshot(
            new UsageLimitBucket(
                "codex",
                "Codex",
                IsGeneral: true,
                [
                    new UsageWindow("5h limit", 20, 300, reset),
                    new UsageWindow("Weekly limit", 85, 10_080, reset.AddDays(2))
                ],
                Credits: null,
                IndividualLimit: null,
                ReachedState: null,
                SpendControlReached: null));

        var viewModel = UsageWidgetViewModel.FromSnapshot(
            snapshot,
            snapshot.MostConstrainedWindow);

        Assert.Equal("15%", viewModel.HeadlineRemainingText);
        Assert.Equal("Weekly limit remaining", viewModel.HeadlineLabel);
        Assert.Equal(15, viewModel.HeadlineRemainingPercent);
        Assert.Equal(2, viewModel.GeneralLimits.Count);
    }

    [Fact]
    public void FromSnapshotUsesDisplayedWindowForHeadline()
    {
        var reset = DateTimeOffset.Now.AddHours(2);
        var fiveHour = new UsageWindow("5h limit", 20, 300, reset);
        var snapshot = CreateSnapshot(
            new UsageLimitBucket(
                "codex",
                "Codex",
                IsGeneral: true,
                [
                    fiveHour,
                    new UsageWindow("Weekly limit", 85, 10_080, reset.AddDays(2))
                ],
                Credits: null,
                IndividualLimit: null,
                ReachedState: null,
                SpendControlReached: null));

        var viewModel = UsageWidgetViewModel.FromSnapshot(snapshot, fiveHour);

        Assert.Equal("80%", viewModel.HeadlineRemainingText);
        Assert.Equal("5h limit remaining", viewModel.HeadlineLabel);
        Assert.Equal(80, viewModel.HeadlineRemainingPercent);
        Assert.Equal(reset, viewModel.HeadlineResetsAt);
    }

    [Fact]
    public void FromSnapshotLabelsPreviewUsage()
    {
        var window = new UsageWindow("5h limit", 20, 300, DateTimeOffset.Now.AddHours(2));
        var snapshot = new UsageSnapshot(
            new UsageRateLimits(
                [
                    new UsageLimitBucket(
                        "codex",
                        "Codex",
                        IsGeneral: true,
                        [window],
                        Credits: null,
                        IndividualLimit: null,
                        ReachedState: null,
                        SpendControlReached: null)
                ],
                "preview",
                ResetCredits: null),
            TokenActivity: null,
            DateTimeOffset.Now);

        var viewModel = UsageWidgetViewModel.FromSnapshot(snapshot, window);

        Assert.Equal("Live · Preview", viewModel.StatusText);
    }

    [Fact]
    public void FromSnapshotFormatsUpdatedTimeUsingPreference()
    {
        var window = new UsageWindow("5h limit", 20, 300, null);
        var snapshot = new UsageSnapshot(
            new UsageRateLimits(
                [new UsageLimitBucket(
                    "codex",
                    "Codex",
                    IsGeneral: true,
                    [window],
                    Credits: null,
                    IndividualLimit: null,
                    ReachedState: null,
                    SpendControlReached: null)],
                "pro",
                ResetCredits: null),
            TokenActivity: null,
            new DateTimeOffset(2030, 8, 31, 14, 5, 9, TimeSpan.Zero));

        var viewModel = UsageWidgetViewModel.FromSnapshot(
            snapshot,
            window,
            TimeFormatPreference.TwelveHour);

        Assert.Equal("Local only · updated 2:05:09 PM", viewModel.UpdatedText);
    }

    [Fact]
    public void FromSnapshotKeepsModelSpecificLimitsOutOfGeneralList()
    {
        var snapshot = CreateSnapshot(
            new UsageLimitBucket(
                "codex",
                "Codex",
                IsGeneral: true,
                [new UsageWindow("Weekly limit", 25, 10_080, null)],
                Credits: null,
                IndividualLimit: null,
                ReachedState: null,
                SpendControlReached: null),
            new UsageLimitBucket(
                "codex_bengalfox",
                "GPT-5.3-Codex-Spark",
                IsGeneral: false,
                [new UsageWindow("Weekly limit", 90, 10_080, null)],
                Credits: null,
                IndividualLimit: null,
                ReachedState: null,
                SpendControlReached: null));

        var viewModel = UsageWidgetViewModel.FromSnapshot(
            snapshot,
            snapshot.MostConstrainedWindow);

        Assert.Single(viewModel.GeneralLimits);
        var modelLimit = Assert.Single(viewModel.ModelLimits);
        Assert.Contains("GPT-5.3-Codex-Spark", modelLimit.Label, StringComparison.Ordinal);
    }

    [Fact]
    public void FromSnapshotBuildsUsagePacePresentation()
    {
        var reset = new DateTimeOffset(2030, 1, 5, 12, 0, 0, TimeSpan.Zero);
        var window = new UsageWindow("Weekly limit", 16, 10_080, reset);
        var snapshot = CreateSnapshot(new UsageLimitBucket(
            "codex",
            "Codex",
            IsGeneral: true,
            [window],
            Credits: null,
            IndividualLimit: null,
            ReachedState: null,
            SpendControlReached: null));
        var pace = new UsagePaceSummary(
        [
            new UsageLimitPace(
                "codex",
                "Weekly limit",
                10_080,
                UsedPercentPerDay: 6,
                ProjectedRemainingAtReset: 66,
                ProjectedExhaustionAt: null,
                WillExhaustBeforeReset: false,
                [
                    new UsagePacePoint(
                        new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero),
                        RemainingPercent: 90),
                    new UsagePacePoint(
                        new DateTimeOffset(2030, 1, 2, 12, 0, 0, TimeSpan.Zero),
                        RemainingPercent: 84)
                ])
        ]);

        var viewModel = UsageWidgetViewModel.FromSnapshot(
            snapshot,
            window,
            TimeFormatPreference.TwentyFourHour,
            pace);

        var presented = Assert.Single(Assert.IsType<UsagePaceViewModel>(viewModel.UsagePace).Limits);
        Assert.Equal("Weekly limit", presented.Label);
        Assert.Equal("6% used per day", presented.PaceText);
        Assert.Equal("At current pace: about 66% left before reset", presented.ProjectionText);
        Assert.False(presented.HasWarning);
        Assert.Equal(2, presented.History.Count);
    }

    [Fact]
    public void UsagePaceChartUsesElapsedTimeForHorizontalSpacing()
    {
        var start = new DateTimeOffset(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var pace = new UsagePaceSummary(
        [
            new UsageLimitPace(
                "codex",
                "5h limit",
                300,
                UsedPercentPerDay: 60,
                ProjectedRemainingAtReset: null,
                ProjectedExhaustionAt: null,
                WillExhaustBeforeReset: false,
                [
                    new UsagePacePoint(start, RemainingPercent: 90),
                    new UsagePacePoint(start.AddHours(1), RemainingPercent: 85),
                    new UsagePacePoint(start.AddHours(4), RemainingPercent: 80)
                ])
        ]);

        var presented = Assert.Single(
            new UsagePaceViewModel(pace, TimeFormatPreference.TwentyFourHour).Limits);

        Assert.Equal(70, presented.ChartPoints[1].X);
    }

    [Fact]
    public void UsagePaceChartRisesAsUsageIncreases()
    {
        var start = new DateTimeOffset(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var pace = new UsagePaceSummary(
        [
            new UsageLimitPace(
                "codex",
                "5h limit",
                300,
                UsedPercentPerDay: null,
                ProjectedRemainingAtReset: null,
                ProjectedExhaustionAt: null,
                WillExhaustBeforeReset: false,
                [
                    new UsagePacePoint(start, RemainingPercent: 90),
                    new UsagePacePoint(start.AddMinutes(30), RemainingPercent: 70)
                ])
        ]);

        var presented = Assert.Single(
            new UsagePaceViewModel(pace, TimeFormatPreference.TwentyFourHour).Limits);

        Assert.True(presented.ChartPoints[1].Y < presented.ChartPoints[0].Y);
    }

    [Fact]
    public void ShortWindowShowsObservedChangeWithoutANormalCaseProjection()
    {
        var start = new DateTimeOffset(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var pace = new UsagePaceSummary(
        [
            new UsageLimitPace(
                "codex",
                "5h limit",
                300,
                UsedPercentPerDay: 240,
                ProjectedRemainingAtReset: 60,
                ProjectedExhaustionAt: null,
                WillExhaustBeforeReset: false,
                [
                    new UsagePacePoint(start, RemainingPercent: 90),
                    new UsagePacePoint(start.AddMinutes(30), RemainingPercent: 85),
                    new UsagePacePoint(start.AddMinutes(66), RemainingPercent: 80)
                ])
        ]);

        var presented = Assert.Single(
            new UsagePaceViewModel(pace, TimeFormatPreference.TwentyFourHour).Limits);

        Assert.Equal("10% used · 1h 6m observed", presented.PaceText);
        Assert.Equal(string.Empty, presented.ProjectionText);
    }

    [Theory]
    [InlineData("5h limit", 300)]
    [InlineData("Weekly limit", 10_080)]
    public void ColdStartShowsTrackingStateWithoutAChart(
        string label,
        long durationMinutes)
    {
        var pace = new UsagePaceSummary(
        [
            new UsageLimitPace(
                "codex",
                label,
                durationMinutes,
                UsedPercentPerDay: null,
                ProjectedRemainingAtReset: null,
                ProjectedExhaustionAt: null,
                WillExhaustBeforeReset: false,
                [new UsagePacePoint(DateTimeOffset.Now, RemainingPercent: 90)])
        ]);

        var presented = Assert.Single(
            new UsagePaceViewModel(pace, TimeFormatPreference.TwentyFourHour).Limits);

        Assert.Equal("Tracking started", presented.PaceText);
        Assert.Equal("First trend after the next readings", presented.ProjectionText);
        Assert.Empty(presented.ChartPoints);
        Assert.Empty(presented.AreaPoints);
    }

    [Fact]
    public void WeeklyWindowShowsObservedTrendBeforeProjectionIsReady()
    {
        var start = new DateTimeOffset(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var pace = new UsagePaceSummary(
        [
            new UsageLimitPace(
                "codex",
                "Weekly limit",
                10_080,
                UsedPercentPerDay: null,
                ProjectedRemainingAtReset: null,
                ProjectedExhaustionAt: null,
                WillExhaustBeforeReset: false,
                [
                    new UsagePacePoint(start, RemainingPercent: 76),
                    new UsagePacePoint(start.AddMinutes(2), RemainingPercent: 76)
                ])
        ]);

        var presented = Assert.Single(
            new UsagePaceViewModel(pace, TimeFormatPreference.TwentyFourHour).Limits);

        Assert.Equal("No change · 2m observed", presented.PaceText);
        Assert.Equal(
            "Projection after 24h of local observations",
            presented.ProjectionText);
        Assert.Equal(2, presented.ChartPoints.Count);
    }

    [Fact]
    public void UsagePaceHistoryBuildsAConciseSingleLineReadout()
    {
        var start = new DateTimeOffset(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var pace = new UsagePaceSummary(
        [
            new UsageLimitPace(
                "codex",
                "5h limit",
                300,
                UsedPercentPerDay: null,
                ProjectedRemainingAtReset: null,
                ProjectedExhaustionAt: null,
                WillExhaustBeforeReset: false,
                [
                    new UsagePacePoint(start, RemainingPercent: 90),
                    new UsagePacePoint(start.AddMinutes(30), RemainingPercent: 85)
                ])
        ]);

        var presented = Assert.Single(
            new UsagePaceViewModel(pace, TimeFormatPreference.TwentyFourHour).Limits);
        var hoverText = presented.History[1].ToolTipText;

        Assert.DoesNotContain(Environment.NewLine, hoverText, StringComparison.Ordinal);
        Assert.Contains("85% remaining", hoverText, StringComparison.Ordinal);
        Assert.Contains("+5% since previous", hoverText, StringComparison.Ordinal);
        Assert.DoesNotContain("15% used", hoverText, StringComparison.Ordinal);
    }

    private static UsageSnapshot CreateSnapshot(params UsageLimitBucket[] limits) => new(
        new UsageRateLimits(limits, "pro", ResetCredits: null),
        TokenActivity: null,
        DateTimeOffset.Now);
}
