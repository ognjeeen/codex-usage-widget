using System.Globalization;

namespace CodexUsageWidget.Localization;

public static class UsageLabelLocalizer
{
    public static string Localize(string label, CultureInfo? culture = null)
    {
        culture ??= Strings.Current.Culture;
        return label switch
        {
            "Weekly limit" => Strings.GetForCulture("Usage_WeeklyLimit", culture),
            "Primary limit" => Strings.GetForCulture("Usage_PrimaryLimit", culture),
            "Secondary limit" => Strings.GetForCulture("Usage_SecondaryLimit", culture),
            _ when TryReadDuration(label, 'd', out var days) =>
                Strings.FormatForCulture(culture, "Usage_DayLimit", days),
            _ when TryReadDuration(label, 'h', out var hours) =>
                Strings.FormatForCulture(culture, "Usage_HourLimit", hours),
            _ when TryReadDuration(label, 'm', out var minutes) =>
                Strings.FormatForCulture(culture, "Usage_MinuteLimit", minutes),
            _ => label
        };
    }

    private static bool TryReadDuration(string label, char unit, out long value)
    {
        value = 0;
        var suffix = $"{unit} limit";
        return label.EndsWith(suffix, StringComparison.Ordinal) &&
            long.TryParse(
                label.AsSpan(0, label.Length - suffix.Length),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value);
    }
}
