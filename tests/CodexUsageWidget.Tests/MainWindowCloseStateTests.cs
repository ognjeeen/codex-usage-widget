using CodexUsageWidget.Views;

namespace CodexUsageWidget.Tests;

public sealed class MainWindowCloseStateTests
{
    [Fact]
    public void OrdinaryWindowCloseMinimizesToTaskbar()
    {
        var state = new MainWindowCloseState();

        Assert.Equal(MainWindowCloseAction.MinimizeToTaskbar, state.GetCloseAction());
    }

    [Fact]
    public void ExplicitExitClosesAndShutsDownApplication()
    {
        var state = new MainWindowCloseState();

        state.RequestExplicitExit();

        Assert.Equal(MainWindowCloseAction.CloseAndShutdownApplication, state.GetCloseAction());
    }

    [Fact]
    public void WindowsSessionEndingClosesWithoutNestedApplicationShutdown()
    {
        var state = new MainWindowCloseState();

        state.NotifySessionEnding();

        Assert.Equal(MainWindowCloseAction.CloseForSessionEnding, state.GetCloseAction());
    }

    [Fact]
    public void WindowsSessionEndingTakesPrecedenceOverConcurrentExplicitExit()
    {
        var state = new MainWindowCloseState();
        state.RequestExplicitExit();

        state.NotifySessionEnding();

        Assert.Equal(MainWindowCloseAction.CloseForSessionEnding, state.GetCloseAction());
    }
}
