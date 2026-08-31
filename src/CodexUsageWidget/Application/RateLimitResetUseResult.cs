namespace CodexUsageWidget.Application;

public enum RateLimitResetUseStatus
{
    Reset,
    AlreadyRedeemed,
    NothingToReset,
    NoCredit,
    TimedOut,
    Failed
}

public sealed record RateLimitResetUseResult(
    RateLimitResetUseStatus Status,
    string? ErrorMessage = null);
