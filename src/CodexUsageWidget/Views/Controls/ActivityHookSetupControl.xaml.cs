using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using CodexUsageWidget.Application;
using CodexUsageWidget.Localization;
using CodexUsageWidget.Views.ViewModels;

namespace CodexUsageWidget.Views.Controls;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "WPF owns the control lifecycle; the Unloaded handler disposes the cancellation source.")]
public partial class ActivityHookSetupControl : System.Windows.Controls.UserControl
{
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(10);
    private readonly IActivityHookSetupService _setupService;
    private readonly ICodexLauncher _codexLauncher;
    private readonly CancellationTokenSource _lifetime = new();
    private Window? _hostWindow;
    private ActivityHookSetupStatus? _lastStatus;
    private string? _errorMessage;
    private string? _errorResourceKey;
    private bool? _instructionCommandCopied;
    private bool _refreshInProgress;
    private bool _refreshTrustOnActivation;
    private bool _stringsSubscribed;
    private volatile bool _disposed;

    public ActivityHookSetupControl(
        IActivityHookSetupService setupService,
        ICodexLauncher codexLauncher)
    {
        ArgumentNullException.ThrowIfNull(setupService);
        ArgumentNullException.ThrowIfNull(codexLauncher);
        _setupService = setupService;
        _codexLauncher = codexLauncher;
        InitializeComponent();
        DataContext = ActivityHookSetupViewModel.Loading();
        Loaded += ActivityHookSetupControlOnLoaded;
        Unloaded += ActivityHookSetupControlOnUnloaded;
    }

    private async void ActivityHookSetupControlOnLoaded(object sender, RoutedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (!_stringsSubscribed)
        {
            Strings.Current.PropertyChanged += StringsOnPropertyChanged;
            _stringsSubscribed = true;
        }

        _hostWindow = Window.GetWindow(this);
        if (_hostWindow is not null)
        {
            _hostWindow.Activated += HostWindowOnActivated;
            _hostWindow.Closed += HostWindowOnClosed;
        }

        await RefreshStatusAsync();
    }

    private void ActivityHookSetupControlOnUnloaded(object sender, RoutedEventArgs e) =>
        DisposeControl();

    private void HostWindowOnClosed(object? sender, EventArgs e) =>
        DisposeControl();

    private void DisposeControl()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_hostWindow is not null)
        {
            _hostWindow.Activated -= HostWindowOnActivated;
            _hostWindow.Closed -= HostWindowOnClosed;
            _hostWindow = null;
        }

        if (_stringsSubscribed)
        {
            Strings.Current.PropertyChanged -= StringsOnPropertyChanged;
            _stringsSubscribed = false;
        }

        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private async void HostWindowOnActivated(object? sender, EventArgs e)
    {
        if (_refreshTrustOnActivation && !_refreshInProgress)
        {
            _refreshTrustOnActivation = false;
            await RefreshStatusAsync();
        }
    }

    private async Task RefreshStatusAsync()
    {
        if (_refreshInProgress || _disposed)
        {
            return;
        }

        _refreshInProgress = true;
        _lastStatus = null;
        _errorMessage = null;
        _errorResourceKey = null;
        _instructionCommandCopied = null;
        DataContext = ActivityHookSetupViewModel.Loading();
        InstructionText.Text = string.Empty;
        InstructionText.Visibility = Visibility.Collapsed;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            timeout.CancelAfter(StatusTimeout);
            var status = await _setupService.GetStatusAsync(timeout.Token);
            _lastStatus = status;
            var viewModel = ActivityHookSetupViewModel.FromStatus(status);
            DataContext = viewModel;
            if (_refreshTrustOnActivation && !viewModel.CanOpenCodex)
            {
                _refreshTrustOnActivation = false;
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
            SetError(resourceKey: "Activity_StatusTimeout");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            SetError(message: ex.Message);
        }
        finally
        {
            _refreshInProgress = false;
        }
    }

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e) =>
        await RefreshStatusAsync();

    private async void InstallButton_OnClick(object sender, RoutedEventArgs e) =>
        await ReviewAndApplyAsync(ActivityHookChangeKind.Install);

    private async void UninstallButton_OnClick(object sender, RoutedEventArgs e) =>
        await ReviewAndApplyAsync(ActivityHookChangeKind.Uninstall);

    private async Task ReviewAndApplyAsync(ActivityHookChangeKind kind)
    {
        try
        {
            var preview = _setupService.PrepareChange(kind);
            if (!preview.HasChanges)
            {
                await RefreshStatusAsync();
                return;
            }

            var reviewWindow = new ActivityHookChangeReviewWindow(preview)
            {
                Owner = Window.GetWindow(this)
            };
            if (reviewWindow.ShowDialog() != true)
            {
                return;
            }

            _setupService.ApplyChange(preview);
            await RefreshStatusAsync();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            SetError(message: ex.Message);
        }
    }

    private void OpenCodexButton_OnClick(object sender, RoutedEventArgs e)
    {
        var commandCopied = TryCopyHooksCommand();
        _refreshTrustOnActivation = true;
        try
        {
            _codexLauncher.OpenInteractive();
            _instructionCommandCopied = commandCopied;
            SetInstructionText(commandCopied);
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or InvalidOperationException)
        {
            _refreshTrustOnActivation = false;
            SetError(message: ex.Message);
        }
    }

    private void StringsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "Item[]" || _disposed)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
            {
                _ = Dispatcher.BeginInvoke(
                    () => StringsOnPropertyChanged(sender, e));
            }

            return;
        }

        DataContext = _lastStatus is not null
            ? ActivityHookSetupViewModel.FromStatus(_lastStatus)
            : _errorResourceKey is not null
                ? ActivityHookSetupViewModel.Error(Strings.Get(_errorResourceKey))
                : _errorMessage is not null
                    ? ActivityHookSetupViewModel.Error(
                        Strings.Format("Activity_SetupErrorDetail", _errorMessage))
                    : ActivityHookSetupViewModel.Loading();
        if (_instructionCommandCopied is { } commandCopied)
        {
            SetInstructionText(commandCopied);
        }
    }

    private void SetError(string? message = null, string? resourceKey = null)
    {
        _lastStatus = null;
        _errorMessage = message;
        _errorResourceKey = resourceKey;
        DataContext = ActivityHookSetupViewModel.Error(
            resourceKey is null
                ? Strings.Format("Activity_SetupErrorDetail", message!)
                : Strings.Get(resourceKey));
    }

    private void SetInstructionText(bool commandCopied)
    {
        InstructionText.Text = Strings.Get(
            commandCopied ? "Activity_CodexOpenCopied" : "Activity_CodexOpenType");
        InstructionText.Visibility = Visibility.Visible;
    }

    private static bool TryCopyHooksCommand()
    {
        try
        {
            System.Windows.Clipboard.SetText("/hooks");
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }
}
