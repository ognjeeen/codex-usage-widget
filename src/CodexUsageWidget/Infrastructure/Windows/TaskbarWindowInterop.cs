using System.Drawing;
using System.Runtime.InteropServices;

namespace CodexUsageWidget.Infrastructure.Windows;

public static class TaskbarWindowInterop
{
    private const int GwlExStyle = -20;
    private const long WsExTopmost = 0x00000008L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;
    private const uint GaRoot = 2;
    private const uint GwHwndPrev = 3;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private static readonly IntPtr HwndTopmost = new(-1);

    public static void ConfigureAsTaskbarOverlay(IntPtr windowHandle)
    {
        var extendedStyle = GetWindowLongPtr(windowHandle, GwlExStyle).ToInt64();
        SetWindowLongPtr(
            windowHandle,
            GwlExStyle,
            new IntPtr(extendedStyle | WsExToolWindow | WsExNoActivate));
    }

    public static void PositionNextToNotificationArea(
        IntPtr windowHandle,
        double logicalWidth,
        double logicalHeight)
    {
        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero || !GetWindowRect(taskbar, out var taskbarRect))
        {
            return;
        }

        var tray = FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
        var trayLeft = tray != IntPtr.Zero && GetWindowRect(tray, out var trayRect)
            ? trayRect.Left
            : taskbarRect.Right - 240;

        var dpi = GetDpiForWindow(taskbar);
        var scale = dpi > 0 ? dpi / 96d : 1d;
        var width = (int)Math.Round(logicalWidth * scale);
        var height = (int)Math.Round(logicalHeight * scale);
        var left = trayLeft - width;
        var top = taskbarRect.Top + Math.Max(0, (taskbarRect.Bottom - taskbarRect.Top - height) / 2);

        var insertAfter = IntPtr.Zero;
        var flags = SwpNoActivate | SwpNoOwnerZOrder | SwpNoZOrder;
        var hit = WindowFromPoint(new Point(left + width / 2, top + height / 2));
        if (GetAncestor(hit, GaRoot) == taskbar &&
            (GetWindowLongPtr(taskbar, GwlExStyle).ToInt64() & WsExTopmost) != 0)
        {
            // Explorer raised the taskbar. Insert just above it, below any open menus.
            var previous = GetWindow(taskbar, GwHwndPrev);
            if (previous != windowHandle)
            {
                insertAfter = previous == IntPtr.Zero ? HwndTopmost : previous;
                flags &= ~SwpNoZOrder;
            }
        }

        SetWindowPos(
            windowHandle,
            insertAfter,
            left,
            top,
            width,
            height,
            flags);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(
        IntPtr parent,
        IntPtr childAfter,
        string className,
        string? windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect rectangle);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(Point point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr window, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr window, uint command);

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
