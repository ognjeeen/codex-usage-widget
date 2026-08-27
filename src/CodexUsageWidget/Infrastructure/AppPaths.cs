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

    public static string LogDirectory => Path.Combine(LocalDataDirectory, "logs");
}
