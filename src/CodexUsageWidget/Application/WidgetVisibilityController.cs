namespace CodexUsageWidget.Application;

public sealed class WidgetVisibilityController
{
    private readonly Func<bool> _isVisible;
    private readonly Action _show;
    private readonly Action _hide;

    public WidgetVisibilityController(Func<bool> isVisible, Action show, Action hide)
    {
        _isVisible = isVisible;
        _show = show;
        _hide = hide;
    }

    public void Show() => _show();

    public void HideOnDeactivated(
        bool taskbarInteractionInProgress,
        bool ownedDialogOpen = false)
    {
        if (!taskbarInteractionInProgress && !ownedDialogOpen)
        {
            _hide();
        }
    }

    public void Toggle()
    {
        if (_isVisible())
        {
            _hide();
            return;
        }

        _show();
    }
}
