using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Tests;

public sealed class UsageMonitorTests
{
    [Fact]
    public async Task RefreshAfterCurrentWaitsAndReadsFreshUsage()
    {
        var provider = new BlockingUsageProvider();
        await using var monitor = new UsageMonitor(provider);

        var activeRefresh = monitor.RefreshAsync();
        await provider.FirstReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var refreshAfterCurrent = monitor.RefreshAfterCurrentAsync();
        Assert.False(refreshAfterCurrent.IsCompleted);

        provider.ReleaseFirstRead.TrySetResult();
        await Task.WhenAll(activeRefresh, refreshAfterCurrent)
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, provider.ReadCount);
    }

    private sealed class BlockingUsageProvider : IUsageProvider
    {
        private int _readCount;

        public TaskCompletionSource FirstReadStarted { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseFirstRead { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public int ReadCount => Volatile.Read(ref _readCount);

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

        public async Task<UsageSnapshot> ReadUsageAsync(
            CancellationToken cancellationToken = default)
        {
            var read = Interlocked.Increment(ref _readCount);
            if (read == 1)
            {
                FirstReadStarted.TrySetResult();
                await ReleaseFirstRead.Task.WaitAsync(cancellationToken);
            }

            return new UsageSnapshot(
                new UsageRateLimits([], PlanType: null, ResetCredits: null),
                TokenActivity: null,
                FetchedAt: DateTimeOffset.Now);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
