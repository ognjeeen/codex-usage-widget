using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Application;

public static class DisplayedUsageSelector
{
    private const long FiveHourDurationMinutes = 300;
    private const long WeeklyDurationThresholdMinutes = 10_000;

    public static DisplayedLimitPreference ResolvePreference(
        UsageSnapshot snapshot,
        DisplayedLimitPreference preference)
    {
        if (IsAvailable(snapshot, preference))
        {
            return preference;
        }

        return preference switch
        {
            DisplayedLimitPreference.FiveHour
                when IsAvailable(snapshot, DisplayedLimitPreference.Weekly) =>
                DisplayedLimitPreference.Weekly,
            DisplayedLimitPreference.Weekly
                when IsAvailable(snapshot, DisplayedLimitPreference.FiveHour) =>
                DisplayedLimitPreference.FiveHour,
            _ when IsAvailable(snapshot, DisplayedLimitPreference.MostConstrained) =>
                DisplayedLimitPreference.MostConstrained,
            _ => preference
        };
    }

    public static bool IsAvailable(
        UsageSnapshot snapshot,
        DisplayedLimitPreference preference) => preference switch
        {
            DisplayedLimitPreference.FiveHour => snapshot.GeneralWindows.Any(
                window => window.WindowDurationMinutes == FiveHourDurationMinutes),
            DisplayedLimitPreference.Weekly => snapshot.GeneralWindows.Any(
                window => window.WindowDurationMinutes >= WeeklyDurationThresholdMinutes),
            DisplayedLimitPreference.MostConstrained => snapshot.MostConstrainedWindow is not null,
            _ => false
        };

    public static UsageWindow? Select(
        UsageSnapshot snapshot,
        DisplayedLimitPreference preference)
    {
        var fiveHour = snapshot.GeneralWindows.FirstOrDefault(
            window => window.WindowDurationMinutes == FiveHourDurationMinutes);
        var weekly = snapshot.GeneralWindows.FirstOrDefault(
            window => window.WindowDurationMinutes >= WeeklyDurationThresholdMinutes);

        return preference switch
        {
            DisplayedLimitPreference.FiveHour => fiveHour ?? weekly ?? snapshot.MostConstrainedWindow,
            DisplayedLimitPreference.Weekly => weekly ?? fiveHour ?? snapshot.MostConstrainedWindow,
            DisplayedLimitPreference.MostConstrained => snapshot.MostConstrainedWindow,
            _ => fiveHour ?? weekly ?? snapshot.MostConstrainedWindow
        };
    }
}
