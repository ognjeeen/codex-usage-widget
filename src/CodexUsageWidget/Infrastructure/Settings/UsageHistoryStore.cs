using System.IO;
using System.Text.Json;
using CodexUsageWidget.Application;
using CodexUsageWidget.Domain;

namespace CodexUsageWidget.Infrastructure.Settings;

public sealed class UsageHistoryStore : IUsageHistoryStore
{
    private readonly string _path;

    public UsageHistoryStore(string? path = null)
    {
        _path = Path.GetFullPath(path ?? AppPaths.UsageHistoryFile);
    }

    public IReadOnlyList<UsageHistoryEntry> Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<List<UsageHistoryEntry>>(
                    File.ReadAllText(_path)) ?? []
                : [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public void Save(IReadOnlyList<UsageHistoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        try
        {
            var directory = Path.GetDirectoryName(_path)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = Path.Combine(
                directory,
                $".{Path.GetFileName(_path)}.{Guid.NewGuid():N}.tmp");
            try
            {
                using (var stream = new FileStream(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None))
                {
                    JsonSerializer.Serialize(stream, entries);
                    stream.Flush(flushToDisk: true);
                }

                if (File.Exists(_path))
                {
                    File.Replace(temporaryPath, _path, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(temporaryPath, _path);
                }
            }
            finally
            {
                TryDeleteTemporaryFile(temporaryPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
