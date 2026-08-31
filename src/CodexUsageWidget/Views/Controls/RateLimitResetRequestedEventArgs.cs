using CodexUsageWidget.Views.ViewModels;

namespace CodexUsageWidget.Views.Controls;

public sealed class RateLimitResetRequestedEventArgs(
    RateLimitResetCreditViewModel credit) : EventArgs
{
    public RateLimitResetCreditViewModel Credit { get; } = credit;
}
