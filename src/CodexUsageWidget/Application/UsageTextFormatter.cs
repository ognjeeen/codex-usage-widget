using System.Globalization;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Application;

public static class UsageTextFormatter
{
    public static string ToFriendlyError(string message)
    {
        if (message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("cannot find", StringComparison.OrdinalIgnoreCase))
        {
            return Strings.Get("Error_CliNotFound");
        }

        if (message.Contains("login", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return Strings.Get("Error_LoginRequired");
        }

        var detail = message.Length > 100 ? message[..100] + "…" : message;
        return Strings.Format("Error_Unexpected", detail);
    }

    public static string FormatReset(
        DateTimeOffset reset,
        DateTimeOffset? now = null,
        TimeFormatPreference timeFormatPreference = TimeFormatPreference.Automatic)
    {
        var remaining = reset - (now ?? DateTimeOffset.Now);
        if (remaining <= TimeSpan.Zero)
        {
            return Strings.Get("Usage_ResetsNow");
        }

        if (remaining < TimeSpan.FromHours(24))
        {
            return Strings.Format(
                "Usage_ResetsInHours",
                Math.Max(1, (int)Math.Ceiling(remaining.TotalHours)),
                TimeTextFormatter.FormatTime(reset, timeFormatPreference));
        }

        return Strings.Format(
            "Usage_ResetsAt",
            TimeTextFormatter.FormatDayAndTime(reset, timeFormatPreference));
    }

    public static string ColorForRemaining(double remainingPercent) => remainingPercent switch
    {
        <= 10 => "#E16D76",
        <= 25 => "#DDA56D",
        _ => "#E7E7E7"
    };
}
