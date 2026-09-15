using System.Globalization;
using CodexUsageWidget.Views;

namespace CodexUsageWidget.Tests;

public sealed class ProgressValueWidthConverterTests
{
    [Theory]
    [InlineData(400d, 88d, 352d)]
    [InlineData(400d, 50d, 200d)]
    [InlineData(400d, 5d, 20d)]
    public void ConvertsProgressToAStableLeftAnchoredWidth(
        double actualWidth,
        double value,
        double expectedWidth)
    {
        var converter = new ProgressValueWidthConverter();

        var result = converter.Convert(
            [actualWidth, value, 0d, 100d],
            typeof(double),
            parameter: null!,
            CultureInfo.InvariantCulture);

        Assert.Equal(expectedWidth, Assert.IsType<double>(result), precision: 6);
    }
}
