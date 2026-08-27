using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using CodexUsageWidget.Application;
using CodexUsageWidget.Views.ViewModels;

namespace CodexUsageWidget.Views;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "WPF owns the window lifecycle; the Closed handler disposes the cancellation source.")]
public partial class ActivityHookSetupWindow : Window
{
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(10);
    private readonly IActivityHookSetupService _setupService;
    private readonly ICodexLauncher _codexLauncher;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _refreshInProgress;
    private bool _refreshTrustOnActivation;
    private bool _reviewDialogOpen;
    private bool _closeRequested;

    public ActivityHookSetupWindow(
        IActivityHookSetupService setupService,
        ICodexLauncher codexLauncher)
    {
        ArgumentNullException.ThrowIfNull(setupService);
        ArgumentNullException.ThrowIfNull(codexLauncher);
        _setupService = setupService;
        _codexLauncher = codexLauncher;
        InitializeComponent();
        DataContext = ActivityHookSetupViewModel.Loading();
        Loaded += ActivityHookSetupWindowOnLoaded;
        Activated += ActivityHookSetupWindowOnActivated;
        Deactivated += ActivityHookSetupWindowOnDeactivated;
        Closing += ActivityHookSetupWindowOnClosing;
        Closed += ActivityHookSetupWindowOnClosed;
    }

    private async void ActivityHookSetupWindowOnLoaded(object sender, RoutedEventArgs e)
    {
        await RefreshStatusAsync();
        UpdateLayout();
        SizeChanged += ActivityHookSetupWindowOnSizeChanged;
    }

    private void ActivityHookSetupWindowOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.HeightChanged)
        {
            Top += e.PreviousSize.Height - e.NewSize.Height;
        }
    }

    private async void ActivityHookSetupWindowOnActivated(object? sender, EventArgs e)
    {
        if (_refreshTrustOnActivation && !_refreshInProgress)
        {
            _refreshTrustOnActivation = false;
            await RefreshStatusAsync();
        }
    }

    private void ActivityHookSetupWindowOnDeactivated(object? sender, EventArgs e)
    {
        if (!_reviewDialogOpen && !_refreshTrustOnActivation)
        {
            RequestClose();
        }
    }

    private void ActivityHookSetupWindowOnClosing(object? sender, CancelEventArgs e) =>
        _closeRequested = true;

    private void ActivityHookSetupWindowOnClosed(object? sender, EventArgs e)
    {
        SizeChanged -= ActivityHookSetupWindowOnSizeChanged;
        Activated -= ActivityHookSetupWindowOnActivated;
        Deactivated -= ActivityHookSetupWindowOnDeactivated;
        Closing -= ActivityHookSetupWindowOnClosing;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private async Task RefreshStatusAsync()
    {
        if (_refreshInProgress)
        {
            return;
        }

        _refreshInProgress = true;
        DataContext = ActivityHookSetupViewModel.Loading();
        InstructionText.Text = string.Empty;
        InstructionText.Visibility = Visibility.Collapsed;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            timeout.CancelAfter(StatusTimeout);
            var status = await _setupService.GetStatusAsync(timeout.Token);
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
            DataContext = ActivityHookSetupViewModel.Error(
                "Codex did not report hook status in time. Check that the CLI is available, then try again.");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            DataContext = ActivityHookSetupViewModel.Error(ex.Message);
        }
        finally
        {
            _refreshInProgress = false;
        }
    }

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e) =>
        await RefreshStatusAsync();

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => RequestClose();

    private void RequestClose()
    {
        if (_closeRequested)
        {
            return;
        }

        _closeRequested = true;
        Close();
    }

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

            var reviewWindow = new ActivityHookChangeReviewWindow(preview) { Owner = this };
            bool accepted;
            _reviewDialogOpen = true;
            try
            {
                accepted = reviewWindow.ShowDialog() == true;
            }
            finally
            {
                _reviewDialogOpen = false;
            }

            if (!accepted)
            {
                return;
            }

            _setupService.ApplyChange(preview);
            await RefreshStatusAsync();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            DataContext = ActivityHookSetupViewModel.Error(ex.Message);
        }
    }

    private void OpenCodexButton_OnClick(object sender, RoutedEventArgs e)
    {
        var commandCopied = TryCopyHooksCommand();
        _refreshTrustOnActivation = true;
        try
        {
            _codexLauncher.OpenInteractive();
            InstructionText.Text = commandCopied
                ? "Codex is open. Paste /hooks and approve the three definitions. Status refreshes when you return here."
                : "Codex is open. Type /hooks and approve the three definitions. Status refreshes when you return here.";
            InstructionText.Visibility = Visibility.Visible;
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or InvalidOperationException)
        {
            _refreshTrustOnActivation = false;
            DataContext = ActivityHookSetupViewModel.Error(ex.Message);
        }
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
