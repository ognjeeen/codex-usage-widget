namespace CodexUsageWidget.Views.ViewModels;

public sealed record DetailMetricViewModel(
    string Label,
    string Value,
    bool IsLast = false);
