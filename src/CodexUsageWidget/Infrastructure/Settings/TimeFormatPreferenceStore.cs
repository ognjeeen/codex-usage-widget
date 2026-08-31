using System.IO;
using CodexUsageWidget.Application;

namespace CodexUsageWidget.Infrastructure.Settings;

public sealed class TimeFormatPreferenceStore
{
    private readonly string _path;

    public TimeFormatPreferenceStore(string? path = null)
    {
        _path = path ?? AppPaths.TimeFormatPreferenceFile;
    }

    public TimeFormatPreference Load()
    {
        try
        {
            return File.ReadAllText(_path).Trim().ToLowerInvariant() switch
            {
                "24-hour" => TimeFormatPreference.TwentyFourHour,
                "12-hour" => TimeFormatPreference.TwelveHour,
                _ => TimeFormatPreference.Automatic
            };
        }
        catch (IOException)
        {
            return TimeFormatPreference.Automatic;
        }
        catch (UnauthorizedAccessException)
        {
            return TimeFormatPreference.Automatic;
        }
    }

    public void Save(TimeFormatPreference preference)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(
                _path,
                preference switch
                {
                    TimeFormatPreference.TwentyFourHour => "24-hour",
                    TimeFormatPreference.TwelveHour => "12-hour",
                    _ => "automatic"
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
