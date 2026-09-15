using System.Text.Json;
using CodexUsageWidget.Infrastructure.Codex;

namespace CodexUsageWidget.Tests;

public sealed class CodexTurnCompletionReaderTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"data\":[]}")]
    [InlineData("{\"data\":[{\"id\":\"other\",\"status\":\"completed\",\"completedAt\":1789463298}]}")]
    [InlineData("{\"data\":[{\"id\":\"target-turn\",\"status\":\"completed\",\"completedAt\":\"invalid\"}]}")]
    public async Task MissingOrMalformedCompletionIsNotEvidenceOfAnIdleTurn(string json)
    {
        await using var session = new FakeSession(json);
        var reader = new CodexTurnCompletionReader(session);
        Assert.False(await reader.IsCompletedAsync("session", "target-turn", CancellationToken.None));
    }

    [Fact]
    public async Task FindsCompletedTurnOnAnOlderPage()
    {
        await using var session = new FakeSession(
            """{ "data": [{"id":"newer", "status":"inProgress"}], "nextCursor":"older" }""",
            """{ "data": [{"id":"target-turn", "status":"completed", "completedAt":1789463298}], "nextCursor":null }""");
        var reader = new CodexTurnCompletionReader(session);
        Assert.True(await reader.IsCompletedAsync("session", "target-turn", CancellationToken.None));
        Assert.Equal("older", session.LastParameters.GetProperty("cursor").GetString());
    }

    [Theory]
    [InlineData("completed", "1789463298", true)]
    [InlineData("interrupted", "1789463298", true)]
    [InlineData("failed", "1789463298", true)]
    [InlineData("interrupted", "null", false)]
    [InlineData("completed", "null", false)]
    [InlineData("inProgress", "null", false)]
    [InlineData("inProgress", "1789463298", false)]
    [InlineData("unknown", "1789463298", false)]
    public async Task RequiresExplicitCompletionForTheExactTurn(
        string status, string completedAt, bool expected)
    {
        await using var session = new FakeSession($$"""
            { "data": [
              { "id": "another-turn", "status": "completed", "completedAt": 1789463298 },
              { "id": "target-turn", "status": "{{status}}", "completedAt": {{completedAt}},
                "itemsView": "notLoaded", "items": [] }
            ], "nextCursor": null }
            """);
        var reader = new CodexTurnCompletionReader(session);

        Assert.Equal(expected, await reader.IsCompletedAsync("session", "target-turn", CancellationToken.None));
    }

    private sealed class FakeSession(params string[] results) : ICodexAppServerSession
    {
        private readonly Queue<string> _results = new(results);
        public JsonElement LastParameters { get; private set; }
        public event EventHandler<string>? NotificationReceived;
        public event EventHandler<string>? DiagnosticMessage;

        public Task<JsonElement> RequestAsync(string method, object? parameters, CancellationToken cancellationToken)
        {
            Assert.Equal("thread/turns/list", method);
            var request = JsonSerializer.SerializeToElement(parameters);
            LastParameters = request;
            Assert.Equal("session", request.GetProperty("threadId").GetString());
            Assert.Equal("notLoaded", request.GetProperty("itemsView").GetString());
            Assert.Equal("desc", request.GetProperty("sortDirection").GetString());
            using var document = JsonDocument.Parse(_results.Dequeue());
            return Task.FromResult(document.RootElement.Clone());
        }

        public ValueTask DisposeAsync()
        {
            GC.KeepAlive(NotificationReceived);
            GC.KeepAlive(DiagnosticMessage);
            return ValueTask.CompletedTask;
        }
    }
}
