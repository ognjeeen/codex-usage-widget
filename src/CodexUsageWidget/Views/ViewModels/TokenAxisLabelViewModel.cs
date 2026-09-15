namespace CodexUsageWidget.Views.ViewModels;

public sealed record TokenAxisLabelViewModel(
    string Text,
    string VerticalText,
    double Position,
    bool IsFirst,
    bool IsLast)
{
    public string DayText => Text.Length >= 5 ? Text[3..5] : Text;

    public string MonthText => Text.Length >= 2 ? Text[..2] : Text;
}
