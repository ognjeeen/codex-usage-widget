namespace CodexUsageWidget.Application;

public sealed class CodexActivityMonitor : IAsyncDisposable
{
    private readonly object _stateLock = new();
    private readonly object _transitionLock = new();
    private readonly ICodexActivitySignalSource _source;
    private readonly ICodexTurnCompletionReader? _completionReader;
    private readonly TimeSpan _reconciliationInterval;
    private readonly TimeSpan _requestTimeout;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _reconciliationGate = new(1, 1);
    private Task? _reconciliationTask;
    private int _disposed;
    private bool _completionCheckFailed;
    private readonly Dictionary<string, string> _activeTurnsBySession =
        new(StringComparer.Ordinal);
    private bool _started;

    public CodexActivityMonitor(
        ICodexActivitySignalSource source,
        ICodexTurnCompletionReader? completionReader = null,
        TimeSpan? reconciliationInterval = null,
        TimeSpan? requestTimeout = null)
    {
        _source = source;
        _completionReader = completionReader;
        _reconciliationInterval = reconciliationInterval ?? TimeSpan.FromSeconds(15);
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(5);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_reconciliationInterval, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_requestTimeout, TimeSpan.Zero);
    }

    public async Task ReconcileAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);
        if (_completionReader is null ||
            !await _reconciliationGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            await ReconcileWithGateHeldAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _reconciliationGate.Release();
        }
    }

    private async Task ReconcileWithGateHeldAsync(CancellationToken cancellationToken)
    {
        KeyValuePair<string, string>[] turns;
        lock (_stateLock)
        {
            turns = _activeTurnsBySession.ToArray();
        }

        var checkFailed = false;
        foreach (var turn in turns)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, _lifetime.Token);
                timeout.CancelAfter(_requestTimeout);
                if (await _completionReader!.IsCompletedAsync(turn.Key, turn.Value, timeout.Token)
                        .ConfigureAwait(false))
                {
                    timeout.Token.ThrowIfCancellationRequested();
                    SourceOnSignalReceived(new(CodexActivitySignalKind.TurnStopped, turn.Key, turn.Value));
                }
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested || _lifetime.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                checkFailed = true;
            }
        }

        if (checkFailed && !_completionCheckFailed)
        {
            DiagnosticMessage?.Invoke(this,
                "Codex activity completion check unavailable; keeping hook activity until completion is confirmed.");
        }

        _completionCheckFailed = checkFailed;
    }

    public event Action<bool>? ActivityChanged;

    public event EventHandler<string>? DiagnosticMessage;

    public bool IsActive
    {
        get
        {
            lock (_stateLock)
            {
                return _activeTurnsBySession.Count > 0;
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);
        if (_started)
        {
            return;
        }

        _started = true;
        _source.SignalReceived += SourceOnSignalReceived;
        await _source.StartAsync(cancellationToken).ConfigureAwait(false);
        if (_completionReader is not null)
        {
            _reconciliationTask = RunReconciliationAsync(_lifetime.Token);
        }
    }

    private async Task RunReconciliationAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_reconciliationInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await ReconcileAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void SourceOnSignalReceived(CodexActivitySignal signal)
    {
        lock (_transitionLock)
        {
            bool? changedState = null;
            var recoveredOrphan = false;
            lock (_stateLock)
            {
                var wasActive = _activeTurnsBySession.Count > 0;
                switch (signal.Kind)
                {
                    case CodexActivitySignalKind.TurnStarted when signal.TurnId is not null:
                        recoveredOrphan = _activeTurnsBySession.TryGetValue(
                            signal.SessionId,
                            out var previousTurnId) &&
                            !string.Equals(previousTurnId, signal.TurnId, StringComparison.Ordinal);
                        _activeTurnsBySession[signal.SessionId] = signal.TurnId;
                        break;
                    case CodexActivitySignalKind.TurnStopped when signal.TurnId is not null:
                        if (_activeTurnsBySession.TryGetValue(signal.SessionId, out var activeTurnId) &&
                            string.Equals(activeTurnId, signal.TurnId, StringComparison.Ordinal))
                        {
                            _activeTurnsBySession.Remove(signal.SessionId);
                        }

                        break;
                    case CodexActivitySignalKind.SessionEnded:
                        _activeTurnsBySession.Remove(signal.SessionId);
                        break;
                }

                var currentActivity = _activeTurnsBySession.Count > 0;
                if (wasActive != currentActivity)
                {
                    changedState = currentActivity;
                }
            }

            if (recoveredOrphan)
            {
                DiagnosticMessage?.Invoke(
                    this,
                    "Recovered stale Codex activity state for one session.");
            }

            if (changedState is { } emittedActivity)
            {
                ActivityChanged?.Invoke(emittedActivity);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _source.SignalReceived -= SourceOnSignalReceived;
        await _lifetime.CancelAsync().ConfigureAwait(false);
        if (_reconciliationTask is not null)
        {
            await _reconciliationTask.ConfigureAwait(false);
        }

        await _reconciliationGate.WaitAsync().ConfigureAwait(false);
        _reconciliationGate.Release();
        await _source.DisposeAsync().ConfigureAwait(false);
        _lifetime.Dispose();
        _reconciliationGate.Dispose();
    }
}
