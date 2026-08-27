using System.Runtime.InteropServices;

namespace CodexUsageWidget.Infrastructure.Windows;

public sealed class ExternalMouseDownWatcher : IDisposable
{
    private const int WhMouseLowLevel = 14;
    private const int WmLeftButtonDown = 0x0201;
    private const int WmRightButtonDown = 0x0204;
    private const int WmMiddleButtonDown = 0x0207;
    private const int WmXButtonDown = 0x020B;

    private readonly Action _externalMouseDown;
    private readonly MouseHookCallback _callback;
    private readonly uint _processId = (uint)Environment.ProcessId;
    private IntPtr _hook;

    public ExternalMouseDownWatcher(Action externalMouseDown)
    {
        ArgumentNullException.ThrowIfNull(externalMouseDown);

        _externalMouseDown = externalMouseDown;
        _callback = OnMouseHook;
        _hook = SetWindowsHookEx(
            WhMouseLowLevel,
            _callback,
            GetModuleHandle(null),
            0);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            _ = UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }

    private IntPtr OnMouseHook(int code, IntPtr message, IntPtr mouseData)
    {
        if (code >= 0 && IsMouseDown(message.ToInt32()))
        {
            try
            {
                var data = Marshal.PtrToStructure<LowLevelMouseInput>(mouseData);
                var target = WindowFromPoint(data.Position);
                _ = GetWindowThreadProcessId(target, out var targetProcessId);
                if (target == IntPtr.Zero || targetProcessId != _processId)
                {
                    _externalMouseDown();
                }
            }
            catch (Exception)
            {
                // Exceptions must not escape a native hook callback.
            }
        }

        return CallNextHookEx(IntPtr.Zero, code, message, mouseData);
    }

    private static bool IsMouseDown(int message) =>
        message is WmLeftButtonDown or
            WmRightButtonDown or
            WmMiddleButtonDown or
            WmXButtonDown;

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(
        int hookType,
        MouseHookCallback callback,
        IntPtr module,
        uint threadId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hook,
        int code,
        IntPtr message,
        IntPtr mouseData);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(NativePoint point);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(
        IntPtr window,
        out uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);

    private delegate IntPtr MouseHookCallback(
        int code,
        IntPtr message,
        IntPtr mouseData);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LowLevelMouseInput
    {
        public NativePoint Position;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }
}
