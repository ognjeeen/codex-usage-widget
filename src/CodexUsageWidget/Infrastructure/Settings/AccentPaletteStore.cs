using System.IO;

namespace CodexUsageWidget.Infrastructure.Settings;

public sealed class AccentPaletteStore
{
    private readonly string _path;

    public AccentPaletteStore(string? path = null)
    {
        _path = path ?? AppPaths.AccentPaletteFile;
    }

    public AccentPalette Load()
    {
        try
        {
            return File.ReadAllText(_path).Trim().ToLowerInvariant() switch
            {
                "blue" => AccentPalette.Blue,
                "violet" => AccentPalette.Violet,
                "teal" => AccentPalette.Teal,
                "emerald" => AccentPalette.Emerald,
                "pink" => AccentPalette.Pink,
                _ => AccentPalette.Emerald
            };
        }
        catch (IOException)
        {
            return AccentPalette.Emerald;
        }
        catch (UnauthorizedAccessException)
        {
            return AccentPalette.Emerald;
        }
    }

    public void Save(AccentPalette palette)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, palette.ToString().ToLowerInvariant());
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
