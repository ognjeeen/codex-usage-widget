using System.IO;
using System.Text.Json;

namespace CodexUsageWidget.Infrastructure.Settings;

public sealed class RateLimitResetAttemptStore
{
    private readonly object _sync = new();
    private readonly string _path;
    private List<PersistedRateLimitResetAttempt>? _attempts;

    public RateLimitResetAttemptStore(string? path = null)
    {
        _path = Path.GetFullPath(path ?? AppPaths.PendingRateLimitResetAttemptFile);
    }

    public string GetOrCreate(string? creditId)
    {
        lock (_sync)
        {
            var attempts = GetAttempts();
            var existing = attempts.FirstOrDefault(attempt =>
                string.Equals(attempt.CreditId, creditId, StringComparison.Ordinal));
            if (existing is not null)
            {
                return existing.IdempotencyKey;
            }

            var created = new PersistedRateLimitResetAttempt(
                creditId,
                Guid.NewGuid().ToString());
            var updated = attempts.Append(created).ToList();
            Save(updated);
            _attempts = updated;
            return created.IdempotencyKey;
        }
    }

    public void Complete(string? creditId)
    {
        lock (_sync)
        {
            var attempts = GetAttempts();
            var updated = attempts.Where(attempt =>
                    !string.Equals(attempt.CreditId, creditId, StringComparison.Ordinal))
                .ToList();
            if (updated.Count != attempts.Count)
            {
                Save(updated);
                _attempts = updated;
            }
        }
    }

    private List<PersistedRateLimitResetAttempt> GetAttempts() =>
        _attempts ??= Load();

    private List<PersistedRateLimitResetAttempt> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        try
        {
            var attempts = JsonSerializer.Deserialize<List<PersistedRateLimitResetAttempt>>(
                    File.ReadAllText(_path)) ??
                throw new InvalidDataException(
                    "The pending rate-limit reset attempt file is empty.");
            if (attempts.Any(attempt =>
                    string.IsNullOrWhiteSpace(attempt.IdempotencyKey)) ||
                attempts.GroupBy(attempt => attempt.CreditId, StringComparer.Ordinal)
                    .Any(group => group.Count() > 1))
            {
                throw new InvalidDataException(
                    "The pending rate-limit reset attempt file is invalid.");
            }

            return attempts;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                "The pending rate-limit reset attempt file is invalid.",
                ex);
        }
    }

    private void Save(IReadOnlyList<PersistedRateLimitResetAttempt> attempts)
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
                JsonSerializer.Serialize(stream, attempts);
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

    private sealed record PersistedRateLimitResetAttempt(
        string? CreditId,
        string IdempotencyKey);
}
