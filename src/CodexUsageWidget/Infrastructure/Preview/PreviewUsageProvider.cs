using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Infrastructure.Preview;

public sealed class PreviewUsageProvider : IUsageProvider, IRateLimitResetConsumer
{
    private readonly IUsageProvider _wrappedProvider;
    private readonly TimeProvider _timeProvider;
    private readonly object _resetCreditsLock = new();
    private readonly List<RateLimitResetCredit> _resetCredits;
    private bool _disposed;

    public PreviewUsageProvider(
        IUsageProvider wrappedProvider,
        TimeProvider? timeProvider = null)
    {
        _wrappedProvider = wrappedProvider;
        _timeProvider = timeProvider ?? TimeProvider.System;
        var now = _timeProvider.GetLocalNow();
        _resetCredits =
        [
            CreateResetCredit("preview-reset-1", now, now.AddDays(7)),
            CreateResetCredit("preview-reset-2", now, now.AddDays(18)),
            CreateResetCredit("preview-reset-3", now, now.AddDays(32))
        ];
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
        ResetCreditSummary resetCredits;
        lock (_resetCreditsLock)
        {
            resetCredits = new ResetCreditSummary(
                _resetCredits.Count,
                _resetCredits.ToArray());
        }

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
                ResetCredits: resetCredits),
            TokenActivity: null,
            FetchedAt: now);

        return Task.FromResult(snapshot);
    }

    public Task<RateLimitResetOutcome> ConsumeAsync(
        string? creditId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_resetCreditsLock)
        {
            var selected = creditId is null
                ? _resetCredits
                    .OrderBy(credit => credit.ExpiresAt is null)
                    .ThenBy(credit => credit.ExpiresAt)
                    .FirstOrDefault()
                : _resetCredits.FirstOrDefault(credit =>
                    string.Equals(credit.Id, creditId, StringComparison.Ordinal));
            if (selected is null)
            {
                return Task.FromResult(RateLimitResetOutcome.NoCredit);
            }

            _resetCredits.Remove(selected);
        }

        return Task.FromResult(RateLimitResetOutcome.Reset);
    }

    private static RateLimitResetCredit CreateResetCredit(
        string id,
        DateTimeOffset grantedAt,
        DateTimeOffset expiresAt) => new(
            id,
            "available",
            grantedAt,
            expiresAt,
            "Rate-limit reset",
            "Reset an eligible Codex rate-limit window.");

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
