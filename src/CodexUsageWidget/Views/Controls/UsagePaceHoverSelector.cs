using System.Windows.Media;

namespace CodexUsageWidget.Views.Controls;

internal static class UsagePaceHoverSelector
{
    public static int FindNearestIndex(PointCollection points, double x)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0 || !double.IsFinite(x))
        {
            return -1;
        }

        var nearestIndex = 0;
        var nearestDistance = Math.Abs(points[0].X - x);
        for (var index = 1; index < points.Count; index++)
        {
            var distance = Math.Abs(points[index].X - x);
            if (distance < nearestDistance)
            {
                nearestIndex = index;
                nearestDistance = distance;
            }
        }

        return nearestIndex;
    }
}
