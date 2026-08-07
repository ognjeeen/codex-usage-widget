namespace CodexUsageWidget.Views;

public enum MainWindowCloseAction
{
    MinimizeToTaskbar,
    CloseAndShutdownApplication,
    CloseForSessionEnding
}

public sealed class MainWindowCloseState
{
    private MainWindowCloseAction _action = MainWindowCloseAction.MinimizeToTaskbar;

    public MainWindowCloseAction GetCloseAction() => _action;

    public void RequestExplicitExit() =>
        _action = MainWindowCloseAction.CloseAndShutdownApplication;

    public void NotifySessionEnding() =>
        _action = MainWindowCloseAction.CloseForSessionEnding;
}
