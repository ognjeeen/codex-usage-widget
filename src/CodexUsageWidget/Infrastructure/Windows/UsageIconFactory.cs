using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace CodexUsageWidget.Infrastructure.Windows;

internal static class UsageIconFactory
{
    public static System.Drawing.Icon Create(double? remainingPercent)
    {
        using var bitmap = new System.Drawing.Bitmap(64, 64);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        var indicatorColor = remainingPercent switch
        {
            null => System.Drawing.Color.FromArgb(151, 163, 182),
            <= 10 => System.Drawing.Color.FromArgb(240, 112, 112),
            <= 25 => System.Drawing.Color.FromArgb(240, 179, 94),
            _ => System.Drawing.Color.FromArgb(101, 216, 146)
        };

        using var backgroundBrush = new System.Drawing.SolidBrush(
            System.Drawing.Color.FromArgb(22, 29, 39));
        graphics.FillEllipse(backgroundBrush, 1, 1, 62, 62);

        DrawTerminalPrompt(graphics);
        DrawStatusIndicator(graphics, indicatorColor);
        return CloneIcon(bitmap);
    }

    private static void DrawTerminalPrompt(System.Drawing.Graphics graphics)
    {
        using var promptPen = new System.Drawing.Pen(
            System.Drawing.Color.FromArgb(246, 248, 252),
            7f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        graphics.DrawLines(
            promptPen,
            [
                new System.Drawing.PointF(19f, 18f),
                new System.Drawing.PointF(34f, 32f),
                new System.Drawing.PointF(19f, 46f)
            ]);
    }

    private static void DrawStatusIndicator(
        System.Drawing.Graphics graphics,
        System.Drawing.Color indicatorColor)
    {
        using var outlineBrush = new System.Drawing.SolidBrush(
            System.Drawing.Color.FromArgb(22, 29, 39));
        using var indicatorBrush = new System.Drawing.SolidBrush(indicatorColor);

        graphics.FillEllipse(outlineBrush, 37, 37, 26, 26);
        graphics.FillEllipse(indicatorBrush, 41, 41, 18, 18);
    }

    private static System.Drawing.Icon CloneIcon(System.Drawing.Bitmap bitmap)
    {
        var iconHandle = bitmap.GetHicon();
        try
        {
            using var temporaryIcon = System.Drawing.Icon.FromHandle(iconHandle);
            return (System.Drawing.Icon)temporaryIcon.Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr iconHandle);
}
