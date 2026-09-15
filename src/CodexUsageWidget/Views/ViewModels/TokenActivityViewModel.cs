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
        var recent = activity.DailyUsage.TakeLast(MaximumChartDays).ToArray();
        DailyBars = BuildDailyBars(recent);
        AxisLabels = BuildAxisLabels(recent);
    }

    public IReadOnlyList<DetailMetricViewModel> Metrics { get; }

    public IReadOnlyList<DailyUsageBarViewModel> DailyBars { get; }

    public IReadOnlyList<TokenAxisLabelViewModel> AxisLabels { get; }

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
        return metrics
            .Select((metric, index) => metric with { IsLast = index == metrics.Count - 1 })
            .ToList();
    }

    private static DailyUsageBarViewModel[] BuildDailyBars(
        DailyTokenUsage[] recent)
    {
        if (recent.Length == 0)
        {
            return Array.Empty<DailyUsageBarViewModel>();
        }

        var maximum = Math.Max(1L, recent.Max(item => item.Tokens));
        var lastIndex = recent.Length - 1;
        return recent
            .Select((item, index) => new DailyUsageBarViewModel(
                Math.Max(3d, 76d * item.Tokens / maximum),
                item.Date.ToString("dddd, MMMM d", CultureInfo.CurrentCulture),
                BuildAxisLabel(item.Date, index, lastIndex),
                index == 0,
                index == lastIndex,
                Strings.Format(
                    "Token_Count",
                    item.Tokens.ToString("N0", CultureInfo.CurrentCulture)),
                Strings.Format("Token_ChartPeak", Math.Round(100d * item.Tokens / maximum))))
            .ToArray();
    }

    private static TokenAxisLabelViewModel[] BuildAxisLabels(
        DailyTokenUsage[] recent)
    {
        if (recent.Length == 0)
        {
            return Array.Empty<TokenAxisLabelViewModel>();
        }

        var lastIndex = recent.Length - 1;
        return Enumerable.Range(0, recent.Length)
            .Where(index => index == 0 || index % 5 == 0 || index == lastIndex)
            .Select(index => new TokenAxisLabelViewModel(
                BuildAxisLabel(recent[index].Date, index, lastIndex)!,
                BuildVerticalAxisLabel(BuildAxisLabel(recent[index].Date, index, lastIndex)!),
                lastIndex == 0 ? 0d : index / (double)lastIndex,
                IsFirst: index == 0,
                IsLast: index == lastIndex))
            .ToArray();
    }

    private static string BuildVerticalAxisLabel(string text)
    {
        var separator = text.IndexOf('/');
        return separator > 0
            ? $"{text[(separator + 1)..]}\n·\n{text[..separator]}"
            : text;
    }

    private static string? BuildAxisLabel(DateOnly date, int index, int lastIndex)
    {
        if (index == lastIndex)
        {
            return date.ToString("MM/dd", CultureInfo.InvariantCulture);
        }

        return index == 0 || index % 5 == 0
            ? date.ToString("MM/dd", CultureInfo.InvariantCulture)
            : null;
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
