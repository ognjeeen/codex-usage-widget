using System.IO;
using CodexUsageWidget.Application;

namespace CodexUsageWidget.Infrastructure.Settings;

public sealed class DisplayedLimitPreferenceStore
{
    private readonly string _path;

    public DisplayedLimitPreferenceStore(string? path = null)
    {
        _path = path ?? AppPaths.DisplayedLimitPreferenceFile;
    }

    public DisplayedLimitPreference Load()
    {
        try
        {
            return File.ReadAllText(_path).Trim().ToLowerInvariant() switch
            {
                "weekly" => DisplayedLimitPreference.Weekly,
                "most-constrained" => DisplayedLimitPreference.MostConstrained,
                _ => DisplayedLimitPreference.FiveHour
            };
        }
        catch (IOException)
        {
            return DisplayedLimitPreference.FiveHour;
        }
        catch (UnauthorizedAccessException)
        {
            return DisplayedLimitPreference.FiveHour;
        }
    }

    public void Save(DisplayedLimitPreference preference)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(
                _path,
                preference switch
                {
                    DisplayedLimitPreference.Weekly => "weekly",
                    DisplayedLimitPreference.MostConstrained => "most-constrained",
                    _ => "five-hour"
                });
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
