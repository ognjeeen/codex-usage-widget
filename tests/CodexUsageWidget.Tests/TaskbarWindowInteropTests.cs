using CodexUsageWidget.Infrastructure.Windows;

namespace CodexUsageWidget.Tests;

public sealed class TaskbarWindowInteropTests
{
    [Fact]
    public void FindsNotificationAreaWhenItIsNestedBelowTheTaskbar()
    {
        var taskbar = new IntPtr(1);
        var intermediateContainer = new IntPtr(2);
        var notificationArea = new IntPtr(3);
        var children = new Dictionary<IntPtr, IntPtr[]>
        {
            [taskbar] = [intermediateContainer],
            [intermediateContainer] = [notificationArea]
        };
        var classNames = new Dictionary<IntPtr, string>
        {
            [intermediateContainer] = "TaskbarFrame",
            [notificationArea] = "TrayNotifyWnd"
        };

        var result = TaskbarWindowInterop.FindDescendantByClass(
            taskbar,
            "TrayNotifyWnd",
            (parent, childAfter) => FindNextChild(children, parent, childAfter),
            window => classNames.TryGetValue(window, out var className) ? className : null);

        Assert.Equal(notificationArea, result);
    }

    [Fact]
    public void ReturnsZeroWhenNotificationAreaIsNotPresent()
    {
        var taskbar = new IntPtr(1);
        var child = new IntPtr(2);
        var children = new Dictionary<IntPtr, IntPtr[]>
        {
            [taskbar] = [child]
        };

        var result = TaskbarWindowInterop.FindDescendantByClass(
            taskbar,
            "TrayNotifyWnd",
            (parent, childAfter) => FindNextChild(children, parent, childAfter),
            _ => "TaskbarFrame");

        Assert.Equal(IntPtr.Zero, result);
    }

    private static IntPtr FindNextChild(
        Dictionary<IntPtr, IntPtr[]> children,
        IntPtr parent,
        IntPtr childAfter)
    {
        if (!children.TryGetValue(parent, out var siblings))
        {
            return IntPtr.Zero;
        }

        var startIndex = 0;
        if (childAfter != IntPtr.Zero)
        {
            var childIndex = Array.IndexOf(siblings, childAfter);
            startIndex = childIndex + 1;
        }

        return startIndex < siblings.Length ? siblings[startIndex] : IntPtr.Zero;
    }
}
