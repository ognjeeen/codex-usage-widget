using System.Globalization;
using System.Windows.Media;
using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Views.ViewModels;

public sealed class UsageWidgetViewModel
{
    private UsageWidgetViewModel()
    {
    }

    public string StatusText { get; private init; } = Strings.Get("Status_Connecting");

    public System.Windows.Media.Brush StatusBrush { get; private init; } = BrushFromHex("#D6A15F");

    public string HeadlineRemainingText { get; private init; } = "--%";

    public string HeadlineLabel { get; private init; } = Strings.Get("Status_WaitingForCodex");

    public string UpdatedText { get; private init; } = Strings.Get("Status_LocalWaiting");

    public string? WarningText { get; private init; }

    public IReadOnlyList<UsageLimitViewModel> GeneralLimits { get; private init; } =
        Array.Empty<UsageLimitViewModel>();

    public IReadOnlyList<UsageLimitViewModel> ModelLimits { get; private init; } =
        Array.Empty<UsageLimitViewModel>();

    public IReadOnlyList<DetailMetricViewModel> AccountMetrics { get; private init; } =
        Array.Empty<DetailMetricViewModel>();

    public RateLimitResetSummaryViewModel? ResetCredits { get; private init; }

    public TokenActivityViewModel? TokenActivity { get; private init; }

    public bool HasWarning => WarningText is not null;

    public bool HasModelLimits => ModelLimits.Count > 0;

    public bool HasAccountMetrics => AccountMetrics.Count > 0 || ResetCredits is not null;

    public bool HasResetCredits => ResetCredits is not null;

    public bool HasTokenActivity => TokenActivity is not null;

    public double? HeadlineRemainingPercent { get; private init; }

    public DateTimeOffset? HeadlineResetsAt { get; private init; }

    public static UsageWidgetViewModel Loading(string? updatedText = null) => new()
    {
        UpdatedText = updatedText ?? Strings.Get("Status_LocalWaiting")
    };

    public static UsageWidgetViewModel Error(string message) => new()
    {
        StatusText = Strings.Get("Status_Offline"),
        StatusBrush = BrushFromHex("#E16D76"),
        HeadlineLabel = message,
        UpdatedText = Strings.Get("Status_LocalRetry")
    };

    public UsageWidgetViewModel Syncing() => new()
    {
        StatusText = Strings.Get("Status_Syncing"),
        StatusBrush = BrushFromHex("#D6A15F"),
        HeadlineRemainingText = HeadlineRemainingText,
        HeadlineLabel = HeadlineLabel,
        UpdatedText = UpdatedText,
        WarningText = WarningText,
        GeneralLimits = GeneralLimits,
        ModelLimits = ModelLimits,
        AccountMetrics = AccountMetrics,
        ResetCredits = ResetCredits,
        TokenActivity = TokenActivity,
        HeadlineRemainingPercent = HeadlineRemainingPercent,
        HeadlineResetsAt = HeadlineResetsAt
    };

    public static UsageWidgetViewModel FromSnapshot(
        UsageSnapshot snapshot,
        UsageWindow? displayedWindow,
        TimeFormatPreference timeFormatPreference = TimeFormatPreference.Automatic)
    {
        var generalLimits = BuildLimitViewModels(
            snapshot.GeneralLimits,
            includeBucketLabel: false,
            timeFormatPreference);
        if (generalLimits.Length == 0 || displayedWindow is not { } displayed)
        {
            return Error(Strings.Get("Status_NoLimits"));
        }

        var modelLimits = BuildLimitViewModels(
            snapshot.RateLimits.Limits.Where(limit => !limit.IsGeneral),
            includeBucketLabel: true,
            timeFormatPreference);
        var plan = FormatPlan(snapshot.RateLimits.PlanType);

        return new UsageWidgetViewModel
        {
            StatusText = Strings.Format("Status_LivePlan", plan ?? "ChatGPT"),
            StatusBrush = BrushFromHex("#68B88A"),
            HeadlineRemainingText = $"{Math.Round(displayed.RemainingPercent):0}%",
            HeadlineLabel = Strings.Format(
                "Status_HeadlineRemaining",
                UsageLabelLocalizer.Localize(displayed.Label)),
            HeadlineRemainingPercent = displayed.RemainingPercent,
            HeadlineResetsAt = displayed.ResetsAt,
            UpdatedText = Strings.Format(
                "Status_LocalUpdated",
                TimeTextFormatter.FormatTimeWithSeconds(
                    snapshot.FetchedAt,
                    timeFormatPreference)),
            WarningText = BuildWarning(snapshot.RateLimits.Limits),
            GeneralLimits = generalLimits,
            ModelLimits = modelLimits,
            AccountMetrics = BuildAccountMetrics(snapshot.RateLimits, timeFormatPreference),
            ResetCredits = BuildResetCredits(
                snapshot.RateLimits.ResetCredits,
                timeFormatPreference),
            TokenActivity = snapshot.TokenActivity is null
                ? null
                : new TokenActivityViewModel(snapshot.TokenActivity)
        };
    }

