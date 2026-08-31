using System.Collections.Concurrent;
using CodexUsageWidget.Application;

namespace CodexUsageWidget.Infrastructure.Codex;

public sealed class CodexRateLimitResetConsumer(
    ICodexAppServerSession session) : IRateLimitResetConsumer
{
    private readonly ConcurrentDictionary<ResetSelection, string> _pendingAttempts = new();

    public async Task<RateLimitResetOutcome> ConsumeAsync(
        string? creditId,
        CancellationToken cancellationToken = default)
    {
        if (creditId is not null && string.IsNullOrWhiteSpace(creditId))
        {
            throw new ArgumentException("Credit ID cannot be empty.", nameof(creditId));
        }

        var selection = new ResetSelection(creditId);
        var idempotencyKey = _pendingAttempts.GetOrAdd(
            selection,
            static _ => Guid.NewGuid().ToString());
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
        _pendingAttempts.TryRemove(selection, out _);
        return outcome;
    }

    private readonly record struct ResetSelection(string? CreditId);
}
