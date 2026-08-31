using System.Text.Json;
using CodexUsageWidget.Application;
using CodexUsageWidget.Infrastructure.Codex;
using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class CodexRateLimitResetConsumerTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ConsumeUsesSelectedCreditAndReturnsResetOutcome()
    {
        await using var session = new RecordingSession("reset");
        var consumer = CreateConsumer(session);

        var outcome = await consumer.ConsumeAsync("reset-2");

        Assert.Equal(RateLimitResetOutcome.Reset, outcome);
        Assert.Equal("account/rateLimitResetCredit/consume", session.Method);
        var parameters = JsonSerializer.SerializeToElement(session.Parameters);
        Assert.Equal("reset-2", parameters.GetProperty("creditId").GetString());
        Assert.True(Guid.TryParse(
            parameters.GetProperty("idempotencyKey").GetString(),
            out _));
    }

    [Fact]
    public async Task RetryingAnUncertainAttemptReusesItsIdempotencyKey()
    {
        await using var session = new UncertainThenSuccessfulSession();
        var consumer = CreateConsumer(session);

        await Assert.ThrowsAsync<IOException>(() => consumer.ConsumeAsync("reset-2"));
        var outcome = await consumer.ConsumeAsync("reset-2");

        Assert.Equal(RateLimitResetOutcome.Reset, outcome);
        Assert.Equal(2, session.IdempotencyKeys.Count);
        Assert.Equal(session.IdempotencyKeys[0], session.IdempotencyKeys[1]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private CodexRateLimitResetConsumer CreateConsumer(ICodexAppServerSession session) =>
        new(
            session,
            new RateLimitResetAttemptStore(
                Path.Combine(_directory, "pending-rate-limit-reset.json")));

    private sealed class RecordingSession(string outcome) : ICodexAppServerSession
    {
        private readonly JsonElement _response = ParseAndClone($$"""{ "outcome": "{{outcome}}" }""");

        public string? Method { get; private set; }

        public object? Parameters { get; private set; }

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
            cancellationToken.ThrowIfCancellationRequested();
            Method = method;
            Parameters = parameters;
            return Task.FromResult(_response);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static JsonElement ParseAndClone(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
    }

    private sealed class UncertainThenSuccessfulSession : ICodexAppServerSession
    {
        private int _requestCount;

        public List<string> IdempotencyKeys { get; } = [];

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
            IdempotencyKeys.Add(json.GetProperty("idempotencyKey").GetString()!);
            if (_requestCount++ == 0)
            {
                return Task.FromException<JsonElement>(
                    new IOException("Response was lost after submission."));
            }

            using var document = JsonDocument.Parse("""{ "outcome": "reset" }""");
            return Task.FromResult(document.RootElement.Clone());
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
