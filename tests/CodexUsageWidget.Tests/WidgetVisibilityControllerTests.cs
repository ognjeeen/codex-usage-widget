using CodexUsageWidget.Application;

namespace CodexUsageWidget.Tests;

public sealed class WidgetVisibilityControllerTests
{
    [Fact]
    public void ConsecutiveTogglesShowThenHideWidget()
    {
        var visible = false;
        var showCount = 0;
        var hideCount = 0;
        var controller = new WidgetVisibilityController(
            () => visible,
            () =>
            {
                visible = true;
                showCount++;
            },
            () =>
            {
                visible = false;
                hideCount++;
            });

        controller.Toggle();
        controller.Toggle();

        Assert.False(visible);
        Assert.Equal(1, showCount);
        Assert.Equal(1, hideCount);
    }

    [Fact]
    public void ExplicitShowCanReactivateVisibleWidget()
    {
        var showCount = 0;
        var controller = new WidgetVisibilityController(
            () => true,
            () => showCount++,
            () => { });

        controller.Show();

        Assert.Equal(1, showCount);
    }

    [Fact]
    public void TaskbarInteractionSuppressesDeactivationBeforeToggle()
    {
        var visible = true;
        var showCount = 0;
        var hideCount = 0;
        var controller = new WidgetVisibilityController(
            () => visible,
            () =>
            {
                visible = true;
                showCount++;
            },
            () =>
            {
                visible = false;
                hideCount++;
            });

        controller.HideOnDeactivated(taskbarInteractionInProgress: true);
        controller.Toggle();

        Assert.False(visible);
        Assert.Equal(0, showCount);
        Assert.Equal(1, hideCount);
    }

    [Fact]
    public void OwnedDialogSuppressesDeactivation()
    {
        var visible = true;
        var hideCount = 0;
        var controller = new WidgetVisibilityController(
            () => visible,
            () => visible = true,
            () =>
            {
                visible = false;
                hideCount++;
            });

        controller.HideOnDeactivated(
            taskbarInteractionInProgress: false,
            ownedDialogOpen: true);

        Assert.True(visible);
        Assert.Equal(0, hideCount);
    }
}
