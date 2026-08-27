using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Infrastructure.Preview;

public sealed class PreviewUsageProvider : IUsageProvider
{
    private readonly IUsageProvider _wrappedProvider;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    public PreviewUsageProvider(
        IUsageProvider wrappedProvider,
        TimeProvider? timeProvider = null)
    {
        _wrappedProvider = wrappedProvider;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public event EventHandler? RateLimitsChanged
    {
        add => _wrappedProvider.RateLimitsChanged += value;
        remove => _wrappedProvider.RateLimitsChanged -= value;
    }

    public event EventHandler<string>? DiagnosticMessage
    {
        add => _wrappedProvider.DiagnosticMessage += value;
        remove => _wrappedProvider.DiagnosticMessage -= value;
    }

    public Task<UsageSnapshot> ReadUsageAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var now = _timeProvider.GetLocalNow();
        var snapshot = new UsageSnapshot(
            new UsageRateLimits(
                [
                    new UsageLimitBucket(
                        "codex",
                        "Codex",
                        IsGeneral: true,
                        [
                            new UsageWindow(
                                "5h limit",
                                UsedPercent: 20,
                                WindowDurationMinutes: 300,
                                now.AddHours(2)),
                            new UsageWindow(
                                "Weekly limit",
                                UsedPercent: 85,
                                WindowDurationMinutes: 10_080,
                                now.AddDays(2))
                        ],
                        Credits: null,
                        IndividualLimit: null,
                        ReachedState: null,
                        SpendControlReached: null)
                ],
                PlanType: "preview",
                ResetCredits: null),
            TokenActivity: null,
            FetchedAt: now);

        return Task.FromResult(snapshot);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        return _wrappedProvider.DisposeAsync();
    }
}
