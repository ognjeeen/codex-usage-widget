using System.Runtime.InteropServices;
using CodexUsageWidget.Infrastructure.Settings;
using Forms = System.Windows.Forms;

namespace CodexUsageWidget.Infrastructure.Windows;

public static class TaskbarWindowInterop
{
    private const int GwlExStyle = -20;
    private const int GwlHwndParent = -8;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;
    private const uint SwpNoActivate = 0x0010;
    private const int ScreenEdgeMarginLogicalPixels = 16;
    private const string TrayNotificationWindowClass = "TrayNotifyWnd";
    private static readonly IntPtr HwndTopmost = new(-1);

    public static void ConfigureAsTaskbarOverlay(IntPtr windowHandle)
    {
        var extendedStyle = GetWindowLongPtr(windowHandle, GwlExStyle).ToInt64();
        SetWindowLongPtr(
            windowHandle,
            GwlExStyle,
            new IntPtr(extendedStyle | WsExToolWindow | WsExNoActivate));

        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar != IntPtr.Zero)
        {
            EnsureOwnedByTaskbar(windowHandle, taskbar);
        }
    }

    public static void PositionAtWorkAreaPosition(
        IntPtr windowHandle,
        double logicalWidth,
        double logicalHeight,
        IndicatorPosition position)
    {
        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero)
        {
            return;
        }

        EnsureOwnedByTaskbar(windowHandle, taskbar);

        var dpi = GetDpiForWindow(taskbar);
        var scale = dpi > 0 ? dpi / 96d : 1d;
        var width = (int)Math.Round(logicalWidth * scale);
        var height = (int)Math.Round(logicalHeight * scale);
        var margin = (int)Math.Round(ScreenEdgeMarginLogicalPixels * scale);
        var screen = Forms.Screen.FromHandle(taskbar);
        var workArea = screen.WorkingArea;
        var verticalMaximum = workArea.Bottom < screen.Bounds.Bottom
            ? workArea.Bottom
            : workArea.Bottom - height - margin;
        var location = CalculateWorkAreaPosition(
            workArea, width, height, margin, position, verticalMaximum);

        SetWindowPos(
            windowHandle,
            HwndTopmost,
            location.X,
            location.Y,
            width,
            height,
            SwpNoActivate);
    }

    internal static System.Drawing.Point CalculateWorkAreaPosition(
        System.Drawing.Rectangle workArea,
        int width,
        int height,
        int margin,
        IndicatorPosition position,
        int verticalMaximum)
    {
        var clamped = position.Clamp();
        var horizontalRange = Math.Max(0, workArea.Width - width - 2 * margin);
        var minimumTop = workArea.Top + margin;
        var verticalRange = Math.Max(0, verticalMaximum - minimumTop);
        var left = workArea.Left + margin + (int)Math.Round(
            horizontalRange * clamped.HorizontalPercent / 100d);
        var top = minimumTop + (int)Math.Round(
            verticalRange * clamped.VerticalPercent / 100d);
        return new System.Drawing.Point(left, top);
    }

    private static void EnsureOwnedByTaskbar(IntPtr windowHandle, IntPtr taskbarHandle)
    {
        if (GetWindowLongPtr(windowHandle, GwlHwndParent) != taskbarHandle)
        {
            SetWindowLongPtr(windowHandle, GwlHwndParent, taskbarHandle);
        }
    }

    internal static IntPtr FindDescendantByClass(
        IntPtr root,
        string className,
        Func<IntPtr, IntPtr, IntPtr> findNextChild,
        Func<IntPtr, string?> getClassName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(className);
        ArgumentNullException.ThrowIfNull(findNextChild);
        ArgumentNullException.ThrowIfNull(getClassName);

        var childAfter = IntPtr.Zero;
        while (true)
        {
            var child = findNextChild(root, childAfter);
            if (child == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            if (string.Equals(getClassName(child), className, StringComparison.Ordinal))
            {
                return child;
            }

            var descendant = FindDescendantByClass(
                child,
                className,
                findNextChild,
                getClassName);
            if (descendant != IntPtr.Zero)
            {
                return descendant;
            }

            childAfter = child;
        }
    }

    private static string? GetWindowClassName(IntPtr windowHandle)
    {
        var classNameBuffer = new char[256];
        var classNameLength = GetClassName(
            windowHandle,
            classNameBuffer,
            classNameBuffer.Length);
        return classNameLength == 0
            ? null
            : new string(classNameBuffer, 0, classNameLength);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(
        IntPtr parent,
        IntPtr childAfter,
        string? className,
        string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(
        IntPtr window,
        [Out] char[] className,
        int maximumCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect rectangle);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr newValue);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
