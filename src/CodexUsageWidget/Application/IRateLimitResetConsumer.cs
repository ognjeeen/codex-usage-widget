namespace CodexUsageWidget.Application;

public interface IRateLimitResetConsumer
{
    Task<RateLimitResetOutcome> ConsumeAsync(
        string? creditId,
        CancellationToken cancellationToken = default);
}
