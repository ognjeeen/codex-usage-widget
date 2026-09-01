using System.Windows;
using System.Windows.Media;
using CodexUsageWidget.Views.Controls;

namespace CodexUsageWidget.Tests;

public sealed class UsagePaceHoverSelectorTests
{
    [Fact]
    public void FindNearestIndexSnapsToTheClosestRecordedPoint()
    {
        var points = new PointCollection(
        [
            new Point(0, 10),
            new Point(70, 12),
            new Point(280, 15)
        ]);

        var selectedIndex = UsagePaceHoverSelector.FindNearestIndex(points, x: 200);

        Assert.Equal(2, selectedIndex);
    }
}
