using CodexUsageWidget.Application;

namespace CodexUsageWidget.Tests;

public sealed class CodexActivityMonitorTests
{
    [Fact]
    public async Task CompletionOfOneSessionDoesNotHideAnotherUnconfirmedSession()
    {
        await using var source = new FakeSignalSource();
        var reader = new FakeCompletionReader((session, _, _) => Task.FromResult(session == "finished"));
        await using var monitor = new CodexActivityMonitor(source, reader);
        await monitor.StartAsync();
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "finished", "turn-a"));
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "running", "turn-b"));

        await monitor.ReconcileAsync();
        await monitor.ReconcileAsync();
        Assert.True(monitor.IsActive);
        source.Publish(new(CodexActivitySignalKind.TurnStopped, "running", "turn-b"));
        Assert.False(monitor.IsActive);
    }

    [Fact]
    public async Task LateCompletionCheckCannotClearANewerTurn()
    {
        await using var source = new FakeSignalSource();
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = new FakeCompletionReader((_, _, _) => result.Task);
        await using var monitor = new CodexActivityMonitor(source, reader);
        await monitor.StartAsync();
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session", "old"));
        var reconciliation = monitor.ReconcileAsync();
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session", "new"));
        result.SetResult(true);

        await reconciliation;
        Assert.True(monitor.IsActive);
        source.Publish(new(CodexActivitySignalKind.TurnStopped, "session", "new"));
        Assert.False(monitor.IsActive);
    }

    [Fact]
    public async Task TimedOutCheckKeepsActivityAndRetriesLater()
    {
        await using var source = new FakeSignalSource();
        var attempts = 0;
        var reader = new FakeCompletionReader(async (_, _, token) =>
        {
            if (++attempts == 1)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }

            return true;
        });
        await using var monitor = new CodexActivityMonitor(source, reader,
            requestTimeout: TimeSpan.FromMilliseconds(30));
        await monitor.StartAsync();
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session", "turn"));

        await monitor.ReconcileAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(monitor.IsActive);
        await monitor.ReconcileAsync();
        Assert.False(monitor.IsActive);
    }

    [Fact]
    public async Task DisposalCancelsPendingCheckWithoutClearingActivity()
    {
        await using var source = new FakeSignalSource();
        var reader = new FakeCompletionReader(async (_, _, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return true;
        });
        await using var monitor = new CodexActivityMonitor(source, reader);
        await monitor.StartAsync();
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session", "turn"));
        var reconciliation = monitor.ReconcileAsync();

        await monitor.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reconciliation);
        Assert.True(monitor.IsActive);
    }

    [Fact]
    public async Task IdleMonitorDoesNotQueryCompletion()
    {
        await using var source = new FakeSignalSource();
        var reads = 0;
        var reader = new FakeCompletionReader((_, _, _) =>
        {
            reads++;
            return Task.FromResult(false);
        });
        await using var monitor = new CodexActivityMonitor(source, reader);
        await monitor.StartAsync();
        await monitor.ReconcileAsync();
        Assert.Equal(0, reads);
    }

    [Fact]
    public async Task PeriodicCheckRecoversMissingStopWithoutAnotherHook()
    {
        await using var source = new FakeSignalSource();
        var reader = new FakeCompletionReader((_, _, _) => Task.FromResult(true));
        await using var monitor = new CodexActivityMonitor(source, reader, TimeSpan.FromMilliseconds(20));
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        monitor.ActivityChanged += active =>
        {
            if (!active)
            {
                stopped.TrySetResult();
            }
        };
        await monitor.StartAsync();
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session", "turn"));

        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(monitor.IsActive);
    }

    [Fact]
    public async Task FailedCompletionCheckPreservesActivityAndCanRecoverOnRetry()
    {
        await using var source = new FakeSignalSource();
        var attempts = 0;
        var reader = new FakeCompletionReader((_, _, _) => ++attempts == 1
            ? Task.FromException<bool>(new InvalidOperationException("Unavailable"))
            : Task.FromResult(true));
        await using var monitor = new CodexActivityMonitor(source, reader);
        await monitor.StartAsync();
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session", "turn"));

        await monitor.ReconcileAsync();
        Assert.True(monitor.IsActive);
        await monitor.ReconcileAsync();
        Assert.False(monitor.IsActive);
    }

    [Fact]
    public async Task ConfirmedCompletionRecoversMissingStopAfterAnotherSessionFinishes()
    {
        await using var source = new FakeSignalSource();
        var reader = new FakeCompletionReader((_, _, _) => Task.FromResult(true));
        await using var monitor = new CodexActivityMonitor(source, reader);
        var changes = new List<bool>();
        monitor.ActivityChanged += changes.Add;
        await monitor.StartAsync();

        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session-a", "turn-a"));
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session-b", "turn-b"));
        source.Publish(new(CodexActivitySignalKind.TurnStopped, "session-b", "turn-b"));
        Assert.True(monitor.IsActive);

        await monitor.ReconcileAsync();

        Assert.False(monitor.IsActive);
        Assert.Equal([true, false], changes);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task MissingStopLeavesActivityActiveAfterAnotherSessionFinishes(
        bool deliverFirstStop,
        bool expectedActivity)
    {
        await using var source = new FakeSignalSource();
        await using var monitor = new CodexActivityMonitor(source);
        var changes = new List<bool>();
        monitor.ActivityChanged += changes.Add;
        await monitor.StartAsync();

        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session-a", "turn-a"));

        // Session A has finished externally. Simulate either delivery or loss of its Stop.
        // No SessionEnd is delivered, as the Codex session can remain open between turns.
        if (deliverFirstStop)
        {
            source.Publish(new(CodexActivitySignalKind.TurnStopped, "session-a", "turn-a"));
        }

        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session-b", "turn-b"));
        source.Publish(new(CodexActivitySignalKind.TurnStopped, "session-b", "turn-b"));
        source.Publish(new(CodexActivitySignalKind.SessionEnded, "session-b"));

        // Characterizes the current limitation, rather than asserting recovery exists.
        Assert.Equal(expectedActivity, monitor.IsActive);
        if (deliverFirstStop)
        {
            Assert.Equal([true, false, true, false], changes);
        }
        else
        {
            Assert.Equal([true], changes);
        }

        // A final event for the stranded session clears the activity immediately.
        source.Publish(new(CodexActivitySignalKind.SessionEnded, "session-a"));
        Assert.False(monitor.IsActive);
        Assert.False(changes[^1]);
    }

    [Fact]
    public async Task FirstStartEnablesActivityAndDuplicateStartIsIdempotent()
    {
        await using var source = new FakeSignalSource();
        await using var monitor = new CodexActivityMonitor(source);
        var changes = new List<bool>();
        monitor.ActivityChanged += changes.Add;
        await monitor.StartAsync();

        var start = new CodexActivitySignal(CodexActivitySignalKind.TurnStarted, "session", "turn");
        source.Publish(start);
        source.Publish(start);

        Assert.True(monitor.IsActive);
        Assert.Equal([true], changes);
    }

    [Fact]
    public async Task ParallelTurnsStayActiveUntilBothStop()
    {
        await using var source = new FakeSignalSource();
        await using var monitor = new CodexActivityMonitor(source);
        var changes = new List<bool>();
        monitor.ActivityChanged += changes.Add;
        await monitor.StartAsync();

        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session-a", "turn-a"));
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session-b", "turn-b"));
        source.Publish(new(CodexActivitySignalKind.TurnStopped, "session-a", "turn-a"));

        Assert.True(monitor.IsActive);
        Assert.Equal([true], changes);

        source.Publish(new(CodexActivitySignalKind.TurnStopped, "session-b", "turn-b"));

        Assert.False(monitor.IsActive);
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public async Task UnknownStopIsNoOp()
    {
        await using var source = new FakeSignalSource();
        await using var monitor = new CodexActivityMonitor(source);
        var changes = new List<bool>();
        monitor.ActivityChanged += changes.Add;
        await monitor.StartAsync();

        source.Publish(new(CodexActivitySignalKind.TurnStopped, "unknown", "unknown"));

        Assert.False(monitor.IsActive);
        Assert.Empty(changes);
    }

    [Fact]
    public async Task NewTurnInSameSessionReplacesOrphanedTurn()
    {
        await using var source = new FakeSignalSource();
        await using var monitor = new CodexActivityMonitor(source);
        var changes = new List<bool>();
        string? diagnostic = null;
        monitor.ActivityChanged += changes.Add;
        monitor.DiagnosticMessage += (_, message) => diagnostic = message;
        await monitor.StartAsync();

        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session", "aborted-turn"));
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session", "next-turn"));
        source.Publish(new(CodexActivitySignalKind.TurnStopped, "session", "next-turn"));

        Assert.False(monitor.IsActive);
        Assert.Equal([true, false], changes);
        Assert.Equal("Recovered stale Codex activity state for one session.", diagnostic);
    }

    [Fact]
    public async Task LateStopForReplacedTurnCannotStopCurrentTurn()
    {
        await using var source = new FakeSignalSource();
        await using var monitor = new CodexActivityMonitor(source);
        var changes = new List<bool>();
        monitor.ActivityChanged += changes.Add;
        await monitor.StartAsync();

        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session", "old-turn"));
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session", "current-turn"));
        source.Publish(new(CodexActivitySignalKind.TurnStopped, "session", "old-turn"));

        Assert.True(monitor.IsActive);
        Assert.Equal([true], changes);

        source.Publish(new(CodexActivitySignalKind.TurnStopped, "session", "current-turn"));

        Assert.False(monitor.IsActive);
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public async Task SessionEndRemovesOnlyTurnsFromThatSession()
    {
        await using var source = new FakeSignalSource();
        await using var monitor = new CodexActivityMonitor(source);
        var changes = new List<bool>();
        monitor.ActivityChanged += changes.Add;
        await monitor.StartAsync();

        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session-a", "turn-a1"));
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session-a", "turn-a2"));
        source.Publish(new(CodexActivitySignalKind.TurnStarted, "session-b", "turn-b"));
        source.Publish(new(CodexActivitySignalKind.SessionEnded, "session-a"));

        Assert.True(monitor.IsActive);
        Assert.Equal([true], changes);

        source.Publish(new(CodexActivitySignalKind.SessionEnded, "session-b"));

        Assert.False(monitor.IsActive);
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public async Task ActivityNotificationsAreSerializedWithStateTransitions()
    {
        await using var source = new FakeSignalSource();
        await using var monitor = new CodexActivityMonitor(source);
        var changes = new List<bool>();
        var startNotificationEntered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var stopNotificationEntered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseStartNotification = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        monitor.ActivityChanged += isActive =>
        {
            lock (changes)
            {
                changes.Add(isActive);
            }

            if (isActive)
            {
                startNotificationEntered.TrySetResult();
                releaseStartNotification.Task.GetAwaiter().GetResult();
            }
            else
            {
                stopNotificationEntered.TrySetResult();
            }
        };
        await monitor.StartAsync();

        var startPublish = Task.Run(() =>
            source.Publish(new(CodexActivitySignalKind.TurnStarted, "session", "turn")));
        await startNotificationEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var stopPublish = Task.Run(() =>
            source.Publish(new(CodexActivitySignalKind.TurnStopped, "session", "turn")));

        await Assert.ThrowsAsync<TimeoutException>(() =>
            stopNotificationEntered.Task.WaitAsync(TimeSpan.FromMilliseconds(100)));
        releaseStartNotification.TrySetResult();
        await Task.WhenAll(startPublish, stopPublish).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal([true, false], changes);
        Assert.False(monitor.IsActive);
    }

    private sealed class FakeCompletionReader(
        Func<string, string, CancellationToken, Task<bool>> read) : ICodexTurnCompletionReader
    {
        public Task<bool> IsCompletedAsync(
            string sessionId, string turnId, CancellationToken cancellationToken) =>
            read(sessionId, turnId, cancellationToken);
    }

    private sealed class FakeSignalSource : ICodexActivitySignalSource
    {
        public event Action<CodexActivitySignal>? SignalReceived;

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Publish(CodexActivitySignal signal) => SignalReceived?.Invoke(signal);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
