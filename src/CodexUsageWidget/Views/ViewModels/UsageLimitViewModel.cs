using System.Windows.Media;
using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Views.ViewModels;

public sealed class UsageLimitViewModel
{
    public UsageLimitViewModel(string label, UsageWindow window)
    {
        Label = label;
        UsedPercent = window.UsedPercent;
        IsNormal = window.RemainingPercent > 25;
        UsedText = $"{Math.Round(window.UsedPercent):0}% used";
        RemainingText = $"{Math.Round(window.RemainingPercent):0}% remaining";
        ResetText = window.ResetsAt is null
            ? "Reset time unavailable"
            : $"Resets {UsageTextFormatter.FormatReset(window.ResetsAt.Value)}";
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
