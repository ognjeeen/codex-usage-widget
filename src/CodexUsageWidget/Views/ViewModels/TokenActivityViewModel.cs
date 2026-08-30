using System.Globalization;
using CodexUsageWidget.Domain;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Views.ViewModels;

public sealed class TokenActivityViewModel
{
    private const int MaximumChartDays = 28;

    public TokenActivityViewModel(TokenActivitySummary activity)
    {
        Metrics = BuildMetrics(activity);
        DailyBars = BuildDailyBars(activity.DailyUsage);
    }

    public IReadOnlyList<DetailMetricViewModel> Metrics { get; }

    public IReadOnlyList<DailyUsageBarViewModel> DailyBars { get; }

    public bool HasDailyUsage => DailyBars.Count > 0;

    private static List<DetailMetricViewModel> BuildMetrics(TokenActivitySummary activity)
    {
        var metrics = new List<DetailMetricViewModel>();
        AddMetric(metrics, Strings.Get("Token_Lifetime"), FormatNumber(activity.LifetimeTokens));
        AddMetric(metrics, Strings.Get("Token_PeakDaily"), FormatNumber(activity.PeakDailyTokens));
        AddMetric(
            metrics,
            Strings.Get("Token_LongestTurn"),
            activity.LongestRunningTurnSeconds is { } seconds
                ? FormatDuration(seconds)
                : null);
        AddMetric(
            metrics,
            Strings.Get("Token_CurrentStreak"),
            activity.CurrentStreakDays is { } currentStreak
                ? Strings.Format("Token_Days", currentStreak.ToString("N0", CultureInfo.CurrentCulture))
                : null);
        AddMetric(
            metrics,
            Strings.Get("Token_LongestStreak"),
            activity.LongestStreakDays is { } longestStreak
                ? Strings.Format("Token_Days", longestStreak.ToString("N0", CultureInfo.CurrentCulture))
                : null);
        return metrics;
    }

    private static DailyUsageBarViewModel[] BuildDailyBars(
        IReadOnlyList<DailyTokenUsage> dailyUsage)
    {
        var recent = dailyUsage.TakeLast(MaximumChartDays).ToArray();
        if (recent.Length == 0)
        {
            return Array.Empty<DailyUsageBarViewModel>();
        }

        var maximum = Math.Max(1L, recent.Max(item => item.Tokens));
        return recent
            .Select(item => new DailyUsageBarViewModel(
                Math.Max(3d, 44d * item.Tokens / maximum),
                item.Date.ToString("dddd, MMMM d", CultureInfo.CurrentCulture),
                Strings.Format(
                    "Token_Count",
                    item.Tokens.ToString("N0", CultureInfo.CurrentCulture)),
                Strings.Format("Token_ChartPeak", Math.Round(100d * item.Tokens / maximum))))
            .ToArray();
    }

    private static void AddMetric(
        List<DetailMetricViewModel> metrics,
        string label,
        string? value)
    {
        if (value is not null)
        {
            metrics.Add(new DetailMetricViewModel(label, value));
        }
    }

    private static string? FormatNumber(long? value) => value is null
        ? null
        : value.Value.ToString("N0", CultureInfo.CurrentCulture);

    private static string FormatDuration(long seconds)
    {
        var duration = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return duration.TotalHours >= 1
            ? Strings.Format(
                "Token_DurationHoursMinutes",
                (int)duration.TotalHours,
                duration.Minutes)
            : Strings.Format(
                "Token_DurationMinutes",
                Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes)));
    }
}
