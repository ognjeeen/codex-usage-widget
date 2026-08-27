using System.Globalization;
using System.Windows.Media;
using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Views.ViewModels;

public sealed class UsageWidgetViewModel
{
    private UsageWidgetViewModel()
    {
    }

    public string StatusText { get; private init; } = "Connecting…";

    public System.Windows.Media.Brush StatusBrush { get; private init; } = BrushFromHex("#D6A15F");

    public string HeadlineRemainingText { get; private init; } = "--%";

    public string HeadlineLabel { get; private init; } = "Waiting for Codex";

    public string UpdatedText { get; private init; } = "Local only · waiting for sync";

    public string? WarningText { get; private init; }

    public IReadOnlyList<UsageLimitViewModel> GeneralLimits { get; private init; } =
        Array.Empty<UsageLimitViewModel>();

    public IReadOnlyList<UsageLimitViewModel> ModelLimits { get; private init; } =
        Array.Empty<UsageLimitViewModel>();

    public IReadOnlyList<DetailMetricViewModel> AccountMetrics { get; private init; } =
        Array.Empty<DetailMetricViewModel>();

    public TokenActivityViewModel? TokenActivity { get; private init; }

    public bool HasWarning => WarningText is not null;

    public bool HasModelLimits => ModelLimits.Count > 0;

    public bool HasAccountMetrics => AccountMetrics.Count > 0;

    public bool HasTokenActivity => TokenActivity is not null;

    public double? HeadlineRemainingPercent { get; private init; }

    public DateTimeOffset? HeadlineResetsAt { get; private init; }

    public static UsageWidgetViewModel Loading(string updatedText = "Local only · waiting for sync") => new()
    {
        UpdatedText = updatedText
    };

    public static UsageWidgetViewModel Error(string message) => new()
    {
        StatusText = "Offline",
        StatusBrush = BrushFromHex("#E16D76"),
        HeadlineLabel = message,
        UpdatedText = "Local only · select refresh to retry"
    };

    public UsageWidgetViewModel Syncing() => new()
    {
        StatusText = "Syncing…",
        StatusBrush = BrushFromHex("#D6A15F"),
        HeadlineRemainingText = HeadlineRemainingText,
        HeadlineLabel = HeadlineLabel,
        UpdatedText = UpdatedText,
        WarningText = WarningText,
        GeneralLimits = GeneralLimits,
        ModelLimits = ModelLimits,
        AccountMetrics = AccountMetrics,
        TokenActivity = TokenActivity,
        HeadlineRemainingPercent = HeadlineRemainingPercent,
        HeadlineResetsAt = HeadlineResetsAt
    };

    public static UsageWidgetViewModel FromSnapshot(
        UsageSnapshot snapshot,
        UsageWindow? displayedWindow)
    {
        var generalLimits = BuildLimitViewModels(snapshot.GeneralLimits, includeBucketLabel: false);
        if (generalLimits.Length == 0 || displayedWindow is not { } displayed)
        {
            return Error("No subscription limits returned. Run codex login first.");
        }

        var modelLimits = BuildLimitViewModels(
            snapshot.RateLimits.Limits.Where(limit => !limit.IsGeneral),
            includeBucketLabel: true);
        var plan = FormatPlan(snapshot.RateLimits.PlanType);

        return new UsageWidgetViewModel
        {
            StatusText = plan is null ? "Live · ChatGPT" : $"Live · {plan}",
            StatusBrush = BrushFromHex("#68B88A"),
            HeadlineRemainingText = $"{Math.Round(displayed.RemainingPercent):0}%",
            HeadlineLabel = $"{displayed.Label} remaining",
            HeadlineRemainingPercent = displayed.RemainingPercent,
            HeadlineResetsAt = displayed.ResetsAt,
            UpdatedText = $"Local only · updated {snapshot.FetchedAt:HH:mm:ss}",
            WarningText = BuildWarning(snapshot.RateLimits.Limits),
            GeneralLimits = generalLimits,
            ModelLimits = modelLimits,
            AccountMetrics = BuildAccountMetrics(snapshot.RateLimits),
            TokenActivity = snapshot.TokenActivity is null
                ? null
                : new TokenActivityViewModel(snapshot.TokenActivity)
        };
    }

    private static UsageLimitViewModel[] BuildLimitViewModels(
        IEnumerable<UsageLimitBucket> limits,
        bool includeBucketLabel) => limits
        .SelectMany(limit => limit.Windows.Select(window => new UsageLimitViewModel(
            includeBucketLabel ? $"{limit.Label} · {window.Label}" : window.Label,
            window)))
        .ToArray();

    private static List<DetailMetricViewModel> BuildAccountMetrics(
        UsageRateLimits rateLimits)
    {
        var metrics = new List<DetailMetricViewModel>();
        var general = rateLimits.Limits.FirstOrDefault(limit => limit.IsGeneral);

        if (general?.Credits is { } credits)
        {
            var value = credits.Unlimited
                ? "Unlimited"
                : !string.IsNullOrWhiteSpace(credits.Balance)
                    ? $"{credits.Balance} remaining"
                    : credits.HasCredits ? "Available" : "None available";
            metrics.Add(new DetailMetricViewModel("ChatGPT credits", value));
        }

        if (general?.IndividualLimit is { } spendLimit)
        {
            metrics.Add(new DetailMetricViewModel(
                "Individual spend limit",
                $"{spendLimit.Used} of {spendLimit.Limit} · " +
                $"{Math.Round(spendLimit.RemainingPercent):0}% remaining"));
            metrics.Add(new DetailMetricViewModel(
                "Spend limit resets",
                spendLimit.ResetsAt.ToString("ddd HH:mm", CultureInfo.CurrentCulture)));
        }

        if (rateLimits.ResetCredits is { } resetCredits)
        {
            metrics.Add(new DetailMetricViewModel(
                "Rate-limit resets",
                $"{resetCredits.AvailableCount:N0} available"));
        }

        return metrics;
    }

    private static string? BuildWarning(IEnumerable<UsageLimitBucket> limits)
    {
        var reached = limits.FirstOrDefault(limit => limit.ReachedState is not null);
        if (reached?.ReachedState is { } reachedState)
        {
            return reachedState switch
            {
                "workspace_owner_credits_depleted" or "workspace_member_credits_depleted" =>
                    "Workspace credits are depleted.",
                "workspace_owner_usage_limit_reached" or "workspace_member_usage_limit_reached" =>
                    "Workspace usage limit reached.",
                _ => "Codex usage limit reached."
            };
        }

        return limits.Any(limit => limit.SpendControlReached == true)
            ? "Individual spend control reached."
            : null;
    }

    private static string? FormatPlan(string? planType) => planType switch
    {
        null => null,
        "free" => "Free",
        "go" => "Go",
        "plus" => "Plus",
        "pro" or "prolite" => "Pro",
        "team" or "business" or "self_serve_business_usage_based" => "Business",
        "enterprise" or "enterprise_cbp_usage_based" or "ent26" => "Enterprise",
        "edu" => "Edu",
        "preview" => "Preview",
        _ => "ChatGPT"
    };

    private static SolidColorBrush BrushFromHex(string color) =>
        new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
}
