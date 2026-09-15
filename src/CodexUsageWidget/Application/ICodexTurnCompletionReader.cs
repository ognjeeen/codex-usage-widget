namespace CodexUsageWidget.Application;

public interface ICodexTurnCompletionReader
{
    Task<bool> IsCompletedAsync(string sessionId, string turnId, CancellationToken cancellationToken);
}
