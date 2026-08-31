namespace CodexUsageWidget.Views.Controls;

public partial class DetailedUsageView : System.Windows.Controls.UserControl
{
    private bool _resetDetailsExpanded;

    public DetailedUsageView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => UpdateResetDetailsState();
    }

    public event EventHandler<RateLimitResetRequestedEventArgs>? ResetUseRequested;

    public void ScrollToTop() => Dispatcher.BeginInvoke(
        () =>
        {
            UpdateLayout();
            DetailsScrollViewer.ScrollToHome();
        },
        System.Windows.Threading.DispatcherPriority.ContextIdle);

    public void SetResetUsePending(bool pending)
    {
        ResetCreditsItems.IsEnabled = !pending;
        ResetDetailsButton.IsEnabled = !pending;
    }

    private void ResetDetailsButton_OnClick(
        object sender,
        System.Windows.RoutedEventArgs e)
    {
        _resetDetailsExpanded = !_resetDetailsExpanded;
        UpdateResetDetailsState();
    }

    private void UpdateResetDetailsState()
    {
        if (DataContext is not ViewModels.UsageWidgetViewModel
            {
                ResetCredits.HasSelectableCredits: true
            })
        {
            _resetDetailsExpanded = false;
        }

        ResetDetailsPanel.Visibility = _resetDetailsExpanded
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
        ResetDetailsGlyphRotation.Angle = _resetDetailsExpanded ? 180d : 0d;
        var tooltip = _resetDetailsExpanded
            ? CodexUsageWidget.Localization.Strings.Get("Usage_ResetCollapseTooltip")
            : CodexUsageWidget.Localization.Strings.Get("Usage_ResetExpandTooltip");
        ResetDetailsButton.ToolTip = tooltip;
        System.Windows.Automation.AutomationProperties.SetName(ResetDetailsButton, tooltip);
    }

    private void UseResetButton_OnClick(
        object sender,
        System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button
            {
                DataContext: ViewModels.RateLimitResetCreditViewModel credit
            })
        {
            ResetUseRequested?.Invoke(
                this,
                new RateLimitResetRequestedEventArgs(credit));
        }
    }
}
