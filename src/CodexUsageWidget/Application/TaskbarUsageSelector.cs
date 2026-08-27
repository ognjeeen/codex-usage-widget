using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Application;

public static class TaskbarUsageSelector
{
    private const long FiveHourDurationMinutes = 300;
    private const long WeeklyDurationThresholdMinutes = 10_000;

    public static TaskbarLimitPreference ResolvePreference(
        UsageSnapshot snapshot,
        TaskbarLimitPreference preference) =>
        preference == TaskbarLimitPreference.FiveHour &&
        !IsAvailable(snapshot, TaskbarLimitPreference.FiveHour)
            ? TaskbarLimitPreference.Weekly
            : preference;

    public static bool IsAvailable(
        UsageSnapshot snapshot,
        TaskbarLimitPreference preference) => preference switch
        {
            TaskbarLimitPreference.FiveHour => snapshot.GeneralWindows.Any(
                window => window.WindowDurationMinutes == FiveHourDurationMinutes),
            TaskbarLimitPreference.Weekly => snapshot.GeneralWindows.Any(
                window => window.WindowDurationMinutes >= WeeklyDurationThresholdMinutes),
            TaskbarLimitPreference.MostConstrained => snapshot.MostConstrainedWindow is not null,
            _ => false
        };

    public static UsageWindow? Select(
        UsageSnapshot snapshot,
        TaskbarLimitPreference preference)
    {
        var fiveHour = snapshot.GeneralWindows.FirstOrDefault(
            window => window.WindowDurationMinutes == FiveHourDurationMinutes);
        var weekly = snapshot.GeneralWindows.FirstOrDefault(
            window => window.WindowDurationMinutes >= WeeklyDurationThresholdMinutes);

        return preference switch
        {
            TaskbarLimitPreference.FiveHour => fiveHour ?? weekly ?? snapshot.MostConstrainedWindow,
            TaskbarLimitPreference.Weekly => weekly ?? fiveHour ?? snapshot.MostConstrainedWindow,
            TaskbarLimitPreference.MostConstrained => snapshot.MostConstrainedWindow,
            _ => fiveHour ?? weekly ?? snapshot.MostConstrainedWindow
        };
    }
}
