using System.IO;

namespace CodexUsageWidget.Infrastructure.Settings;

public sealed class LanguagePreferenceStore
{
    private readonly string _path;

    public LanguagePreferenceStore(string? path = null)
    {
        _path = path ?? AppPaths.LanguagePreferenceFile;
    }

    public LanguagePreference Load()
    {
        try
        {
            return File.ReadAllText(_path).Trim().ToLowerInvariant() switch
            {
                "english" => LanguagePreference.English,
                "simplified-chinese" => LanguagePreference.SimplifiedChinese,
                _ => LanguagePreference.System
            };
        }
        catch (IOException)
        {
            return LanguagePreference.System;
        }
        catch (UnauthorizedAccessException)
        {
            return LanguagePreference.System;
        }
    }

    public void Save(LanguagePreference preference)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(
                _path,
                preference switch
                {
                    LanguagePreference.English => "english",
                    LanguagePreference.SimplifiedChinese => "simplified-chinese",
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
