using System.Text.Json;
using CodexUsageWidget.Application;

namespace CodexUsageWidget.Infrastructure.Codex;

public sealed class CodexTurnCompletionReader(ICodexAppServerSession session) : ICodexTurnCompletionReader
{
    public async Task<bool> IsCompletedAsync(
        string sessionId, string turnId, CancellationToken cancellationToken)
    {
        string? cursor = null;
        var seenCursors = new HashSet<string>(StringComparer.Ordinal);
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await session.RequestAsync(
                "thread/turns/list",
                new { threadId = sessionId, cursor, limit = 100, sortDirection = "desc", itemsView = "notLoaded" },
                cancellationToken).ConfigureAwait(false);
            if (result.ValueKind != JsonValueKind.Object ||
                !result.TryGetProperty("data", out var turns) || turns.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var turn in turns.EnumerateArray())
            {
                if (turn.ValueKind == JsonValueKind.Object &&
                    turn.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String &&
                    string.Equals(id.GetString(), turnId, StringComparison.Ordinal))
                {
                    // A foreign running turn can be reconstructed as interrupted without an end time.
                    // Never infer completion from that status alone, or from an absent turn.
                    return turn.TryGetProperty("status", out var status) &&
                        status.ValueKind == JsonValueKind.String &&
                        status.GetString() is "completed" or "interrupted" or "failed" &&
                        turn.TryGetProperty("completedAt", out var completedAt) &&
                        completedAt.ValueKind == JsonValueKind.Number &&
                        completedAt.TryGetInt64(out var timestamp) && timestamp > 0;
                }
            }

            cursor = result.TryGetProperty("nextCursor", out var next) && next.ValueKind == JsonValueKind.String
                ? next.GetString()
                : null;
        }
        while (!string.IsNullOrEmpty(cursor) && seenCursors.Add(cursor));

        return false;
    }
}
