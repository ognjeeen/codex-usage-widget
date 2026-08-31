using System.ComponentModel;
using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Tests;

public sealed class RateLimitResetUseCaseTests
{
    [Fact]
    public async Task UseReturnsFailureWhenTheResetAdapterCannotStartCodex()
    {
        var provider = new TrackingUsageProvider();
        await using var monitor = new UsageMonitor(provider);
        var useCase = new RateLimitResetUseCase(
            new ThrowingResetConsumer(
                new Win32Exception("The Codex executable could not be found.")),
            monitor);

        var result = await useCase.UseAsync("reset-2");

        Assert.Equal(RateLimitResetUseStatus.Failed, result.Status);
        Assert.Equal("The Codex executable could not be found.", result.ErrorMessage);
        Assert.Equal(0, provider.ReadCount);
    }

    private sealed class ThrowingResetConsumer(Exception exception) : IRateLimitResetConsumer
    {
        public Task<RateLimitResetOutcome> ConsumeAsync(
            string? creditId,
            CancellationToken cancellationToken = default)
        {
            _ = creditId;
            _ = cancellationToken;
            return Task.FromException<RateLimitResetOutcome>(exception);
        }
    }

    private sealed class TrackingUsageProvider : IUsageProvider
    {
        private int _readCount;

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

        public Task<UsageSnapshot> ReadUsageAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _readCount);
            return Task.FromResult(new UsageSnapshot(
                new UsageRateLimits([], PlanType: null, ResetCredits: null),
                TokenActivity: null,
                FetchedAt: DateTimeOffset.Now));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
