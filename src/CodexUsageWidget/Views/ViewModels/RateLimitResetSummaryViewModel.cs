namespace CodexUsageWidget.Views.ViewModels;

public sealed record RateLimitResetSummaryViewModel(
    string AvailableText,
    string? ToolTipText,
    IReadOnlyList<RateLimitResetCreditViewModel> Credits)
{
    public bool HasSelectableCredits => Credits.Count > 0;
}

public sealed record RateLimitResetCreditViewModel(
    string? CreditId,
    DateTimeOffset? ExpiresAt,
    string ExpirationText,
    string UseButtonText);
