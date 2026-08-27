using System.IO;

namespace CodexUsageWidget.Infrastructure.Settings;

public sealed class ThemePreferenceStore
{
    private readonly string _path;

    public ThemePreferenceStore(string? path = null)
    {
        _path = path ?? AppPaths.ThemePreferenceFile;
    }

    public ThemePreference Load()
    {
        try
        {
            return File.ReadAllText(_path).Trim().ToLowerInvariant() switch
            {
                "light" => ThemePreference.Light,
                "dark" => ThemePreference.Dark,
                _ => ThemePreference.System
            };
        }
        catch (IOException)
        {
            return ThemePreference.System;
        }
        catch (UnauthorizedAccessException)
        {
            return ThemePreference.System;
        }
    }

    public void Save(ThemePreference preference)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(
                _path,
                preference switch
                {
                    ThemePreference.Light => "light",
                    ThemePreference.Dark => "dark",
                    _ => "system"
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
