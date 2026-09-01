using System.Globalization;
using System.Windows;
using System.Windows.Media;
using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;
using CodexUsageWidget.Localization;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfPoint = System.Windows.Point;

namespace CodexUsageWidget.Views.ViewModels;

public sealed class UsagePaceViewModel
{
    private const double ChartWidth = 280d;
    private const double ChartHeight = 42d;
    private const double ChartPadding = 3d;

    public UsagePaceViewModel(
        UsagePaceSummary summary,
        TimeFormatPreference timeFormatPreference)
    {
        ArgumentNullException.ThrowIfNull(summary);
        Limits = summary.Limits
            .Select(limit => BuildLimit(limit, timeFormatPreference))
            .ToArray();
    }

    public IReadOnlyList<UsageLimitPaceViewModel> Limits { get; }

    private static UsageLimitPaceViewModel BuildLimit(
        UsageLimitPace pace,
        TimeFormatPreference timeFormatPreference)
    {
        var paceText = pace.History.Count < 2
            ? Strings.Get("Pace_TrackingStarted")
            : pace.WindowDurationMinutes < 1_440 || pace.UsedPercentPerDay is null
                ? BuildObservedUsageText(pace)
                : Strings.Format("Pace_UsedPerDay", FormatRate(pace.UsedPercentPerDay.Value));
        var projectionText = pace switch
        {
            { History.Count: < 2 } => Strings.Get("Pace_FirstTrendAfterReadings"),
            { WillExhaustBeforeReset: true, ProjectedExhaustionAt: { } exhaustion } =>
                Strings.Format(
                    "Pace_ProjectedExhaustion",
                    TimeTextFormatter.FormatDayAndTime(exhaustion, timeFormatPreference)),
            { WindowDurationMinutes: < 1_440, UsedPercentPerDay: not null } => string.Empty,
            { ProjectedRemainingAtReset: { } remaining } => Strings.Format(
                "Pace_ProjectedRemaining",
                Math.Round(remaining)),
            { UsedPercentPerDay: null, WindowDurationMinutes: < 1_440 } =>
                Strings.Get("Pace_NeedsShortHistory"),
            _ => Strings.Get("Pace_NeedsLongHistory")
        };

        var history = BuildHistory(pace.History, timeFormatPreference);
        var chartPoints = BuildChartPoints(pace.History);
        var areaPoints = new PointCollection();
        if (chartPoints.Count > 0)
        {
            areaPoints.Add(new WpfPoint(0, ChartHeight));
            foreach (var point in chartPoints)
            {
                areaPoints.Add(point);
            }

            areaPoints.Add(new WpfPoint(ChartWidth, ChartHeight));
        }

        var remainingPercent = pace.History.Count > 0
            ? pace.History[^1].RemainingPercent
            : 100d;
        var currentPoint = chartPoints.Count > 0
            ? chartPoints[^1]
            : new WpfPoint(ChartWidth, ChartHeight / 2d);
        return new UsageLimitPaceViewModel(
            UsageLabelLocalizer.Localize(pace.WindowLabel),
            paceText,
            projectionText,
            pace.WillExhaustBeforeReset,
            remainingPercent > 25d,
            new SolidColorBrush(
                (System.Windows.Media.Color)WpfColorConverter.ConvertFromString(
                    UsageTextFormatter.ColorForRemaining(remainingPercent))),
            chartPoints,
            areaPoints,
            currentPoint.X - 3d,
            currentPoint.Y - 3d,
            history);
    }

    private static UsagePacePointViewModel[] BuildHistory(
        IReadOnlyList<UsagePacePoint> history,
        TimeFormatPreference timeFormatPreference)
    {
        const int maximumPoints = 24;
        var selected = history.Count <= maximumPoints
            ? history.ToArray()
            : Enumerable.Range(0, maximumPoints)
                .Select(index => history[(int)Math.Round(
                    index * (history.Count - 1d) / (maximumPoints - 1d))])
                .ToArray();

        return selected.Select((point, index) =>
        {
            var remaining = Math.Round(Math.Clamp(point.RemainingPercent, 0d, 100d));
            var time = TimeTextFormatter.FormatDayAndTime(
                point.CapturedAt,
                timeFormatPreference);
            if (index == 0)
            {
                return new UsagePacePointViewModel(
                    Strings.Format("Pace_PointFirstReadout", time, remaining));
            }

            var deltaUsed = Math.Max(
                0d,
                selected[index - 1].RemainingPercent - point.RemainingPercent);
            return new UsagePacePointViewModel(
                Strings.Format(
                    "Pace_PointReadout",
                    time,
                    remaining,
                    FormatRate(deltaUsed)));
        })
            .ToArray();
    }

    private static PointCollection BuildChartPoints(IReadOnlyList<UsagePacePoint> history)
    {
        if (history.Count < 2)
        {
            return [];
        }

        const int maximumPoints = 24;
        var selected = history.Count <= maximumPoints
            ? history.ToArray()
            : Enumerable.Range(0, maximumPoints)
                .Select(index => history[(int)Math.Round(
                    index * (history.Count - 1d) / (maximumPoints - 1d))])
                .ToArray();
        var points = new PointCollection(selected.Length);
        var totalSeconds = selected.Length > 1
            ? (selected[^1].CapturedAt - selected[0].CapturedAt).TotalSeconds
            : 0d;
        for (var index = 0; index < selected.Length; index++)
        {
            var x = selected.Length == 1
                ? ChartWidth
                : totalSeconds > 0d
                    ? ChartWidth *
                        (selected[index].CapturedAt - selected[0].CapturedAt).TotalSeconds /
                        totalSeconds
                    : ChartWidth * index / (selected.Length - 1d);
            var remaining = Math.Clamp(selected[index].RemainingPercent, 0d, 100d);
            var y = ChartPadding +
                remaining * (ChartHeight - 2d * ChartPadding) / 100d;
            points.Add(new WpfPoint(x, y));
        }

        return points;
    }

    private static string FormatRate(double value) =>
        Math.Max(0d, value).ToString("0.#", CultureInfo.CurrentCulture);

    private static string BuildObservedUsageText(UsageLimitPace pace)
    {
        if (pace.History.Count < 2)
        {
            return Strings.Get("Pace_Collecting");
        }

        var usedPercent = Math.Max(
            0d,
            pace.History[0].RemainingPercent - pace.History[^1].RemainingPercent);
        var elapsed = pace.History[^1].CapturedAt - pace.History[0].CapturedAt;
        var totalMinutes = Math.Max(1, (int)Math.Round(elapsed.TotalMinutes));
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        var elapsedText = (hours, minutes) switch
        {
            (> 0, > 0) => Strings.Format("Pace_HoursMinutesShort", hours, minutes),
            (> 0, _) => Strings.Format("Pace_HoursShort", hours),
            _ => Strings.Format("Pace_MinutesShort", minutes)
        };
        return usedPercent <= 0d
            ? Strings.Format("Pace_NoChangeInObservation", elapsedText)
            : Strings.Format(
                "Pace_UsedInObservation",
                FormatRate(usedPercent),
                elapsedText);
    }
}

public sealed record UsageLimitPaceViewModel(
    string Label,
    string PaceText,
    string ProjectionText,
    bool HasWarning,
    bool IsNormal,
    System.Windows.Media.Brush ProgressBrush,
    PointCollection ChartPoints,
    PointCollection AreaPoints,
    double CurrentPointLeft,
    double CurrentPointTop,
    IReadOnlyList<UsagePacePointViewModel> History);

public sealed record UsagePacePointViewModel(
    string ToolTipText);