    private static UsageLimitViewModel[] BuildLimitViewModels(
        IEnumerable<UsageLimitBucket> limits,
        bool includeBucketLabel,
        TimeFormatPreference timeFormatPreference) => limits
        .SelectMany(limit => limit.Windows.Select(window => new UsageLimitViewModel(
            includeBucketLabel
                ? $"{limit.Label} · {UsageLabelLocalizer.Localize(window.Label)}"
                : UsageLabelLocalizer.Localize(window.Label),
            window,
            timeFormatPreference)))
        .ToArray();

    private static List<DetailMetricViewModel> BuildAccountMetrics(
        UsageRateLimits rateLimits,
        TimeFormatPreference timeFormatPreference)
    {
        var metrics = new List<DetailMetricViewModel>();
        var general = rateLimits.Limits.FirstOrDefault(limit => limit.IsGeneral);

        if (general?.Credits is { } credits)
        {
            var value = credits.Unlimited
                ? Strings.Get("Common_Unlimited")
                : !string.IsNullOrWhiteSpace(credits.Balance)
                    ? Strings.Format("Usage_CreditsRemaining", credits.Balance)
                    : credits.HasCredits
                        ? Strings.Get("Common_Available")
                        : Strings.Get("Common_NoneAvailable");
            metrics.Add(new DetailMetricViewModel(Strings.Get("Usage_ChatGptCredits"), value));
        }

        if (general?.IndividualLimit is { } spendLimit)
        {
            metrics.Add(new DetailMetricViewModel(
                Strings.Get("Usage_IndividualSpendLimit"),
                Strings.Format(
                    "Usage_SpendLimitValue",
                    spendLimit.Used,
                    spendLimit.Limit,
                    Math.Round(spendLimit.RemainingPercent))));
            metrics.Add(new DetailMetricViewModel(
                Strings.Get("Usage_SpendLimitResets"),
                TimeTextFormatter.FormatDayAndTime(
                    spendLimit.ResetsAt,
                    timeFormatPreference)));
        }

        return metrics;
    }

    private static RateLimitResetSummaryViewModel? BuildResetCredits(
        ResetCreditSummary? resetCredits,
        TimeFormatPreference timeFormatPreference)
    {
        if (resetCredits is null)
        {
            return null;
        }

        var availableCount = Math.Max(0, resetCredits.AvailableCount);
        var details = (resetCredits.Credits ?? [])
            .Where(credit => string.Equals(
                credit.Status,
                "available",
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(credit => credit.ExpiresAt is null)
            .ThenBy(credit => credit.ExpiresAt)
            .Take((int)Math.Min(availableCount, int.MaxValue))
            .Select(credit => new RateLimitResetCreditViewModel(
                credit.Id,
                credit.ExpiresAt,
                credit.ExpiresAt is { } expiresAt
                    ? Strings.Format(
                        "Usage_ResetExpires",
                        expiresAt,
                        TimeTextFormatter.FormatTime(expiresAt, timeFormatPreference))
                    : Strings.Get("Usage_ResetExpirationUnavailable"),
                Strings.Get("Usage_UseReset")))
            .ToList();

        if (availableCount > details.Count)
        {
            details.Add(new RateLimitResetCreditViewModel(
                CreditId: null,
                ExpiresAt: null,
                ExpirationText: Strings.Get("Usage_ResetExpirationUnavailable"),
                UseButtonText: Strings.Get("Usage_UseNextReset")));
        }

        var nextExpiration = details
            .Select(detail => detail.ExpiresAt)
            .OfType<DateTimeOffset>()
            .FirstOrDefault();
        return new RateLimitResetSummaryViewModel(
            Strings.Format(
                "Usage_CountAvailable",
                availableCount.ToString("N0", CultureInfo.CurrentCulture)),
            nextExpiration == default
                ? null
                : Strings.Format(
                    "Usage_ResetNextExpires",
                    nextExpiration,
                    TimeTextFormatter.FormatTime(nextExpiration, timeFormatPreference)),
            details);
    }

    private static string? BuildWarning(IEnumerable<UsageLimitBucket> limits)
    {
        var reached = limits.FirstOrDefault(limit => limit.ReachedState is not null);
        if (reached?.ReachedState is { } reachedState)
        {
            return reachedState switch
            {
                "workspace_owner_credits_depleted" or "workspace_member_credits_depleted" =>
                    Strings.Get("Warning_WorkspaceCreditsDepleted"),
                "workspace_owner_usage_limit_reached" or "workspace_member_usage_limit_reached" =>
                    Strings.Get("Warning_WorkspaceUsageLimitReached"),
                _ => Strings.Get("Warning_UsageLimitReached")
            };
        }

        return limits.Any(limit => limit.SpendControlReached == true)
            ? Strings.Get("Warning_SpendControlReached")
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
