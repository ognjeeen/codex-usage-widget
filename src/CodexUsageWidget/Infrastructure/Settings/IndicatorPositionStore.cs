using System.Globalization;
using System.IO;

namespace CodexUsageWidget.Infrastructure.Settings;

public sealed class IndicatorPositionStore
{
    private readonly string _path;

    public IndicatorPositionStore(string? path = null)
    {
        _path = path ?? AppPaths.IndicatorPositionFile;
    }

    public IndicatorPosition Load()
    {
        try
        {
            var parts = File.ReadAllText(_path).Trim().Split(',', StringSplitOptions.TrimEntries);
            return parts.Length == 2 &&
                   int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var horizontal) &&
                   int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var vertical)
                ? new IndicatorPosition(horizontal, vertical).Clamp()
                : IndicatorPosition.BottomLeft;
        }
        catch (IOException)
        {
            return IndicatorPosition.BottomLeft;
        }
        catch (UnauthorizedAccessException)
        {
            return IndicatorPosition.BottomLeft;
        }
    }

    public void Save(IndicatorPosition position)
    {
        try
        {
            var clamped = position.Clamp();
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(
                _path,
                $"{clamped.HorizontalPercent.ToString(CultureInfo.InvariantCulture)},{clamped.VerticalPercent.ToString(CultureInfo.InvariantCulture)}");
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}