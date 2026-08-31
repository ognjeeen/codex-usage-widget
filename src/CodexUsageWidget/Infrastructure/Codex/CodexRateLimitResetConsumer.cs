using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Infrastructure.Codex;

public sealed class CodexRateLimitResetConsumer(
    ICodexAppServerSession session,
    RateLimitResetAttemptStore attemptStore) : IRateLimitResetConsumer
{
    public async Task<RateLimitResetOutcome> ConsumeAsync(
        string? creditId,
        CancellationToken cancellationToken = default)
    {
        if (creditId is not null && string.IsNullOrWhiteSpace(creditId))
        {
            throw new ArgumentException("Credit ID cannot be empty.", nameof(creditId));
        }

        var idempotencyKey = attemptStore.GetOrCreate(creditId);
        var parameters = new Dictionary<string, object?>
        {
            ["idempotencyKey"] = idempotencyKey
        };
        if (creditId is not null)
        {
            parameters["creditId"] = creditId;
        }

        var result = await session.RequestAsync(
                "account/rateLimitResetCredit/consume",
                parameters,
                cancellationToken)
            .ConfigureAwait(false);

        var outcome = result.GetProperty("outcome").GetString() switch
        {
            "reset" => RateLimitResetOutcome.Reset,
            "alreadyRedeemed" => RateLimitResetOutcome.AlreadyRedeemed,
            "nothingToReset" => RateLimitResetOutcome.NothingToReset,
            "noCredit" => RateLimitResetOutcome.NoCredit,
            var unknownOutcome => throw new InvalidOperationException(
                $"Unknown rate-limit reset outcome: {unknownOutcome ?? "<null>"}.")
        };
        attemptStore.Complete(creditId);
        return outcome;
    }
}
