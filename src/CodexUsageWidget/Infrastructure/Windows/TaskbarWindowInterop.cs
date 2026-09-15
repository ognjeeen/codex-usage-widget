using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace CodexUsageWidget.Infrastructure.Windows;

public static class TaskbarWindowInterop
{
    private const int GwlExStyle = -20;
    private const int GwlHwndParent = -8;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;
    private const uint SwpNoActivate = 0x0010;
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

    public static void PositionNextToNotificationArea(
        IntPtr windowHandle,
        double logicalWidth,
        double logicalHeight)
    {
        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero || !GetWindowRect(taskbar, out var taskbarRect))
        {
            PositionAtWorkAreaEdge(windowHandle, logicalWidth, logicalHeight);
            return;
        }

        EnsureOwnedByTaskbar(windowHandle, taskbar);

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

        SetWindowPos(
            windowHandle,
            HwndTopmost,
            left,
            top,
            width,
            height,
            SwpNoActivate);
    }

    private static void PositionAtWorkAreaEdge(
        IntPtr windowHandle,
        double logicalWidth,
        double logicalHeight)
    {
        var workingArea = Forms.Screen.FromHandle(windowHandle).WorkingArea;
        var dpi = GetDpiForWindow(windowHandle);
        var scale = dpi > 0 ? dpi / 96d : 1d;
        var width = (int)Math.Round(logicalWidth * scale);
        var height = (int)Math.Round(logicalHeight * scale);
        var edgePadding = Math.Max(4, (int)Math.Round(12d * scale));
        var bottomPadding = Math.Max(4, (int)Math.Round(8d * scale));
        var left = workingArea.Right - width - edgePadding;
        var top = workingArea.Bottom - height - bottomPadding;

        SetWindowPos(
            windowHandle,
            HwndTopmost,
            left,
            top,
            width,
            height,
            SwpNoActivate);
    }

    private static void EnsureOwnedByTaskbar(IntPtr windowHandle, IntPtr taskbarHandle)
    {
        if (GetWindowLongPtr(windowHandle, GwlHwndParent) != taskbarHandle)
        {
            SetWindowLongPtr(windowHandle, GwlHwndParent, taskbarHandle);
        }
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
