using System.Globalization;

namespace CodexUsageWidget.Application;

public static class TimeTextFormatter
{
    public static string FormatTime(
        DateTimeOffset value,
        TimeFormatPreference preference = TimeFormatPreference.Automatic,
        CultureInfo? culture = null)
    {
        if (preference == TimeFormatPreference.TwelveHour)
        {
            return FormatTwelveHour(value, includeSeconds: false, culture);
        }

        var format = preference == TimeFormatPreference.TwentyFourHour ? "HH:mm" : "t";
        return value.ToString(format, culture ?? CultureInfo.CurrentCulture);
    }

    public static string FormatTimeWithSeconds(
        DateTimeOffset value,
        TimeFormatPreference preference = TimeFormatPreference.Automatic,
        CultureInfo? culture = null)
    {
        if (preference == TimeFormatPreference.TwelveHour)
        {
            return FormatTwelveHour(value, includeSeconds: true, culture);
        }

        var format = preference == TimeFormatPreference.TwentyFourHour ? "HH:mm:ss" : "T";
        return value.ToString(format, culture ?? CultureInfo.CurrentCulture);
    }

    public static string FormatDayAndTime(
        DateTimeOffset value,
        TimeFormatPreference preference = TimeFormatPreference.Automatic,
        CultureInfo? culture = null)
    {
        var effectiveCulture = culture ?? CultureInfo.CurrentCulture;
        return $"{value.ToString("ddd", effectiveCulture)} " +
            FormatTime(value, preference, effectiveCulture);
    }

    private static string FormatTwelveHour(
        DateTimeOffset value,
        bool includeSeconds,
        CultureInfo? culture)
    {
        var effectiveCulture = culture ?? CultureInfo.CurrentCulture;
        var timePattern = includeSeconds ? "h:mm:ss" : "h:mm";
        var format = effectiveCulture.TwoLetterISOLanguageName == "zh"
            ? $"tt {timePattern}"
            : $"{timePattern} tt";
        return value.ToString(format, effectiveCulture);
    }
}
