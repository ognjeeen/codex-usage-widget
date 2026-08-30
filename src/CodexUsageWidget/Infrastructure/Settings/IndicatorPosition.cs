namespace CodexUsageWidget.Infrastructure.Settings;

public readonly record struct IndicatorPosition(int HorizontalPercent, int VerticalPercent)
{
    public const int MinimumPercent = 0;
    public const int MaximumPercent = 100;

    public static IndicatorPosition BottomLeft => new(MinimumPercent, MaximumPercent);

    public IndicatorPosition Clamp() => new(
        Math.Clamp(HorizontalPercent, MinimumPercent, MaximumPercent),
        Math.Clamp(VerticalPercent, MinimumPercent, MaximumPercent));
}