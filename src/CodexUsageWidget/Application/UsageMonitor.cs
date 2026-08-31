using CodexUsageWidget.Domain;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Application;

public sealed class UsageMonitor : IAsyncDisposable
{
    private readonly IUsageProvider _provider;
    private readonly TimeSpan _refreshInterval;
    private readonly TimeSpan _requestTimeout;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private Task? _periodicRefreshTask;
    private bool _started;

    public UsageMonitor(
        IUsageProvider provider,
        TimeSpan? refreshInterval = null,
        TimeSpan? requestTimeout = null)
    {
        _provider = provider;
        _refreshInterval = refreshInterval ?? TimeSpan.FromMinutes(2);
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(12);
    }

    public event Action? RefreshStarted;

    public event Action<UsageSnapshot>? SnapshotUpdated;

    public event Action<string>? RefreshFailed;

    public event EventHandler<string>? DiagnosticMessage
    {
        add => _provider.DiagnosticMessage += value;
        remove => _provider.DiagnosticMessage -= value;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _provider.RateLimitsChanged += ProviderOnRateLimitsChanged;
        _periodicRefreshTask = RunPeriodicRefreshAsync(_lifetime.Token);
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!await _refreshGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await RefreshWithGateHeldAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RefreshAfterCurrentAsync(CancellationToken cancellationToken = default)
    {
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        await RefreshWithGateHeldAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RefreshWithGateHeldAsync(CancellationToken cancellationToken)
    {
        try
        {
            RefreshStarted?.Invoke();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetime.Token);
            timeout.CancelAfter(_requestTimeout);

            var snapshot = await _provider.ReadUsageAsync(timeout.Token).ConfigureAwait(false);
            SnapshotUpdated?.Invoke(snapshot);
        }
        catch (OperationCanceledException) when (!_lifetime.IsCancellationRequested)
        {
            RefreshFailed?.Invoke(Strings.Get("Error_ResponseTimeout"));
        }
        catch (Exception ex)
        {
            RefreshFailed?.Invoke(UsageTextFormatter.ToFriendlyError(ex.Message));
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private void ProviderOnRateLimitsChanged(object? sender, EventArgs e) =>
        _ = RefreshAsync(_lifetime.Token);

    private async Task RunPeriodicRefreshAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_refreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await RefreshAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        _provider.RateLimitsChanged -= ProviderOnRateLimitsChanged;
        await _lifetime.CancelAsync().ConfigureAwait(false);

        if (_periodicRefreshTask is not null)
        {
            try
            {
                await _periodicRefreshTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        await _provider.DisposeAsync().ConfigureAwait(false);
        _refreshGate.Dispose();
        _lifetime.Dispose();
    }
}
