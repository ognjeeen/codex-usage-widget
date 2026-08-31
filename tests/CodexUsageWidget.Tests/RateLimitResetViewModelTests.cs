using System.Globalization;
using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;
using CodexUsageWidget.Localization;
using CodexUsageWidget.Views.ViewModels;

namespace CodexUsageWidget.Tests;

[Collection("Localization")]
public sealed class RateLimitResetViewModelTests : IDisposable
{
    [Fact]
    public void SnapshotShowsSelectableResetsByExpirationAndUnknownRemainder()
    {
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("en-US"));
        var window = new UsageWindow("Weekly limit", 100, 10_080, null);
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
                "pro",
                new ResetCreditSummary(
                    AvailableCount: 3,
                    Credits:
                    [
                        new RateLimitResetCredit(
                            "reset-later",
                            "available",
                            new DateTimeOffset(2030, 8, 1, 10, 0, 0, TimeSpan.Zero),
                            new DateTimeOffset(2030, 9, 18, 14, 30, 0, TimeSpan.Zero),
                            "Rate-limit reset",
                            null),
                        new RateLimitResetCredit(
                            "reset-sooner",
                            "available",
                            new DateTimeOffset(2030, 8, 1, 10, 0, 0, TimeSpan.Zero),
                            new DateTimeOffset(2030, 9, 7, 8, 47, 0, TimeSpan.Zero),
                            "Rate-limit reset",
                            null)
                    ])),
            TokenActivity: null,
            FetchedAt: new DateTimeOffset(2030, 8, 31, 9, 0, 0, TimeSpan.Zero));

        var viewModel = UsageWidgetViewModel.FromSnapshot(snapshot, window);

        var resets = Assert.IsType<RateLimitResetSummaryViewModel>(viewModel.ResetCredits);
        Assert.Equal("3 available", resets.AvailableText);
        Assert.Equal("Next reset expires Sep 7 at 8:47 AM", resets.ToolTipText);
        Assert.Collection(
            resets.Credits,
            reset =>
            {
                Assert.Equal("reset-sooner", reset.CreditId);
                Assert.Equal("Expires Sep 7 at 8:47 AM", reset.ExpirationText);
                Assert.Equal("Use reset", reset.UseButtonText);
            },
            reset => Assert.Equal("reset-later", reset.CreditId),
            reset =>
            {
                Assert.Null(reset.CreditId);
                Assert.Equal("Expiration unavailable", reset.ExpirationText);
                Assert.Equal("Use next reset", reset.UseButtonText);
            });

        var twentyFourHourViewModel = UsageWidgetViewModel.FromSnapshot(
            snapshot,
            window,
            TimeFormatPreference.TwentyFourHour);
        var twentyFourHourResets = Assert.IsType<RateLimitResetSummaryViewModel>(
            twentyFourHourViewModel.ResetCredits);
        Assert.Equal("Next reset expires Sep 7 at 08:47", twentyFourHourResets.ToolTipText);
        Assert.Equal("Expires Sep 7 at 08:47", twentyFourHourResets.Credits[0].ExpirationText);
    }

    public void Dispose() =>
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("en-US"));
}
