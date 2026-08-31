using System.Text.Json;
using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure.Codex;
using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class RateLimitResetAttemptPersistenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task AnonymousRetryAfterRestartReusesTheUncertainAttemptKey()
    {
        var path = Path.Combine(_directory, "pending-rate-limit-reset.json");
        await using var uncertainSession = new RecordingSession(
            new IOException("Response was lost after submission."));
        var firstConsumer = new CodexRateLimitResetConsumer(
            uncertainSession,
            new RateLimitResetAttemptStore(path));

        await Assert.ThrowsAsync<IOException>(() => firstConsumer.ConsumeAsync(null));

        await using var successfulSession = new RecordingSession("reset");
        var restartedConsumer = new CodexRateLimitResetConsumer(
            successfulSession,
            new RateLimitResetAttemptStore(path));
        var outcome = await restartedConsumer.ConsumeAsync(null);

        Assert.Equal(RateLimitResetOutcome.Reset, outcome);
        Assert.Equal(uncertainSession.IdempotencyKey, successfulSession.IdempotencyKey);
        Assert.False(successfulSession.IncludedCreditId);
    }

    [Fact]
    public void FailedPersistenceBlocksEveryAttemptUntilTheKeyCanBeStored()
    {
        Directory.CreateDirectory(_directory);
        var blockingFile = Path.Combine(_directory, "not-a-directory");
        File.WriteAllText(blockingFile, "occupied");
        var store = new RateLimitResetAttemptStore(
            Path.Combine(blockingFile, "pending-rate-limit-reset.json"));

        Assert.Throws<IOException>(() => store.GetOrCreate(creditId: null));
        Assert.Throws<IOException>(() => store.GetOrCreate(creditId: null));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class RecordingSession : ICodexAppServerSession
    {
        private readonly Exception? _exception;
        private readonly JsonElement _response;

        public RecordingSession(Exception exception)
        {
            _exception = exception;
        }

        public RecordingSession(string outcome)
        {
            using var document = JsonDocument.Parse(
                $$"""{ "outcome": "{{outcome}}" }""");
            _response = document.RootElement.Clone();
        }

        public string? IdempotencyKey { get; private set; }

        public bool IncludedCreditId { get; private set; }

        public event EventHandler<string>? NotificationReceived
        {
            add { }
            remove { }
        }

        public event EventHandler<string>? DiagnosticMessage
        {
            add { }
            remove { }
        }

        public Task<JsonElement> RequestAsync(
            string method,
            object? parameters,
            CancellationToken cancellationToken)
        {
            _ = method;
            cancellationToken.ThrowIfCancellationRequested();
            var json = JsonSerializer.SerializeToElement(parameters);
            IdempotencyKey = json.GetProperty("idempotencyKey").GetString();
            IncludedCreditId = json.TryGetProperty("creditId", out _);
            return _exception is null
                ? Task.FromResult(_response)
                : Task.FromException<JsonElement>(_exception);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
