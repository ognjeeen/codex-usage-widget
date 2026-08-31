using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;
using CodexUsageWidget.Infrastructure.Preview;

namespace CodexUsageWidget.Tests;

public sealed class PreviewUsageProviderTests
{
    [Fact]
    public async Task ReadUsageReturnsFiveHourAndWeeklyWindowsWithoutReadingCodex()
    {
        var wrappedProvider = new TrackingUsageProvider();
        var now = new DateTimeOffset(2030, 4, 5, 10, 30, 0, TimeSpan.FromHours(2));
        await using var provider = new PreviewUsageProvider(
            wrappedProvider,
            new FixedTimeProvider(now));

        var snapshot = await provider.ReadUsageAsync();

        Assert.Equal("preview", snapshot.RateLimits.PlanType);
        Assert.Equal(now, snapshot.FetchedAt);
        Assert.Collection(
            snapshot.GeneralWindows,
            window =>
            {
                Assert.Equal("5h limit", window.Label);
                Assert.Equal(80, window.RemainingPercent);
                Assert.Equal(300, window.WindowDurationMinutes);
                Assert.Equal(now.AddHours(2), window.ResetsAt);
            },
            window =>
            {
                Assert.Equal("Weekly limit", window.Label);
                Assert.Equal(15, window.RemainingPercent);
                Assert.Equal(10_080, window.WindowDurationMinutes);
                Assert.Equal(now.AddDays(2), window.ResetsAt);
            });
        Assert.Equal(0, wrappedProvider.ReadCount);
    }

    [Fact]
    public async Task ConsumeRemovesOnlyTheSelectedSyntheticResetWithoutReadingCodex()
    {
        var wrappedProvider = new TrackingUsageProvider();
        var now = new DateTimeOffset(2030, 4, 5, 10, 30, 0, TimeSpan.FromHours(2));
        await using var provider = new PreviewUsageProvider(
            wrappedProvider,
            new FixedTimeProvider(now));
        var before = await provider.ReadUsageAsync();
        var selected = Assert.IsAssignableFrom<IReadOnlyList<RateLimitResetCredit>>(
            Assert.IsType<ResetCreditSummary>(before.RateLimits.ResetCredits).Credits)[1];

        var outcome = await provider.ConsumeAsync(selected.Id);
        var after = await provider.ReadUsageAsync();

        Assert.Equal(RateLimitResetOutcome.Reset, outcome);
        var remaining = Assert.IsType<ResetCreditSummary>(after.RateLimits.ResetCredits);
        Assert.Equal(2, remaining.AvailableCount);
        Assert.DoesNotContain(remaining.Credits!, credit => credit.Id == selected.Id);
        Assert.Equal(0, wrappedProvider.ReadCount);
    }

    private sealed class TrackingUsageProvider : IUsageProvider
    {
        public int ReadCount { get; private set; }

        public event EventHandler? RateLimitsChanged
        {
            add { }
            remove { }
        }

        public event EventHandler<string>? DiagnosticMessage
        {
            add { }
            remove { }
        }

        public Task<UsageSnapshot> ReadUsageAsync(CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromException<UsageSnapshot>(new InvalidOperationException());
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();

        public override TimeZoneInfo LocalTimeZone { get; } =
            TimeZoneInfo.CreateCustomTimeZone("Preview", now.Offset, "Preview", "Preview");
    }
}
