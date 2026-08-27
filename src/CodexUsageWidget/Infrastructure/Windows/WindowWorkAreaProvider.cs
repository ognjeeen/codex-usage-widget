using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace CodexUsageWidget.Infrastructure.Windows;

public interface IWindowWorkAreaProvider
{
    double GetAvailableHeightInDips(Window referenceWindow);
}

public sealed class WindowWorkAreaProvider : IWindowWorkAreaProvider
{
    private const double WindowMargin = 24d;

    public double GetAvailableHeightInDips(Window referenceWindow)
    {
        ArgumentNullException.ThrowIfNull(referenceWindow);

        var handle = new WindowInteropHelper(referenceWindow).EnsureHandle();
        var workArea = Forms.Screen.FromHandle(handle).WorkingArea;
        var dpiScale = VisualTreeHelper.GetDpi(referenceWindow).DpiScaleY;
        return Math.Max(0d, workArea.Height / dpiScale - WindowMargin);
    }
}
