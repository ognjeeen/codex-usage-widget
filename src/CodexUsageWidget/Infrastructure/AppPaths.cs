using System.IO;

namespace CodexUsageWidget.Infrastructure;

public static class AppPaths
{
    public static string LocalDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CodexUsageWidget");

    public static string DisplayModeFile => Path.Combine(LocalDataDirectory, "display-mode.txt");

    public static string WidgetDensityFile => Path.Combine(LocalDataDirectory, "widget-density.txt");

    public static string DisplayedLimitPreferenceFile => Path.Combine(
        LocalDataDirectory,
        "displayed-limit.txt");

    public static string ThemePreferenceFile => Path.Combine(LocalDataDirectory, "theme.txt");

    public static string LanguagePreferenceFile => Path.Combine(LocalDataDirectory, "language.txt");

    public static string TimeFormatPreferenceFile => Path.Combine(
        LocalDataDirectory,
        "time-format.txt");

    public static string AccentPaletteFile => Path.Combine(LocalDataDirectory, "accent-palette.txt");

    public static string PendingRateLimitResetAttemptFile => Path.Combine(
        LocalDataDirectory,
        "pending-rate-limit-reset.json");

    public static string LogDirectory => Path.Combine(LocalDataDirectory, "logs");
}
