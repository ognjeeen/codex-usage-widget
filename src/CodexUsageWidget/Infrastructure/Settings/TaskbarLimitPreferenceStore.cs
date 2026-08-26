using System.IO;
using CodexUsageWidget.Application;

namespace CodexUsageWidget.Infrastructure.Settings;

public sealed class TaskbarLimitPreferenceStore
{
    private readonly string _path;

    public TaskbarLimitPreferenceStore(string? path = null)
    {
        _path = path ?? AppPaths.TaskbarLimitPreferenceFile;
    }

    public TaskbarLimitPreference Load()
    {
        try
        {
            return File.ReadAllText(_path).Trim().ToLowerInvariant() switch
            {
                "weekly" => TaskbarLimitPreference.Weekly,
                "most-constrained" => TaskbarLimitPreference.MostConstrained,
                _ => TaskbarLimitPreference.FiveHour
            };
        }
        catch (IOException)
        {
            return TaskbarLimitPreference.FiveHour;
        }
        catch (UnauthorizedAccessException)
        {
            return TaskbarLimitPreference.FiveHour;
        }
    }

    public void Save(TaskbarLimitPreference preference)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(
                _path,
                preference switch
                {
                    TaskbarLimitPreference.Weekly => "weekly",
                    TaskbarLimitPreference.MostConstrained => "most-constrained",
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
