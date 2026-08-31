namespace CodexUsageWidget.Application;

public sealed class RateLimitResetUseCase
{
    private readonly IRateLimitResetConsumer _consumer;
    private readonly UsageMonitor _usageMonitor;
    private readonly TimeSpan _requestTimeout;

    public RateLimitResetUseCase(
        IRateLimitResetConsumer consumer,
        UsageMonitor usageMonitor,
        TimeSpan? requestTimeout = null)
    {
        _consumer = consumer;
        _usageMonitor = usageMonitor;
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(12);
    }

    public async Task<RateLimitResetUseResult> UseAsync(
        string? creditId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            timeout.CancelAfter(_requestTimeout);
            var outcome = await _consumer.ConsumeAsync(creditId, timeout.Token)
                .ConfigureAwait(false);

            await _usageMonitor.RefreshAfterCurrentAsync(cancellationToken)
                .ConfigureAwait(false);
            return new RateLimitResetUseResult(outcome switch
            {
                RateLimitResetOutcome.Reset => RateLimitResetUseStatus.Reset,
                RateLimitResetOutcome.AlreadyRedeemed => RateLimitResetUseStatus.AlreadyRedeemed,
                RateLimitResetOutcome.NothingToReset => RateLimitResetUseStatus.NothingToReset,
                RateLimitResetOutcome.NoCredit => RateLimitResetUseStatus.NoCredit,
                _ => throw new InvalidOperationException(
                    $"Unknown rate-limit reset outcome: {outcome}.")
            });
        }
        catch (OperationCanceledException)
        {
            return new RateLimitResetUseResult(RateLimitResetUseStatus.TimedOut);
        }
        catch (Exception ex)
        {
            return new RateLimitResetUseResult(
                RateLimitResetUseStatus.Failed,
                ex.Message);
        }
    }
}
