namespace CodexUsageWidget.Views.ViewModels;

public sealed record DailyUsageBarViewModel(
    double Height,
    string DateText,
    string? AxisLabel,
    bool IsFirstAxisLabel,
    bool IsLastAxisLabel,
    string TokensText,
    string PeakComparisonText);
