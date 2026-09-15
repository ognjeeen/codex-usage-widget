using System.Globalization;
using System.Windows.Data;

namespace CodexUsageWidget.Views;

public sealed class ProgressValueWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 4 ||
            !TryRead(values[0], out var actualWidth) ||
            !TryRead(values[1], out var value) ||
            !TryRead(values[2], out var minimum) ||
            !TryRead(values[3], out var maximum) ||
            double.IsNaN(actualWidth) ||
            double.IsNaN(value) ||
            double.IsNaN(minimum) ||
            double.IsNaN(maximum) ||
            actualWidth <= 0d ||
            maximum <= minimum)
        {
            return 0d;
        }

        var progress = Math.Clamp((value - minimum) / (maximum - minimum), 0d, 1d);
        return actualWidth * progress;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static bool TryRead(object? value, out double result)
    {
        if (value is double doubleValue)
        {
            result = doubleValue;
            return true;
        }

        return double.TryParse(
            value?.ToString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result);
    }
}
