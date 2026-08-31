using System.Windows.Media;
using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Views.ViewModels;

public sealed class UsageLimitViewModel
{
    public UsageLimitViewModel(
        string label,
        UsageWindow window,
        TimeFormatPreference timeFormatPreference = TimeFormatPreference.Automatic)
    {
        Label = label;
        UsedPercent = window.UsedPercent;
        IsNormal = window.RemainingPercent > 25;
        UsedText = Strings.Format("Usage_UsedPercent", Math.Round(window.UsedPercent));
        RemainingText = Strings.Format(
            "Usage_RemainingPercent",
            Math.Round(window.RemainingPercent));
        ResetText = window.ResetsAt is null
            ? Strings.Get("Usage_ResetUnavailable")
            : UsageTextFormatter.FormatReset(
                window.ResetsAt.Value,
                timeFormatPreference: timeFormatPreference);
        ProgressBrush = new SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                UsageTextFormatter.ColorForRemaining(window.RemainingPercent)));
    }

    public string Label { get; }

    public double UsedPercent { get; }

    public bool IsNormal { get; }

    public string UsedText { get; }

    public string RemainingText { get; }

    public string ResetText { get; }

    public System.Windows.Media.Brush ProgressBrush { get; }
}
